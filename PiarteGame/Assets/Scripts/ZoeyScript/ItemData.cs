using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [Header("Identity (must be unique)")]
    public string itemId = "item_001";

    [Header("UI Text")]
    public string displayName = "Item";

    [Header("Category")]
    public PickUpItemCategory category;

    [Header("Description")]
    [TextArea(2, 8)]
    public string description;

    [Header("Stacking (optional)")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("Initial Amount (Start Inventory)")]
    [Min(0)]
    public int initialAmount = 0;   // ✅ NEW: 自定义初始量

    [Header("Consumable Effects")]
    public bool isConsumable;
    public float healAmount;

    [Header("Inspect (3D preview in UI)")]
    public GameObject inspectPrefab; // ✅ 你同学的3D预览用这个，不动
}
