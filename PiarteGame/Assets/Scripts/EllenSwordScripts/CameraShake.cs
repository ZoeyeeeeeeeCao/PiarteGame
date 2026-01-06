using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalPosition;
    private bool isShaking = false;
    private Transform shakeTarget;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // If this is a camera, shake itself
            // If this is a parent of camera, shake the camera child
            if (GetComponent<Camera>() != null)
            {
                shakeTarget = transform;
            }
            else
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    shakeTarget = cam.transform;
                }
                else
                {
                    shakeTarget = transform;
                }
            }

            Debug.Log($"✅ CameraShake Instance created on {gameObject.name}, shaking: {shakeTarget.name}");
        }
        else
        {
            Destroy(this);
        }
    }

    public void Shake(float duration = 0.2f, float magnitude = 0.1f, float rotationStrength = 2f)
    {
        if (shakeTarget == null)
        {
            Debug.LogError("❌ CameraShake: shakeTarget is null!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(duration, magnitude, rotationStrength));

        Debug.Log($"📹 Camera shake START: Duration={duration}, Magnitude={magnitude}, Rotation={rotationStrength}");
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude, float rotationStrength)
    {
        isShaking = true;
        originalPosition = shakeTarget.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float damper = 1f - (elapsed / duration);

            float x = Random.Range(-1f, 1f) * magnitude * damper;
            float y = Random.Range(-1f, 1f) * magnitude * damper;
            float z = Random.Range(-1f, 1f) * magnitude * damper * 0.3f;

            shakeTarget.localPosition = originalPosition + new Vector3(x, y, z);

            // Optional rotation shake
            float rotX = Random.Range(-1f, 1f) * rotationStrength * damper;
            float rotY = Random.Range(-1f, 1f) * rotationStrength * damper;
            float rotZ = Random.Range(-1f, 1f) * rotationStrength * damper;

            shakeTarget.localRotation = Quaternion.Euler(rotX, rotY, rotZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalPosition;
        shakeTarget.localRotation = Quaternion.identity;
        isShaking = false;

        Debug.Log("📹 Camera shake COMPLETE");
    }
}