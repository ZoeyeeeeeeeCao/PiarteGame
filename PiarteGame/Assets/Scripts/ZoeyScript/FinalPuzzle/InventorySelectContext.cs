using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySelectContext : MonoBehaviour
{
    public static InventorySelectContext Instance { get; private set; }

    [Header("Refs")]
    public InventoryMenuUI inventoryUI;
    public PlacementPrefabRegistry prefabRegistry;

    [Header("Placement Rules")]
    [Tooltip("If true, disable InteractPickup + hint UI + trigger colliders on placed objects")]
    public bool disablePickupOnPlaced = true;

    // 通知外部：某个spawnPoint上放置了什么
    // 参数：spawnPoint, itemData, sourcePrefab, spawnedInstance
    public event Action<Transform, ItemData, GameObject, GameObject> OnPlacedChanged;

    [Header("Runtime")]
    public bool selectModeActive;
    public Transform spawnPoint;

    // ✅ NEW: 每个 spawnPoint 各自有一个“槽位状态”
    [Serializable]
    private class SlotState
    {
        public GameObject placedGO;
        public ItemData placedItem;
    }

    private readonly Dictionary<Transform, SlotState> slotStates = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginSelect(Transform spawnAt)
    {
        spawnPoint = spawnAt;
        selectModeActive = true;

        inventoryUI.SendMessage("OpenUI", SendMessageOptions.DontRequireReceiver);
    }

    public void EndSelect()
    {
        selectModeActive = false;
        spawnPoint = null;
    }

    public void SelectItem(ItemData newItem)
    {
        if (!selectModeActive || newItem == null) return;

        if (!spawnPoint)
        {
            Debug.LogWarning("SelectItem: spawnPoint is null");
            return;
        }

        var newPrefab = prefabRegistry ? prefabRegistry.GetPrefab(newItem) : null;
        if (!newPrefab)
        {
            Debug.LogWarning($"No world prefab mapped for item: {newItem.displayName}");
            return;
        }

        // 取出/创建这个 spawnPoint 的槽位状态
        if (!slotStates.TryGetValue(spawnPoint, out var slot))
        {
            slot = new SlotState();
            slotStates[spawnPoint] = slot;
        }

        // 1) 先扣新物品（-1）
        if (!StaticInventory.Remove(newItem, 1))
        {
            Debug.LogWarning($"SelectItem failed: inventory has no {newItem.displayName}");
            return;
        }

        // 2) 只替换“同一个 spawnPoint”上的旧物体（其他盆不受影响）
        if (slot.placedGO != null && slot.placedItem != null)
        {
            bool returned = StaticInventory.Add(slot.placedItem, 1);

            if (!returned)
            {
                // 回收失败：回滚新物品
                StaticInventory.Add(newItem, 1);
                Debug.LogWarning($"Replace blocked: cannot return old item '{slot.placedItem.displayName}' to inventory.");
                return;
            }

            Destroy(slot.placedGO);
            slot.placedGO = null;
            slot.placedItem = null;
        }

        // 3) 在这个 spawnPoint 生成新物体，并记录到该槽位
        var instance = Instantiate(newPrefab, spawnPoint.position, spawnPoint.rotation);
        instance.transform.SetParent(spawnPoint, true);

        slot.placedGO = instance;
        slot.placedItem = newItem;

        // 4) 放进盆后禁用拾取（可开关）
        if (disablePickupOnPlaced)
            DisablePickupFeatures(instance);

        // 5) 关闭背包（恢复 timeScale）
        inventoryUI.SendMessage("CloseUI", SendMessageOptions.DontRequireReceiver);

        // 6) 通知外部（盆脚本会收到，并且只认自己的 spawnPoint）
        OnPlacedChanged?.Invoke(spawnPoint, newItem, newPrefab, instance);

        // 7) 结束选择模式
        EndSelect();
    }

    private void DisablePickupFeatures(GameObject placedGO)
    {
        if (!placedGO) return;

        // 关 InteractPickup + hint
        var pickups = placedGO.GetComponentsInChildren<InteractPickup>(true);
        foreach (var p in pickups)
        {
            if (!p) continue;
            if (p.hintUI) p.hintUI.SetActive(false);
            p.enabled = false;
        }

        // 禁用 Trigger Collider（避免提示/交互抢E）
        var colliders = placedGO.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (!col) continue;
            if (col.isTrigger) col.enabled = false;
        }
    }

    // （可选）如果你未来要“清空某个盆”，可以调用这个
    public void ClearSlot(Transform point, bool returnToInventory)
    {
        if (!point) return;
        if (!slotStates.TryGetValue(point, out var slot)) return;

        if (slot.placedGO)
            Destroy(slot.placedGO);

        if (returnToInventory && slot.placedItem)
            StaticInventory.Add(slot.placedItem, 1);

        slot.placedGO = null;
        slot.placedItem = null;
    }
}
