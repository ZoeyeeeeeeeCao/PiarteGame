using UnityEngine;

public class KillTriggerZone : MonoBehaviour
{
    [Header("Who can die")]
    public string playerTag = "Player";

    [Header("Options")]
    [Tooltip("如果勾选，只会生效一次")]
    public bool onlyOnce = false;

    private bool used = false;

    private void OnTriggerEnter(Collider other)
    {
        // 不是玩家，直接忽略
        if (!other.CompareTag(playerTag)) return;

        // 只触发一次的情况
        if (onlyOnce && used) return;

        // 拿到 PlayerHealthController 单例
        var health = PlayerHealthController.Instance;
        if (health == null)
        {
            Debug.LogWarning("KillTriggerZone: 没找到 PlayerHealthController.Instance");
            return;
        }

        used = true;

        // 直接扣掉当前所有血量，相当于调用 Die()
        health.Damage(health.MaxHealth);
        // 也可以用一个超大数，比如：
        // health.Damage(9999f);
    }
}

