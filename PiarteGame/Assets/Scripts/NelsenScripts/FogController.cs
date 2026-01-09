using UnityEngine;
using VolumetricFogAndMist2;

public class FogController : MonoBehaviour
{
    [Header("Settings")]
    public VolumetricFog fogRenderer;
    public VolumetricFogProfile fogProfile;

    [Space]
    public float startDensity = 0.0f;
    public float targetDensity = 0.2f;
    public float transitionSpeed = 0.1f;

    private VolumetricFogProfile runtimeProfile;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (fogRenderer != null && fogProfile != null)
        {
            // Create a runtime instance so we don't modify the project asset file
            runtimeProfile = Instantiate(fogProfile);

            // Apply the starting density immediately
            runtimeProfile.density = startDensity;

            // Assign the unique instance to the renderer
            fogRenderer.profile = runtimeProfile;

            // Sync the renderer with the new starting value
            fogRenderer.UpdateMaterialProperties();
        }
    }

    /// <summary>
    /// Call this function from your CutsceneController's UnityEvent
    /// </summary>
    public void ActivateFog()
    {
        if (runtimeProfile != null)
        {
            isTransitioning = true;
            Debug.Log("Fog Transition Started...");
        }
    }

    private void Update()
    {
        if (!isTransitioning || runtimeProfile == null) return;

        // Move the density value incrementally
        runtimeProfile.density = Mathf.MoveTowards(
            runtimeProfile.density,
            targetDensity,
            transitionSpeed * Time.deltaTime
        );

        // FORCE the renderer to send the new density to the GPU
        fogRenderer.UpdateMaterialProperties();

        // Stop the loop once we reach the target
        if (Mathf.Approximately(runtimeProfile.density, targetDensity))
        {
            isTransitioning = false;
            Debug.Log("Fog Transition Complete.");
        }
    }
}