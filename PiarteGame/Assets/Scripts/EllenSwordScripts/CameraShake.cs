using UnityEngine;
using FS_ThirdPerson;

public class FS_FS_CameraShakeBridgeBridge : MonoBehaviour
{
    public static FS_FS_CameraShakeBridgeBridge Instance;
    private CameraController controller;

    void Awake()
    {
        // If an instance already exists, destroy this one to avoid duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        controller = GetComponent<CameraController>();
    }

    public void Shake(float duration = 0.2f, float magnitude = 0.1f, float rotationStrength = 2f)
    {
        if (controller != null)
        {
            // This calls the shake function ALREADY inside your CameraController.cs
            controller.StartFS_CameraShakeBridge(magnitude, duration);
            Debug.Log($"📹 FS Bridge Triggered Shake: Mag {magnitude}, Dur {duration}");
        }
    }
}