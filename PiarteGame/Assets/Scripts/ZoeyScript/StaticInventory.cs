using System;
using System.Collections.Generic;

public static class StaticInventory
{
    private static Dictionary<string, int> items = new Dictionary<string, int>();
    public static event Action OnChanged;

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
    }
}
