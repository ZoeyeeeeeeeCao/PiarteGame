using UnityEngine;

public class PlayerLockControllerLevel3 : MonoBehaviour
{
    [Header("Player Refs")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody rigidbody3D;

    [Header("Disable these while locked (movement, camera look, weapons, etc.)")]
    [SerializeField] private Behaviour[] scriptsToDisable;

    [Header("Cursor Settings")]
    [SerializeField] private bool showCursorWhenLocked = true;

    private bool _locked;
    private bool _cachedRBKinematic;

    private void Awake()
    {
        if (characterController == null) characterController = GetComponentInChildren<CharacterController>();
        if (rigidbody3D == null) rigidbody3D = GetComponentInChildren<Rigidbody>();
    }

    public void Lock()
    {
        if (_locked) return;
        _locked = true;

        if (scriptsToDisable != null)
        {
            foreach (var b in scriptsToDisable)
                if (b != null) b.enabled = false;
        }

        if (characterController != null)
            characterController.enabled = false;

        if (rigidbody3D != null)
        {
            _cachedRBKinematic = rigidbody3D.isKinematic;
            rigidbody3D.linearVelocity = Vector3.zero;
            rigidbody3D.angularVelocity = Vector3.zero;
            rigidbody3D.isKinematic = true;
        }

        if (showCursorWhenLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Unlock()
    {
        if (!_locked) return;
        _locked = false;

        if (characterController != null)
            characterController.enabled = true;

        if (rigidbody3D != null)
            rigidbody3D.isKinematic = _cachedRBKinematic;

        if (scriptsToDisable != null)
        {
            foreach (var b in scriptsToDisable)
                if (b != null) b.enabled = true;
        }

        if (showCursorWhenLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public bool IsLocked => _locked;
}
