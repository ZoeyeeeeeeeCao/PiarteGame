using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractPickup : MonoBehaviour
{
    [Header("Item to give")]
    public ItemData item;
    public int amount = 1;

    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("Optional hint UI")]
    public GameObject hintUI; // 例如一个 world-space UI 写 “Press E”

    private bool inRange;

    private void Reset()
    {
        // 方便：自动把Collider设为Trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Start()
    {
        if (hintUI) hintUI.SetActive(false);
    }

    private void Update()
    {
        if (!inRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            bool ok = StaticInventory.Add(item, amount);

            if (!ok)
            {
                Debug.Log($"Pickup failed (rule blocked / full): {item?.displayName}");
                return;
            }

            if (hintUI) hintUI.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = true;
        if (hintUI) hintUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = false;
        if (hintUI) hintUI.SetActive(false);
    }
}
