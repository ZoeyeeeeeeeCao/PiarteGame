using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MonologueTriggerOnce : MonoBehaviour
{
    public string playerTag = "Player";

    [Header("Dialogue")]
    public MonologueData monologue;

    [Header("UI Controller")]
    public MonologueUIController ui;

    public bool triggerOnce = true;
    private bool triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered) return;
        if (!other.CompareTag(playerTag)) return;

        if (!ui) ui = FindFirstObjectByType<MonologueUIController>();
        if (!ui) return;

        triggered = true;
        ui.Play(monologue);
    }
}
