using UnityEngine;
using System.Collections;

public class UIFader : MonoBehaviour
{
    public static UIFader Instance;

    [Header("UI Elements")]
    public GameObject hudCanvas;
    public GameObject[] dialoguePanels; // Array of GameObjects (dialogue panels)

    [Header("Settings")]
    public float fadeDuration = 0.25f;

    private CanvasGroup hudCanvasGroup;
    private CanvasGroup[] dialogueCanvasGroups;
    private bool isDialogueActive = false; // Track if any dialogue is showing

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Setup HUD CanvasGroup
        if (hudCanvas != null)
        {
            hudCanvasGroup = hudCanvas.GetComponent<CanvasGroup>();
            if (hudCanvasGroup == null)
            {
                hudCanvasGroup = hudCanvas.AddComponent<CanvasGroup>();
            }
        }

        // Setup Dialogue CanvasGroups from GameObjects
        if (dialoguePanels != null && dialoguePanels.Length > 0)
        {
            dialogueCanvasGroups = new CanvasGroup[dialoguePanels.Length];

            for (int i = 0; i < dialoguePanels.Length; i++)
            {
                if (dialoguePanels[i] != null)
                {
                    dialogueCanvasGroups[i] = dialoguePanels[i].GetComponent<CanvasGroup>();
                    if (dialogueCanvasGroups[i] == null)
                    {
                        dialogueCanvasGroups[i] = dialoguePanels[i].AddComponent<CanvasGroup>();
                    }
                    // Start with dialogue hidden
                    dialogueCanvasGroups[i].alpha = 0f;
                    dialogueCanvasGroups[i].interactable = false;
                    dialogueCanvasGroups[i].blocksRaycasts = false;
                }
            }
        }
    }

    // Call this when dialogue starts (uses first dialogue panel by default)
    public void ShowDialogue()
    {
        ShowDialogue(0);
    }

    // Call this when dialogue starts with specific index
    public void ShowDialogue(int index)
    {
        if (dialogueCanvasGroups == null || index < 0 || index >= dialogueCanvasGroups.Length)
        {
            Debug.LogWarning($"Invalid dialogue panel index: {index}");
            return;
        }

        isDialogueActive = true;

        // Force hide HUD immediately when dialogue appears
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 0f;
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
        }

        // Show the dialogue
        StartCoroutine(FadeIn(dialogueCanvasGroups[index]));
    }

    // Call this when dialogue ends
    public void ShowHUD()
    {
        isDialogueActive = false;

        // Hide all dialogue panels
        if (dialogueCanvasGroups != null)
        {
            for (int i = 0; i < dialogueCanvasGroups.Length; i++)
            {
                if (dialogueCanvasGroups[i] != null && dialogueCanvasGroups[i].alpha > 0)
                {
                    StartCoroutine(FadeOut(dialogueCanvasGroups[i]));
                }
            }
        }

        // Show HUD
        if (hudCanvasGroup != null)
        {
            StartCoroutine(FadeIn(hudCanvasGroup));
        }
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        if (cg == null) yield break;

        float elapsed = 0f;
        float startAlpha = cg.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private IEnumerator FadeOut(CanvasGroup cg)
    {
        if (cg == null) yield break;

        float elapsed = 0f;
        float startAlpha = cg.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    // Public method to check if dialogue is active (useful for other scripts)
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}