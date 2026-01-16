using System;
using System.Collections.Generic;
using UnityEngine;

public static class StaticInventory
{
    private static Dictionary<string, int> items = new Dictionary<string, int>();
    public static event Action OnChanged;

    private static bool initialized;

    public static void InitializeFromDatabase(ItemDatabase db)
    {
        if (initialized) return;
        initialized = true;

        if (db == null || db.items == null) return;

        bool changed = false;

        foreach (var item in db.items)
        {
            if (item == null) continue;
            if (item.initialAmount <= 0) continue;

            // 如果已存在（比如未来做存档载入），不覆盖
            if (items.ContainsKey(item.itemId)) continue;

            int amt = item.initialAmount;

            // 遵守原本规则
            if (!item.stackable)
                amt = Mathf.Clamp(amt, 0, 1);
            else
                amt = Mathf.Clamp(amt, 0, item.maxStack);

            if (amt <= 0) continue;

            items[item.itemId] = amt;
            changed = true;
        }

        if (changed) OnChanged?.Invoke();
    }

    public static int Count(ItemData item)
    {
        if (item == null) return 0;
        return items.TryGetValue(item.itemId, out int c) ? c : 0;
    }

    public static bool Has(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        return Count(item) >= amount;
    }

    public static bool Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        int current = Count(item);

        if (!item.stackable)
        {
            if (current > 0) return false;
            items[item.itemId] = 1;
            OnChanged?.Invoke();
            return true;
        }

        if (current + amount > item.maxStack) return false;

        items[item.itemId] = current + amount;
        OnChanged?.Invoke();
        return true;
    }

    public static bool Remove(ItemData item, int amount = 1)
    {
        if (!Has(item, amount)) return false;

        int left = Count(item) - amount;
        if (left <= 0) items.Remove(item.itemId);
        else items[item.itemId] = left;

        OnChanged?.Invoke();
        return true;
    }

    public static void Clear()
    {
        items.Clear();
        OnChanged?.Invoke();
        initialized = false; // ✅ 允许你 Clear 后重新初始化初始量
    }
}
