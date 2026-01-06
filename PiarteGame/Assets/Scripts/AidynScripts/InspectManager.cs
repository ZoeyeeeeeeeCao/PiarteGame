using UnityEngine;

public class InspectManager : MonoBehaviour
{
    public Transform spawnPoint;
    public Camera inspectCamera;

    [Header("Optional: Let InspectManager manage pause/cursor")]
    public bool manageTimeScale = false;
    public bool manageCursor = false;

    GameObject current;
    InspectRotator currentRotator;

    float prevTimeScale = 1f;
    CursorLockMode prevLockMode;
    bool prevCursorVisible;

    public InspectRotator CurrentRotator => currentRotator;

    public void Show(GameObject collectiblePrefab)
    {
        if (collectiblePrefab == null)
        {
            Debug.LogWarning("[InspectManager] Show called with null prefab.");
            return;
        }

        Hide();

        // Save state (so Hide restores correctly if manage* toggles are ON)
        prevTimeScale = Time.timeScale;
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        current = Instantiate(collectiblePrefab, spawnPoint.position, spawnPoint.rotation);

        int inspectLayer = LayerMask.NameToLayer("Inspect");
        current.layer = inspectLayer;
        foreach (Transform t in current.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = inspectLayer;

        currentRotator = current.GetComponent<InspectRotator>();
        if (currentRotator == null) currentRotator = current.AddComponent<InspectRotator>();
        currentRotator.inspectCamera = inspectCamera;

        if (manageTimeScale) Time.timeScale = 0f;

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Hide()
    {
        if (current != null) Destroy(current);
        current = null;
        currentRotator = null;

        if (manageTimeScale) Time.timeScale = prevTimeScale;

        if (manageCursor)
        {
            Cursor.lockState = prevLockMode;
            Cursor.visible = prevCursorVisible;
        }
    }

    public bool IsInspecting()
    {
        return current != null;
    }
}
