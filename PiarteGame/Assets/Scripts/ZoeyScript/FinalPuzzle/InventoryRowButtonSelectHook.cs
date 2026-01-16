using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRowButtonSelectHook : MonoBehaviour
{
    [Header("Refs")]
    public InventoryMenuUI inventoryUI;

    private FieldInfo boundItemField;

    private void Awake()
    {
        boundItemField = typeof(InventoryRowButton).GetField(
            "boundItem",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
    }

    private void Update()
    {
        var ctx = InventorySelectContext.Instance;
        if (ctx == null) return;
        if (!ctx.selectModeActive) return;

        if (!inventoryUI || !inventoryUI.windowRoot || !inventoryUI.windowRoot.activeSelf) return;

        RebindAllRows();
    }

    private void RebindAllRows()
    {
        if (!inventoryUI.contentRoot) return;

        for (int i = 0; i < inventoryUI.contentRoot.childCount; i++)
        {
            var child = inventoryUI.contentRoot.GetChild(i);
            var row = child.GetComponent<InventoryRowButton>();
            if (!row) continue;

            var btn = row.button ? row.button : row.GetComponent<Button>();
            if (!btn) continue;

            ItemData item = null;
            if (boundItemField != null)
                item = boundItemField.GetValue(row) as ItemData;

            // 选择模式：强制接管点击（spawn + close）
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var ctx = InventorySelectContext.Instance;
                if (ctx != null && ctx.selectModeActive)
                {
                    ctx.SelectItem(item);
                }
                else
                {
                    // 非选择模式：回到原本详情/预览逻辑
                    inventoryUI.SendMessage("OnRowClicked", item, SendMessageOptions.DontRequireReceiver);
                }
            });
        }
    }
}
