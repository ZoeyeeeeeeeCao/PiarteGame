using System.Collections;
using UnityEngine;

public class DisappearPlatform : MonoBehaviour
{
    [Header("Disappear Settings")]
    public float delay = 2f;

    private bool triggered = false;
    private RevertibleObject revertible;

    private void Awake()
    {
        // 找同物体上的 RevertibleObject（为了配合checkpoint复活）
        revertible = GetComponent<RevertibleObject>();
        if (revertible == null)
        {
            Debug.LogWarning($"{name}: DisappearPlatform 没找到 RevertibleObject，" +
                             "将退回到 Destroy 行为（不能通过checkpoint复活这块板子）。");
        }
    }

    private void OnEnable()
    {
        // 订阅“玩家复活”事件：复活时重置 triggered
        PlayerTriggerResetOnDeath.OnPlayerRespawned += HandlePlayerRespawn;
    }

    private void OnDisable()
    {
        PlayerTriggerResetOnDeath.OnPlayerRespawned -= HandlePlayerRespawn;
    }

    private void HandlePlayerRespawn()
    {
        // 复活时，如果这块板子是激活的，就允许它再次被触发
        if (gameObject.activeSelf)
        {
            triggered = false;
            // Debug.Log($"{name}: reset triggered on respawn.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        Debug.Log("Player detected on disappear platform");

        if (revertible != null)
        {
            StartCoroutine(DisappearRoutine());
        }
        else
        {
            Destroy(gameObject, delay);
        }
    }

    private IEnumerator DisappearRoutine()
    {
        yield return new WaitForSeconds(delay);

        if (revertible != null)
        {
            revertible.FakeDestroy();   // SetActive(false) → checkpoint时会被RestoreState拉回来
        }
    }
}
