using UnityEngine;

public class GateControllerLevel3 : MonoBehaviour
{
    public enum GateOpenMode { AnimatorTrigger, RotateOpen }

    [Header("Mode")]
    [SerializeField] private GateOpenMode openMode = GateOpenMode.AnimatorTrigger;

    [Header("Animator Mode")]
    [SerializeField] private Animator gateAnimator;
    [SerializeField] private string openTrigger = "Open";

    [Header("Rotate Mode")]
    [SerializeField] private Transform gateTransform;
    [SerializeField] private Vector3 openEulerAngles = new Vector3(0f, 90f, 0f);
    [SerializeField] private float rotateSpeed = 120f;

    private bool _opened;
    private Quaternion _openRot;

    private void Awake()
    {
        if (gateAnimator == null) gateAnimator = GetComponentInChildren<Animator>();
        if (gateTransform == null) gateTransform = transform;
        _openRot = Quaternion.Euler(gateTransform.eulerAngles + openEulerAngles);
    }

    public void OpenGate()
    {
        if (_opened) return;
        _opened = true;

        if (openMode == GateOpenMode.AnimatorTrigger)
        {
            if (gateAnimator != null) gateAnimator.SetTrigger(openTrigger);
        }
    }

    private void Update()
    {
        if (!_opened) return;
        if (openMode != GateOpenMode.RotateOpen) return;

        gateTransform.rotation = Quaternion.RotateTowards(gateTransform.rotation, _openRot, rotateSpeed * Time.deltaTime);
    }
}
