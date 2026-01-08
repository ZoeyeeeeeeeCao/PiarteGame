using System;
using UnityEngine;

public class NPCWaypointFollowerLevel3 : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotateSpeed = 8.0f;
    [SerializeField] private float arriveDistance = 0.25f;

    [Header("Animator (Walking)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBoolParam = "Walk";

    private int _index;
    private bool _moving;
    private Action _onFinished;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        SetWalk(false);
    }

    public void Begin(Action onFinished)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            onFinished?.Invoke();
            return;
        }

        _index = 0;
        _moving = true;
        _onFinished = onFinished;

        SetWalk(true);
    }

    private void Update()
    {
        if (!_moving) return;

        var target = waypoints[_index];
        if (target == null) { Advance(); return; }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= arriveDistance)
        {
            Advance();
            return;
        }

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            var desiredRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotateSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * (moveSpeed * Time.deltaTime);
    }

    private void Advance()
    {
        _index++;
        if (_index >= waypoints.Length)
        {
            _moving = false;
            SetWalk(false);

            _onFinished?.Invoke();
            _onFinished = null;
        }
    }

    private void SetWalk(bool isWalking)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(walkBoolParam))
            animator.SetBool(walkBoolParam, isWalking);
    }
}
