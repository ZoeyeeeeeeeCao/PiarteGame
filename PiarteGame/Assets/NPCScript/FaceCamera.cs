using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Assign your Cinemachine virtual camera's brain or main camera")]
    public Camera targetCamera;

    private Vector3 _offsetFromParent;

    private void Start()
    {
        // If no camera assigned, try to find main camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Calculate the initial offset above the cube's position
        _offsetFromParent = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // Maintain the position offset above the parent
        transform.position = transform.parent.position + _offsetFromParent;

        // Make the canvas face the camera
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;
        directionToCamera.y = 0;  // Keep the text upright

        // Apply rotation to face the camera
        if (directionToCamera == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        transform.rotation = targetRotation;
    }
}