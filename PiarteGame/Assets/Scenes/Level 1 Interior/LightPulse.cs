using UnityEngine;

public class LightPulse : MonoBehaviour
{
    [Header("Settings")]
    public Light targetLight; // Drag your light here, or leave empty to auto-detect
    public float minIntensity = 0.5f; // Lowest brightness
    public float maxIntensity = 2.0f; // Highest brightness
    public float pulseSpeed = 2.0f;   // How fast it pulses

    void Start()
    {
        // If you didn't assign a light manually, try to find one on this object
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }
    }

    void Update()
    {
        if (targetLight != null)
        {
            // calculate a value between 0 and 1 that goes up and down smoothly
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f;

            // Blend between min and max intensity based on t
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }
    }
}