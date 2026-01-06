using UnityEngine;

public class InspectRotator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 0.2f;
    public bool invertY = false;

    [Header("Zoom")]
    public Camera inspectCamera;
    public float zoomSpeed = 2f;
    public float minFov = 20f;
    public float maxFov = 60f;

    Vector2 lastPos;
    bool dragging;

    void Update()
    {
        // Mouse drag rotate
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            lastPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        if (dragging)
        {
            Vector2 cur = Input.mousePosition;
            Vector2 delta = cur - lastPos;
            lastPos = cur;

            float yaw = -delta.x * rotateSpeed;
            float pitch = (invertY ? -1f : 1f) * delta.y * rotateSpeed;

            // Rotate around world axes for stable feel
            transform.Rotate(Vector3.up, yaw, Space.World);
            transform.Rotate(Vector3.right, pitch, Space.World);
        }

        // Scroll zoom (FOV)
        if (inspectCamera != null)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                inspectCamera.fieldOfView = Mathf.Clamp(
                    inspectCamera.fieldOfView - scroll * zoomSpeed,
                    minFov,
                    maxFov
                );
            }
        }
    }
}
