using UnityEngine;

public class CutsceneInventoryReward : MonoBehaviour
{
    [Header("Reward")]
    public ItemData item;
    [Min(1)] public int amount = 1;

    [Header("Rules")]
    [Tooltip("If true, this reward can only be given once.")]
    public bool giveOnlyOnce = true;

    private bool _given;

    /// <summary>
    /// Call this from a UnityEvent (cutscene start or end)
    /// </summary>
    public void GiveReward()
    {
        if (giveOnlyOnce && _given) return;
        if (item == null)
        {
            Debug.LogWarning("CutsceneInventoryReward: No ItemData assigned.");
            return;
        }

        bool ok = StaticInventory.Add(item, amount);

        if (ok)
        {
            _given = true;
            Debug.Log($"[Cutscene Reward] Added {item.displayName} x{amount}");
        }
        else
        {
            Debug.LogWarning($"[Cutscene Reward] FAILED (blocked/full): {item.displayName}");
        }
    }
}
