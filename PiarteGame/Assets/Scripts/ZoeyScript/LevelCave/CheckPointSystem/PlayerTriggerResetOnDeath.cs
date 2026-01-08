using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerTriggerResetOnDeath : MonoBehaviour
{
    public static PlayerTriggerResetOnDeath Instance { get; private set; }

    // ⭐ 全局复活事件（别的系统可以选择性订阅）
    public static event Action OnPlayerRespawned;  // NEW

    private Collider[] allColliders;
    private bool collidersDisabled = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 把玩家自己和子物体上的所有 Collider 抓出来
        allColliders = GetComponentsInChildren<Collider>(includeInactive: true);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (PlayerHealthController.Instance != null)
            PlayerHealthController.Instance.OnDeath -= OnPlayerDeath;
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (PlayerHealthController.Instance == null)
            yield return null;

        PlayerHealthController.Instance.OnDeath += OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        // 死亡瞬间：关闭所有 Collider，强制触发 OnTriggerExit
        SetCollidersEnabled(false);
        collidersDisabled = true;

        Debug.Log("PlayerTriggerResetOnDeath: Disabled colliders on death → TriggerExit fired.");
    }

    public void OnRespawn()
    {
        if (collidersDisabled)
        {
            SetCollidersEnabled(true);
            collidersDisabled = false;
        }

        // ⭐⭐ 向全局广播“玩家复活了”
        OnPlayerRespawned?.Invoke();

        Debug.Log("PlayerTriggerResetOnDeath: Re-enabled colliders after respawn + fired OnPlayerRespawned.");
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (allColliders == null) return;

        foreach (var col in allColliders)
        {
            if (col == null) continue;
            col.enabled = enabled;
        }
    }
}
