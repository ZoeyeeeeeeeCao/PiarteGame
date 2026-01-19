using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRowButton : MonoBehaviour
{
    public Button button;
    public TMP_Text nameText;
    public TMP_Text amountText;

    private ItemData boundItem;

    public void Bind(ItemData item, int amount, Action<ItemData> onClick)
    {
        boundItem = item;

        if (nameText)
            nameText.text = item ? item.displayName : "(Unknown)";

        if (amountText)
        {
            // ✅ 核心判断在这里
            if (item != null && item.stackable)
                amountText.text = amount.ToString();
            else
                amountText.text = "";   // not stackable → 留空
        }

        if (!button)
            button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(boundItem));
    }

}
