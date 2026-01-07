using UnityEngine;
using System.Collections;

public class HardAttackCinematic : MonoBehaviour
{
    public static HardAttackCinematic Instance;

    [Header("Camera References")]
    public Camera mainCamera;
    public Transform playerHead; // Assign player's head bone

    [Header("Cinematic Settings")]
    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float zoomDuration = 0.4f;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float zoomOutDuration = 0.5f;
    [SerializeField] private float closeupDistance = 2f;
    [SerializeField] private Vector3 closeupOffset = new Vector3(0.5f, 0.2f, 0);
    [SerializeField] private float closeupFOV = 40f;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private float originalFOV;
    private bool isCinematicActive = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        originalFOV = mainCamera.fieldOfView;
    }

    public void PlayHardAttackCinematic()
    {
        if (!isCinematicActive)
            StartCoroutine(CinematicSequence());
    }

    private IEnumerator CinematicSequence()
    {
        isCinematicActive = true;

        // Store original camera state
        Transform cameraTransform = mainCamera.transform;
        originalCameraPosition = cameraTransform.localPosition;
        originalCameraRotation = cameraTransform.localRotation;

        // Slow down time
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Calculate target position (close-up of player face)
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;

        if (playerHead != null)
        {
            // Position camera in front of player's face
            Vector3 facePosition = playerHead.position + playerHead.TransformDirection(closeupOffset);
            Vector3 direction = (playerHead.position - facePosition).normalized;
            targetPosition = facePosition - direction * closeupDistance;
            targetRotation = Quaternion.LookRotation(playerHead.position - targetPosition);

            // Convert to local space
            targetPosition = cameraTransform.parent.InverseTransformPoint(targetPosition);
            targetRotation = Quaternion.Inverse(cameraTransform.parent.rotation) * targetRotation;
        }

        // Zoom in phase
        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            float t = elapsed / zoomDuration;
            t = EaseInOutCubic(t);

            cameraTransform.localPosition = Vector3.Lerp(originalCameraPosition, targetPosition, t);
            cameraTransform.localRotation = Quaternion.Slerp(originalCameraRotation, targetRotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(originalFOV, closeupFOV, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Hold phase
        yield return new WaitForSecondsRealtime(holdDuration);

        // Zoom out phase
        elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            float t = elapsed / zoomOutDuration;
            t = EaseInOutCubic(t);

            cameraTransform.localPosition = Vector3.Lerp(targetPosition, originalCameraPosition, t);
            cameraTransform.localRotation = Quaternion.Slerp(targetRotation, originalCameraRotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(closeupFOV, originalFOV, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Restore camera
        cameraTransform.localPosition = originalCameraPosition;
        cameraTransform.localRotation = originalCameraRotation;
        mainCamera.fieldOfView = originalFOV;

        // Restore time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        isCinematicActive = false;
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}