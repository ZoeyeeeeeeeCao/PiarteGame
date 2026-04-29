using System.Collections.Generic;
using UnityEngine;

public class InventoryForceToOne : MonoBehaviour
{
    [Header("Trigger")]
    public KeyCode applyKey = KeyCode.F6;

    [Header("Target Items (set these to 1)")]
    public List<ItemData> targetItems = new List<ItemData>();

    [Header("Debug")]
    public bool log = true;

    private void Update()
    {
        if (Input.GetKeyDown(applyKey))
        {
            Apply();
        }
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        if (targetItems == null || targetItems.Count == 0)
        {
            if (log) Debug.LogWarning("[InventoryForceToOne] No target items assigned.");
            return;
        }

        for (int i = 0; i < targetItems.Count; i++)
        {
            var item = targetItems[i];
            if (item == null) continue;

            // If player has none -> add 1
            if (!StaticInventory.Has(item, 1))
            {
                StaticInventory.Add(item, 1);
                if (log) Debug.Log($"[InventoryForceToOne] {item.displayName}: 0 -> 1");
                continue;
            }

            // If player has 2 or more -> keep removing until only 1 left
            int safety = 999; // avoid infinite loops if something is wrong
            while (StaticInventory.Has(item, 2) && safety-- > 0)
            {
                StaticInventory.Remove(item, 1);
            }

            if (log) Debug.Log($"[InventoryForceToOne] {item.displayName}: forced to 1");
        }
    }
}