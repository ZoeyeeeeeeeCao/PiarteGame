using UnityEngine;

public abstract class TutorialManagerBase : MonoBehaviour
{
    // The NPC calls this when conversation ends
    public abstract void OnDialogueComplete();

    // Helper to check if mission is running
    public abstract bool IsMissionActive();
}