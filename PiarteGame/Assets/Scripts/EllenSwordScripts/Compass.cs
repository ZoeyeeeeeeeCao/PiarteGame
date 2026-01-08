using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public RawImage compassTape;
    public Transform playerTransform;

    [Header("Visual Settings")]
    [Tooltip("How much of the compass is visible (zoom level)")]
    public float viewScale = 0.25f;

    [Header("Smoothing")]
    [Tooltip("Higher = smoother but more lag. Try 5-15")]
    public float smoothSpeed = 10f;

    private float currentRotation;
    private float targetRotation;

    void Start()
    {
        // Initialize with current rotation
        currentRotation = playerTransform.eulerAngles.y;
        targetRotation = currentRotation;
    }

    void Update()
    {
        // Get target rotation
        targetRotation = playerTransform.eulerAngles.y;

        // Handle the 360->0 wraparound smoothly
        float delta = Mathf.DeltaAngle(currentRotation, targetRotation);
        currentRotation += delta * Time.deltaTime * smoothSpeed;

        // Keep within 0-360 range
        currentRotation = Mathf.Repeat(currentRotation, 360f);

        // Map rotation to UV coordinates
        // Texture layout: W N E S W (5 segments over 450°)
        // W=270°, N=0°/360°, E=90°, S=180°
        // Only use first 80% (0.0 to 0.8) to avoid duplicate W at end

        // Convert player's Y rotation to match compass directions
        // Unity: North=0°, East=90°, South=180°, West=270°
        float adjustedRotation = currentRotation;

        // Map 360° rotation to 0.8 texture range (W-N-E-S, excluding final W)
        float uvOffset = (adjustedRotation / 360f) * 0.8f;

        // Shift so West (270°) aligns with W at start of texture (0.0)
        // 270° / 360° * 0.8 = 0.6, but W is at 0.0, so shift by -0.6
        float westAlignment = -0.6f;

        // Calculate final position centered on current direction
        float finalX = uvOffset + westAlignment - (viewScale / 2f);

        // Wrap within the valid 0.0-0.8 range
        finalX = Mathf.Repeat(finalX + 0.8f, 0.8f);

        // Apply to compass tape
        compassTape.uvRect = new Rect(finalX, 0, viewScale, 1);
    }
}