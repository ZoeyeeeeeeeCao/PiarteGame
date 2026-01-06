using UnityEngine;
using FS_ThirdPerson; // This is the namespace used in your CameraController

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private CameraController controller;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Get the Third Person Controller sitting on this same camera
        controller = GetComponent<CameraController>();
    }

    // This is called by your Sword script when the Animation Event hits an enemy
    public void Shake(float duration = 0.2f, float magnitude = 0.1f, float rotationStrength = 2f)
    {
        if (controller != null)
        {
            // We call the function ALREADY inside your CameraController
            // Note: I suggest using a higher magnitude (like 0.5) in the inspector 
            // because the FS_ThirdPerson math is very subtle.
            controller.StartCameraShake(magnitude, duration);

            Debug.Log($"📹 Animation Event triggered shake: Mag {magnitude}, Dur {duration}");
        }
    }
}