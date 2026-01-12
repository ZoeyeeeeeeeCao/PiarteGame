using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CelyneMissionManager : MonoBehaviour
{
    [Header("Mission UI Group")]
    public GameObject missionBox;
    public Image missionImage;
    public TextMeshProUGUI missionText;

    [Header("Phase 1: Talk to NPCs")]
    public string triggerTag = "Player";
    [Tooltip("The object with the Box Collider (Is Trigger must be checked)")]
    public GameObject startingTrigger;
    [Tooltip("How long to wait after collision before showing Phase 1 mission")]
    public float delayAfterCollision = 5.0f;

    public List<GameObject> npcTriggers;
    public string phase1Text = "Talk to the villagers (0/3)";

    [Header("Phase 2: New Objective")]
    public string phase2Text = "Investigate the strange particles.";
    public GameObject newObjectiveMarker;
    public GameObject phase2ObjectToReveal;
    public GameObject phase2ObjectToHide1;
    public GameObject phase2ObjectToHide2;
    public MonoBehaviour scriptToUnlock;

    [Header("Phase 3: Final Mission")]
    public string finalMissionText = "Follow the wine cart";

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip missionUpdateSound;

    [Header("Slide Through Settings")]
    public float slideSpeed = 500f;
    public float slideDuration = 1f;
    public Color yellowColor = Color.yellow;

    private int npcsTalkedTo = 0;
    private bool missionStarted = false;
    private bool phase1Complete = false;
    private bool phase2Complete = false;
    private bool collisionDetected = false;
    private bool isSliding = false;

    void Start()
    {
        // Mission box stays visible at start (showing "Explore the area" from other script)
        // We'll hide it when player collides with trigger

        if (newObjectiveMarker != null) newObjectiveMarker.SetActive(false);
        if (phase2ObjectToReveal != null) phase2ObjectToReveal.SetActive(false);
        if (phase2ObjectToHide1 != null) phase2ObjectToHide1.SetActive(false);
        if (phase2ObjectToHide2 != null) phase2ObjectToHide2.SetActive(false);
        if (scriptToUnlock != null) scriptToUnlock.enabled = false;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // When player collides, hide mission box and start countdown
        if (other.CompareTag(triggerTag) && !collisionDetected)
        {
            Debug.Log("Player collided with trigger! Hiding mission box...");
            collisionDetected = true;
            StartCoroutine(WaitForDialogueThenStartMission());
        }
    }

    IEnumerator WaitForDialogueThenStartMission()
    {
        // Immediately hide the mission box (while dialogue plays)
        HideMissionUI();

        Debug.Log("Waiting " + delayAfterCollision + " seconds for dialogue to finish...");

        // Wait for dialogue to finish (adjust this time based on your dialogue length)
        yield return new WaitForSeconds(delayAfterCollision);

        // Now start the actual mission
        missionStarted = true;

        // Show mission box with Phase 1
        if (missionBox != null) missionBox.SetActive(true);
        if (missionImage != null) missionImage.gameObject.SetActive(true);

        UpdateMissionUI(phase1Text.Replace("0/3", "0/" + npcTriggers.Count));
        PlayMissionSound();
        Debug.Log("Phase 1 Mission Started!");
    }

    void Update()
    {
        if (!missionStarted) return;

        // NPC Tracking
        if (!phase1Complete && !isSliding)
        {
            CheckNPCProgress();
        }

        // Phase 2 Tracking
        if (phase1Complete && !phase2Complete)
        {
            if (newObjectiveMarker == null)
            {
                CompletePhase2();
            }
        }
    }

    void CheckNPCProgress()
    {
        int currentCount = 0;
        foreach (GameObject npc in npcTriggers)
        {
            if (npc == null) currentCount++;
        }

        if (currentCount != npcsTalkedTo)
        {
            npcsTalkedTo = currentCount;
            Debug.Log("NPC Progress updated: " + npcsTalkedTo + "/" + npcTriggers.Count);

            UpdateMissionUI("Talk to the villagers (" + npcsTalkedTo + "/" + npcTriggers.Count + ")");
            PlayMissionSound();

            // Check if all NPCs are destroyed
            if (npcsTalkedTo >= npcTriggers.Count)
            {
                StartCoroutine(CompletePhase1WithSlide());
            }
        }
    }

    IEnumerator CompletePhase1WithSlide()
    {
        isSliding = true;
        phase1Complete = true;
        Debug.Log("Phase 1 Complete! Starting slide animation...");

        // Change text color to yellow
        if (missionText != null)
        {
            missionText.color = yellowColor;
        }

        // Slide through animation
        RectTransform missionRect = missionBox.GetComponent<RectTransform>();
        if (missionRect != null)
        {
            Vector3 startPos = missionRect.anchoredPosition;
            Vector3 endPos = startPos + new Vector3(slideSpeed, 0, 0);

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideDuration;
                missionRect.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }

        // Reset position and start Phase 2
        if (missionRect != null)
        {
            missionRect.anchoredPosition = Vector3.zero;
        }

        // Reset text color
        if (missionText != null)
        {
            missionText.color = Color.white;
        }

        StartPhase2();
        isSliding = false;
    }

    void StartPhase2()
    {
        Debug.Log("Starting Phase 2...");

        // Reveal the two hidden objects for Phase 2
        if (phase2ObjectToHide1 != null) phase2ObjectToHide1.SetActive(true);
        if (phase2ObjectToHide2 != null) phase2ObjectToHide2.SetActive(true);

        // Show phase 2 objects
        if (newObjectiveMarker != null) newObjectiveMarker.SetActive(true);
        if (phase2ObjectToReveal != null) phase2ObjectToReveal.SetActive(true);
        if (scriptToUnlock != null) scriptToUnlock.enabled = true;

        UpdateMissionUI(phase2Text);
        PlayMissionSound();
    }

    void CompletePhase2()
    {
        phase2Complete = true;
        Debug.Log("Phase 2 Complete!");
        UpdateMissionUI(finalMissionText);
        PlayMissionSound();
    }

    void HideMissionUI()
    {
        if (missionBox != null) missionBox.SetActive(false);
        if (missionImage != null) missionImage.gameObject.SetActive(false);
    }

    void UpdateMissionUI(string newText)
    {
        if (missionText != null) missionText.text = newText;
    }

    void PlayMissionSound()
    {
        if (audioSource != null && missionUpdateSound != null)
        {
            audioSource.PlayOneShot(missionUpdateSound);
        }
    }
}