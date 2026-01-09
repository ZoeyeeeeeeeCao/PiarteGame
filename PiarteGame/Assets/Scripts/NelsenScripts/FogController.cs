using UnityEngine;
using VolumetricFogAndMist2;

public class FogController : MonoBehaviour
{
    public VolumetricFogProfile fogProfile;
    public VolumetricFog fogRenderer;

    public float startDensity = 0.0f;
    public float targetDensity = 0.2f;
    public float changeSpeed = 0.5f;

    private VolumetricFogProfile runtimeProfile;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (fogProfile != null && fogRenderer != null)
        {
            // Create the runtime copy so we don't edit the project asset
            runtimeProfile = Instantiate(fogProfile);
            runtimeProfile.density = startDensity;
            fogRenderer.profile = runtimeProfile;
        }
    }

    // This is the method the Cutscene script will call
    public void ActivateFog()
    {
        isTransitioning = true; 
    }

    private void Update()
    {
        if (!isTransitioning || runtimeProfile == null) return;

        runtimeProfile.density = Mathf.MoveTowards(
            runtimeProfile.density,
            targetDensity,
            changeSpeed * Time.deltaTime
        );

        if (Mathf.Approximately(runtimeProfile.density, targetDensity))
        {
            isTransitioning = false;
        }
    }
}