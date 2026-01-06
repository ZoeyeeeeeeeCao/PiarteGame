using System.Collections;
using UnityEngine;

public class DoorCameraCut : MonoBehaviour
{
    [Header("Cameras")]
    public Camera playerCamera;
    public Camera doorCamera;

    [Header("Timing")]
    public float holdTime = 3f;

    [Header("Optional: Disable Player Control Scripts")]
    public MonoBehaviour[] disableDuringCut;

    bool playing;
    bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // ✅ Start 更晚执行，通常能拿到 Instance
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (QuestFinalSceneManager.Instance == null) return;

        QuestFinalSceneManager.Instance.OnDoorOpened += PlayCut;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (QuestFinalSceneManager.Instance != null)
            QuestFinalSceneManager.Instance.OnDoorOpened -= PlayCut;

        subscribed = false;
    }

    public void PlayCut()
    {
        if (playing) return;
        StartCoroutine(CutRoutine());
    }

    IEnumerator CutRoutine()
    {
        playing = true;

        for (int i = 0; i < disableDuringCut.Length; i++)
            if (disableDuringCut[i]) disableDuringCut[i].enabled = false;

        if (playerCamera) playerCamera.enabled = false;
        if (doorCamera) doorCamera.enabled = true;

        yield return new WaitForSeconds(holdTime);

        if (doorCamera) doorCamera.enabled = false;
        if (playerCamera) playerCamera.enabled = true;

        for (int i = 0; i < disableDuringCut.Length; i++)
            if (disableDuringCut[i]) disableDuringCut[i].enabled = true;

        playing = false;
    }
}
