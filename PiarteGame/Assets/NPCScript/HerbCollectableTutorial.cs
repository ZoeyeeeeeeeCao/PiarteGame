using UnityEngine;

public class HerbCollectableTutorial : MonoBehaviour
{
    public float collectionRadius = 2f;
    public GameObject interactPrompt;
    private HerbTutorialSystem manager;
    private bool playerInRange = false;

    void Start()
    {
        manager = FindObjectOfType<HerbTutorialSystem>();
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update()
    {
        CheckPlayer();
        if (playerInRange && Input.GetKeyDown(KeyCode.E)) Collect();
    }

    void CheckPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, collectionRadius);
        bool inRange = false;
        foreach (var h in hits) if (h.CompareTag("Player")) inRange = true;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (interactPrompt != null) interactPrompt.SetActive(playerInRange);
        }
    }

    void Collect()
    {
        if (manager != null) manager.OnHerbCollected();
        Destroy(gameObject);
    }
}