using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Placement Prefab Registry", fileName = "PlacementPrefabRegistry")]
public class PlacementPrefabRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemData item;
        public GameObject worldPrefab;   // ✅ 仅用于场景生成，与inspectPrefab无关
    }

    public Entry[] entries;

    public GameObject GetPrefab(ItemData item)
    {
        if (item == null || entries == null) return null;

        foreach (var e in entries)
        {
            if (e != null && e.item == item)
                return e.worldPrefab;
        }
        return null;
    }
}
