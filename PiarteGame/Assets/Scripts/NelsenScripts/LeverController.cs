using UnityEngine;
using System.Collections;

public class LeverController : MonoBehaviour
{
    [SerializeField] public Animator leverAnimator;
    [SerializeField] public CutsceneController cutsceneController;

    [Header("Floating Text")]
    [SerializeField] private GameObject floatingText; // Assign your floating text GameObject here

    [Header("Settings")]
    public float delayBeforeCutscene = 3f;
    public bool leverStatus = false;

    private void Start()
    {
        // Safety check in case it's not assigned in Inspector
        if (leverAnimator == null)
            leverAnimator = GetComponent<Animator>();
    }

    public void ActivateLever()
    {
        // Prevent re-triggering if the lever is already active
        if (leverStatus) return;

        leverStatus = true;

        // Hide/destroy the floating text immediately
        if (floatingText != null)
        {
            Destroy(floatingText);
            // OR just disable it: floatingText.SetActive(false);
        }

        // Start the process
        StartCoroutine(LeverSequenceRoutine());
    }

    private IEnumerator LeverSequenceRoutine()
    {
        // 1. Play the animation immediately
        leverAnimator.SetTrigger("On");
        Debug.Log("Lever pulled, waiting...");

        // 2. Wait for 3 seconds
        yield return new WaitForSeconds(delayBeforeCutscene);

        // 3. Start the cutscene
        if (cutsceneController != null)
        {
            cutsceneController.ActivateSequence();
        }
        else
        {
            Debug.LogError("CutsceneController is missing from the Lever!");
        }
    }
}