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

    [Header("Consumable Effects")]
    public bool isConsumable;
    public float healAmount;

}
