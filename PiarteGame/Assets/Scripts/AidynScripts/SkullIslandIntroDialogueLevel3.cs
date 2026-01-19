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
    [Tooltip("Optional. If not set, we will use the CharacterController transform.")]
    [SerializeField] private Transform playerRoot; // visual root (optional)
    [SerializeField] private CharacterController playerCC;
    [SerializeField] private Rigidbody playerRB;
    [SerializeField] private Transform standPoint;

    [Header("References")]
    [SerializeField] private DialogueUILevel3 dialogueUI;
    [SerializeField] private PlayerLockControllerLevel3 playerLock;

    [Header("UI To Hide During Dialogue")]
    [Tooltip("Any UI roots you want hidden during dialogue (HUD, minimap, interaction prompts, etc).")]
    [SerializeField] private GameObject[] uiToHideDuringDialogue;

    // Cache of previous active states so we restore correctly after dialogue
    private bool[] _uiPrevActiveStates;

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

    [Header("Camera Switching (Regular Cameras)")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera tribalTalkCamera;
    [SerializeField] private Camera playerTalkCamera;

    [Header("Skip")]
    [SerializeField] private KeyCode skipKey = KeyCode.Return; // Enter

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLineLevel3[] introLines;
    [SerializeField] private DialogueLineLevel3 questionLine;
    [SerializeField] private DialogueLineLevel3[] afterChoiceALines;
    [SerializeField] private DialogueLineLevel3[] afterChoiceBLines;

    [Header("Choice Buttons")]
    [SerializeField] private string choiceALabel = "A - The relic stone.";
    [SerializeField] private string choiceBLabel = "B - I'm just a traveler. The sea dragged me here.";
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

        // Auto-find if not assigned
        if (playerCC == null && playerRoot != null) playerCC = playerRoot.GetComponentInChildren<CharacterController>();
        if (playerRB == null && playerRoot != null) playerRB = playerRoot.GetComponentInChildren<Rigidbody>();

        if (tribalAnimator == null) tribalAnimator = GetComponentInChildren<Animator>();
        if (playerAnimator == null && playerRoot != null) playerAnimator = playerRoot.GetComponentInChildren<Animator>();

        SetTalkState(tribalTalking: false, playerTalking: false);

        SetActiveCameraModeGameplay();
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

        // 1) Snap player to stand point
        MovePlayerToStandPoint_Safe();

        // 2) Lock controls
        playerLock.Lock();

        // 2.5) Hide other UIs you don't want during dialogue
        HideDialogueBlockedUI();

        // 3) Talking: player talks the entire conversation
        SetTalkState(tribalTalking: false, playerTalking: true);

        // UI setup
        dialogueUI.ShowPanel();
        dialogueUI.SetHint("Press Enter to skip");
        dialogueUI.HideChoicesInstant();

        // Intro
        yield return PlayLines(introLines, PlayContext.Normal);

        // Question
        yield return PlayLine(questionLine, PlayContext.Normal, lineIndex: 0);

        // Choice buttons fade in
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
            yield return PlayLines(afterChoiceALines, PlayContext.AfterChoiceA);
            yield return AttackAndRestartRoutine();

            RestoreAfterDialogue();
        }
        else
        {
            yield return PlayLines(afterChoiceBLines, PlayContext.AfterChoiceB);

            // Unlock immediately after convo ends
            RestoreAfterDialogue();

            // Gate sequence continues while player is free
            StartCoroutine(GateSequenceNonBlockingRoutine());
        }

        _sequenceRunning = false;
    }

    // =========================
    // UI hide / restore
    // =========================
    private void HideDialogueBlockedUI()
    {
        if (uiToHideDuringDialogue == null || uiToHideDuringDialogue.Length == 0)
            return;

        // Allocate cache sized exactly to current list
        _uiPrevActiveStates = new bool[uiToHideDuringDialogue.Length];

        for (int i = 0; i < uiToHideDuringDialogue.Length; i++)
        {
            var go = uiToHideDuringDialogue[i];
            if (go == null)
            {
                _uiPrevActiveStates[i] = false;
                continue;
            }

            _uiPrevActiveStates[i] = go.activeSelf;
            if (go.activeSelf) go.SetActive(false);
        }
    }

    private void RestoreDialogueBlockedUI()
    {
        if (uiToHideDuringDialogue == null || uiToHideDuringDialogue.Length == 0)
            return;

        // If something weird happened (not cached), just enable them (safe default)
        if (_uiPrevActiveStates == null || _uiPrevActiveStates.Length != uiToHideDuringDialogue.Length)
        {
            for (int i = 0; i < uiToHideDuringDialogue.Length; i++)
            {
                var go = uiToHideDuringDialogue[i];
                if (go != null) go.SetActive(true);
            }
            return;
        }

        for (int i = 0; i < uiToHideDuringDialogue.Length; i++)
        {
            var go = uiToHideDuringDialogue[i];
            if (go == null) continue;

            // Restore exactly what it was before dialogue started
            go.SetActive(_uiPrevActiveStates[i]);
        }
    }

    // =========================
    // BIG FIX: teleport safely
    // =========================
    private void MovePlayerToStandPoint_Safe()
    {
        if (standPoint == null)
        {
            Debug.LogWarning("[SkullIslandIntroDialogueLevel3] StandPoint is not assigned.");
            return;
        }

        Transform controllerT = null;

        if (playerCC == null)
        {
            if (playerRoot != null) playerCC = playerRoot.GetComponentInChildren<CharacterController>();
        }
        if (playerCC != null) controllerT = playerCC.transform;

        if (controllerT == null)
        {
            controllerT = playerRoot != null ? playerRoot : transform;
            Debug.LogWarning("[SkullIslandIntroDialogueLevel3] CharacterController not found; using playerRoot instead.");
        }

        bool ccWasEnabled = (playerCC != null && playerCC.enabled);
        if (playerCC != null) playerCC.enabled = false;

        if (playerRB == null && playerRoot != null)
            playerRB = playerRoot.GetComponentInChildren<Rigidbody>();

        if (playerRB != null)
        {
            // NOTE: Unity Rigidbody uses velocity, not linearVelocity (unless you have DOTS/other extension).
            playerRB.linearVelocity = Vector3.zero;
            playerRB.angularVelocity = Vector3.zero;
        }

        controllerT.position = standPoint.position;
        controllerT.rotation = standPoint.rotation;

        if (playerRoot != null && playerRoot != controllerT)
        {
            playerRoot.position = controllerT.position;
            playerRoot.rotation = controllerT.rotation;
        }

        if (playerCC != null) playerCC.enabled = ccWasEnabled;
    }

    private void RestoreAfterDialogue()
    {
        // Turn off dialogue UI
        if (dialogueUI != null) dialogueUI.HidePanel();

        // Restore hidden UI
        RestoreDialogueBlockedUI();

        // Restore original camera
        SetActiveCameraModeGameplay();

        // Stop talk anims
        SetTalkState(tribalTalking: false, playerTalking: false);

        // Unlock player controls
        if (playerLock != null) playerLock.Unlock();
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

        SetSpeakerCamera(line.speaker);

        bool tribalTalkingNow = (line.speaker == SpeakerLevel3.Tribal);
        SetTalkState(tribalTalking: tribalTalkingNow, playerTalking: true);

        if (ctx == PlayContext.AfterChoiceA && line.speaker == SpeakerLevel3.Tribal && tribalAnimator != null)
        {
            if (lineIndex == 0 && !string.IsNullOrWhiteSpace(angryTrigger))
                tribalAnimator.SetTrigger(angryTrigger);

            if (lineIndex == 1 && !string.IsNullOrWhiteSpace(whistleTrigger))
                tribalAnimator.SetTrigger(whistleTrigger);
        }

        string speakerName = line.speaker == SpeakerLevel3.Tribal ? "Guard" : "Ashford";
        dialogueUI.SetLine(speakerName, line.text);

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

        SetTalkState(tribalTalking: false, playerTalking: true);
    }

    private void SetSpeakerCamera(SpeakerLevel3 speaker)
    {
        if (gameplayCamera == null) return;

        if (speaker == SpeakerLevel3.Player)
            SetActiveCamera(playerTalkCamera, fallbackToGameplay: true);
        else
            SetActiveCamera(tribalTalkCamera, fallbackToGameplay: true);
    }

    private void SetActiveCameraModeGameplay()
    {
        if (gameplayCamera != null) gameplayCamera.enabled = true;
        if (tribalTalkCamera != null) tribalTalkCamera.enabled = false;
        if (playerTalkCamera != null) playerTalkCamera.enabled = false;

        EnsureSingleAudioListener(gameplayCamera, tribalTalkCamera, playerTalkCamera);
    }

    private void SetActiveCamera(Camera cam, bool fallbackToGameplay)
    {
        if (gameplayCamera != null) gameplayCamera.enabled = false;
        if (tribalTalkCamera != null) tribalTalkCamera.enabled = false;
        if (playerTalkCamera != null) playerTalkCamera.enabled = false;

        if (cam != null) cam.enabled = true;
        else if (fallbackToGameplay && gameplayCamera != null) gameplayCamera.enabled = true;

        EnsureSingleAudioListener(gameplayCamera, tribalTalkCamera, playerTalkCamera);
    }

    private void EnsureSingleAudioListener(params Camera[] cams)
    {
        foreach (var c in cams)
        {
            if (c == null) continue;
            var listener = c.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = c.enabled;
        }
    }

    private IEnumerator AttackAndRestartRoutine()
    {
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
