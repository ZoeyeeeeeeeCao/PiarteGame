using UnityEngine;

public class FindGodTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (QuestFinalSceneManager.Instance != null)
            QuestFinalSceneManager.Instance.CompleteFindTheGod();

        // 只触发一次
        gameObject.SetActive(false);
    }
}
