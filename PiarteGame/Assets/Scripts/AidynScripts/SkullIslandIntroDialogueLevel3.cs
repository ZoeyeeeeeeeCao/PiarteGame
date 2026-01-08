using Unity.Cinemachine; // needs Cinemachine package
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkullIslandIntroDialogueLevel3 : MonoBehaviour
{
    public enum SpeakerLevel3 { Tribal, Player }

    [Serializable]
    public class DialogueLineLevel3
    {
        public SpeakerLevel3 speaker;
        [TextArea(2, 4)] public string text;
        public AudioClip voiceClip;
        [Tooltip("Used if no clip is provided.")]
        public float fallbackDuration = 2.0f;
    }

    [Header("Trigger Once")]
    [SerializeField] private bool triggerOnlyOnce = true;
    private bool _hasTriggered;
    private bool _sequenceRunning;

    [Header("Player Positioning (Stand Point)")]
    [SerializeField] private Transform playerRoot;             // player transform
    [SerializeField] private CharacterController playerCC;      // player's CC
    [SerializeField] private Rigidbody playerRB;               // player's RB (optional)
    [SerializeField] private Transform standPoint;             // where player stands to talk

    [Header("References")]
    [SerializeField] private DialogueUILevel3 dialogueUI;
    [SerializeField] private PlayerLockControllerLevel3 playerLock;

    [Header("Voice AudioSources")]
    [SerializeField] private AudioSource tribalVoiceSource;
    [SerializeField] private AudioSource playerVoiceSource;

    [Header("Talking Animations")]
    [SerializeField] private Animator tribalAnimator;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string talkBoolParam = "Talk";

    [Header("Choice A Special Tribal Animations")]
    [SerializeField] private string angryTrigger = "Angry";
    [SerializeField] private string whistleTrigger = "Whistle";

    [Header("Camera Switching (Cinemachine)")]
    [SerializeField] private CinemachineCamera playerTalkCam;
    [SerializeField] private CinemachineCamera tribalTalkCam;
    [SerializeField] private int activeCamPriority = 20;
    [SerializeField] private int inactiveCamPriority = 5;

    [Header("Skip")]
    [SerializeField] private KeyCode skipKey = KeyCode.Return; // Enter

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLineLevel3[] introLines;
    [SerializeField] private DialogueLineLevel3 questionLine;
    [SerializeField] private DialogueLineLevel3[] afterChoiceALines;
    [SerializeField] private DialogueLineLevel3[] afterChoiceBLines;

    [Header("Choice Buttons")]
    [SerializeField] private string choiceALabel = "A � The relic stone.";
    [SerializeField] private string choiceBLabel = "B � I�m just a traveler. The sea dragged me here.";
    [SerializeField] private float choicesFadeInDuration = 0.6f;

    [Header("Branch A - Attack")]
    [SerializeField] private GameObject[] attackersToEnable;
    [SerializeField] private float attackDelayBeforeRestart = 1.2f;

    [Header("Branch B - Gate (runs AFTER dialogue, while player is free)")]
    [SerializeField] private NPCWaypointFollowerLevel3 greeterFollower;
    [SerializeField] private float followerSafetyTimeout = 12f;
    [SerializeField] private GateControllerLevel3 gateController;

    [Header("Fade (Optional)")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.6f;

    private enum PlayContext { Normal, AfterChoiceA, AfterChoiceB }

    private void Awake()
    {
        if (dialogueUI != null) dialogueUI.HidePanel();

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        if (tribalAnimator == null) tribalAnimator = GetComponentInChildren<Animator>();
        if (playerAnimator == null && playerRoot != null) playerAnimator = playerRoot.GetComponentInChildren<Animator>();

        SetTalkState(false, false);
        SetSpeakerCamera(SpeakerLevel3.Tribal); // default
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_sequenceRunning) return;
        if (!other.CompareTag("Player")) return;
        if (triggerOnlyOnce && _hasTriggered) return;

        _hasTriggered = true;
        StartCoroutine(DialogueSequenceRoutine());
    }

    private IEnumerator DialogueSequenceRoutine()
    {
        _sequenceRunning = true;

        if (dialogueUI == null || playerLock == null)
        {
            Debug.LogError("[SkullIslandIntroDialogueLevel3] Missing DialogueUI or PlayerLock reference.");
            _sequenceRunning = false;
            yield break;
        }

        // Move player to stand point BEFORE locking (prevents sliding / physics weirdness)
        MovePlayerToStandPoint();

        // Lock player controls for the conversation
        playerLock.Lock();

        dialogueUI.ShowPanel();
        dialogueUI.SetHint("Press Enter to skip");
        dialogueUI.HideChoicesInstant();

        // Intro dialogue (no choices)
        yield return PlayLines(introLines, PlayContext.Normal);

        // The question line
        yield return PlayLine(questionLine, PlayContext.Normal, lineIndex: 0);

        // Fade in choice buttons
        bool chosen = false;
        bool choseA = false;

        dialogueUI.ShowChoicesFadeIn(
            choiceALabel,
            choiceBLabel,
            onA: () => { chosen = true; choseA = true; },
            onB: () => { chosen = true; choseA = false; },
            fadeDuration: choicesFadeInDuration
        );

        while (!chosen)
            yield return null;

        dialogueUI.HideChoicesInstant();

        if (choseA)
        {
            // Choice A: keep player locked, play lines, then attack+restart
            yield return PlayLines(afterChoiceALines, PlayContext.AfterChoiceA);
            yield return AttackAndRestartRoutine();
            // won't really matter due to reload, but safe:
            playerLock.Unlock();
        }
        else
        {
            // Choice B: play lines while locked
            yield return PlayLines(afterChoiceBLines, PlayContext.AfterChoiceB);

            // Unlock IMMEDIATELY after conversation ends (your request)
            playerLock.Unlock();
            dialogueUI.HidePanel();
            SetTalkState(false, false);

            // Now run gate sequence while player is already free
            StartCoroutine(GateSequenceNonBlockingRoutine());
        }

        _sequenceRunning = false;
    }

    private void MovePlayerToStandPoint()
    {
        if (standPoint == null || playerRoot == null) return;

        // Disable CC while teleporting
        if (playerCC == null) playerCC = playerRoot.GetComponentInChildren<CharacterController>();
        if (playerRB == null) playerRB = playerRoot.GetComponentInChildren<Rigidbody>();

        bool ccWasEnabled = playerCC != null && playerCC.enabled;
        if (playerCC != null) playerCC.enabled = false;

        if (playerRB != null)
        {
            playerRB.linearVelocity = Vector3.zero;
            playerRB.angularVelocity = Vector3.zero;
        }

        playerRoot.position = standPoint.position;
        playerRoot.rotation = standPoint.rotation;

        if (playerCC != null) playerCC.enabled = ccWasEnabled;
    }

    private IEnumerator PlayLines(DialogueLineLevel3[] lines, PlayContext ctx)
    {
        if (lines == null) yield break;
        for (int i = 0; i < lines.Length; i++)
            yield return PlayLine(lines[i], ctx, i);
    }

    private IEnumerator PlayLine(DialogueLineLevel3 line, PlayContext ctx, int lineIndex)
    {
        if (line == null) yield break;

        // Camera switches to current speaker
        SetSpeakerCamera(line.speaker);

        // Talking animation: current speaker ON, other OFF
        SetTalkState(
            tribalTalking: line.speaker == SpeakerLevel3.Tribal,
            playerTalking: line.speaker == SpeakerLevel3.Player
        );

        // Choice A special tribal animations:
        // first tribal line after choosing A -> Angry
        // second tribal line after choosing A -> Whistle
        if (ctx == PlayContext.AfterChoiceA && line.speaker == SpeakerLevel3.Tribal && tribalAnimator != null)
        {
            if (lineIndex == 0 && !string.IsNullOrWhiteSpace(angryTrigger))
                tribalAnimator.SetTrigger(angryTrigger);

            if (lineIndex == 1 && !string.IsNullOrWhiteSpace(whistleTrigger))
                tribalAnimator.SetTrigger(whistleTrigger);
        }

        string speakerName = line.speaker == SpeakerLevel3.Tribal ? "Tribal Guard" : "Ashford";
        dialogueUI.SetLine(speakerName, line.text);

        // Voice playback
        AudioSource src = (line.speaker == SpeakerLevel3.Tribal) ? tribalVoiceSource : playerVoiceSource;
        float duration = Mathf.Max(0.2f, line.fallbackDuration);

        if (src != null)
        {
            src.Stop();
            if (line.voiceClip != null)
            {
                src.clip = line.voiceClip;
                src.Play();
                duration = Mathf.Max(0.2f, line.voiceClip.length);
            }
        }

        // Wait until time OR Enter skip
        float t = 0f;
        while (t < duration)
        {
            if (Input.GetKeyDown(skipKey))
            {
                if (src != null) src.Stop();
                break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Stop talking pose after each line (optional, keeps it snappy)
        SetTalkState(false, false);
    }

    private IEnumerator AttackAndRestartRoutine()
    {
        // Optional: attackers appear / rush
        if (attackersToEnable != null)
        {
            foreach (var go in attackersToEnable)
                if (go != null) go.SetActive(true);
        }

        float t = 0f;
        while (t < attackDelayBeforeRestart)
        {
            if (Input.GetKeyDown(skipKey)) break;
            t += Time.deltaTime;
            yield return null;
        }

        if (fadeImage != null) yield return Fade(1f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator GateSequenceNonBlockingRoutine()
    {
        // NPC walks to gate (player is already free)
        if (greeterFollower != null)
        {
            bool finished = false;
            greeterFollower.Begin(() => finished = true);

            float t = 0f;
            while (!finished && t < followerSafetyTimeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (gateController != null)
            gateController.OpenGate();
    }

    private void SetTalkState(bool tribalTalking, bool playerTalking)
    {
        if (tribalAnimator != null && !string.IsNullOrWhiteSpace(talkBoolParam))
            tribalAnimator.SetBool(talkBoolParam, tribalTalking);

        if (playerAnimator != null && !string.IsNullOrWhiteSpace(talkBoolParam))
            playerAnimator.SetBool(talkBoolParam, playerTalking);
    }

    private void SetSpeakerCamera(SpeakerLevel3 speaker)
    {
        if (playerTalkCam == null || tribalTalkCam == null) return;

        if (speaker == SpeakerLevel3.Player)
        {
            playerTalkCam.Priority = activeCamPriority;
            tribalTalkCam.Priority = inactiveCamPriority;
        }
        else
        {
            tribalTalkCam.Priority = activeCamPriority;
            playerTalkCam.Priority = inactiveCamPriority;
        }
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / dur);
            var c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;
            yield return null;
        }

        var final = fadeImage.color;
        final.a = targetAlpha;
        fadeImage.color = final;
    }
}
