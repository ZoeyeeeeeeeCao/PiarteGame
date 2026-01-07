using UnityEngine;
using FS_ThirdPerson;

public class CameraNewShake : MonoBehaviour
{
    public static CameraNewShake Instance { get; private set; }

    private CameraController controller;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get the CameraController component
        controller = GetComponent<CameraController>();

        if (controller == null)
        {
            Debug.LogError("❌ CameraController not found on " + gameObject.name);
        }
    }

    /// <summary>
    /// Triggers camera shake using the built-in FS_ThirdPerson shake system
    /// </summary>
    public void Shake(float duration = 0.2f, float magnitude = 0.1f, float rotationStrength = 2f)
    {
        if (controller != null)
        {
            // Call the correct method from CameraController
            // Parameters: (shakeAmount, duration)
            controller.StartFS_CameraShakeBridge(magnitude, duration);
            Debug.Log($"📹 Camera Shake Triggered: Magnitude={magnitude}, Duration={duration}");
        }
        else
        {
            Debug.LogWarning("⚠️ CameraController is null, cannot shake camera");
        }
    }
}