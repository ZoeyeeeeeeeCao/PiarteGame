using UnityEngine;

public class WorldSpacePromptLevel3 : MonoBehaviour
{
    [SerializeField] private GameObject root;          // the canvas root (or same object)
    [SerializeField] private Transform lookAtCamera;    // optional; auto uses Camera.main

    private void Awake()
    {
        if (root == null) root = gameObject;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        var cam = lookAtCamera != null ? lookAtCamera : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;

        // Face camera (billboard)
        Vector3 dir = root.transform.position - cam.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        root.transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
