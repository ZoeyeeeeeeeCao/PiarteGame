using UnityEngine;
using FS_ThirdPerson;

/// <summary>
/// Add this script to any GameObject in your gameplay scenes (like Player or GameManager)
/// It will automatically load and apply saved settings when the scene starts
/// </summary>
public class SettingsInitializer : MonoBehaviour
{
    private void Start()
    {
        ApplySavedSettings();
    }

    private void ApplySavedSettings()
    {
        // Load mouse sensitivity and apply to all cameras
        float mouseSensitivity = SettingsManager.GetMouseSensitivity();

        var cameraControllers = FindObjectsOfType<CameraController>();
        foreach (var camController in cameraControllers)
        {
            // Apply to third person camera
            if (camController.thirdPersonCamera?.defaultSettings != null)
            {
                camController.thirdPersonCamera.defaultSettings.sensitivity = mouseSensitivity;

                if (camController.thirdPersonCamera.overrideCameraSettings != null)
                {
                    foreach (var overrideSetting in camController.thirdPersonCamera.overrideCameraSettings)
                    {
                        if (overrideSetting.settings != null)
                            overrideSetting.settings.sensitivity = mouseSensitivity;
                    }
                }
            }

            // Apply to first person camera
            if (camController.firstPersonCamera?.defaultSettings != null)
            {
                camController.firstPersonCamera.defaultSettings.sensitivity = mouseSensitivity;

                if (camController.firstPersonCamera.overrideCameraSettings != null)
                {
                    foreach (var overrideSetting in camController.firstPersonCamera.overrideCameraSettings)
                    {
                        if (overrideSetting.settings != null)
                            overrideSetting.settings.sensitivity = mouseSensitivity;
                    }
                }
            }
        }

        // Audio and brightness are handled automatically by SettingsManager's DontDestroyOnLoad
    }
}