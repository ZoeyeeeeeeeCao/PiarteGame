using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue System/Dialogue")]
public class Dialogue : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(3, 10)]
        public string text;

        [Tooltip("Audio clip to play for this line (optional)")]
        public AudioClip audioClip;
    }

    public string npcName = "NPC";

    [Tooltip("All dialogue lines with optional audio")]
    public DialogueLine[] dialogueLines;
}