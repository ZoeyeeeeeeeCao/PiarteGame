using UnityEngine;

public class CapsulePlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    private Rigidbody rb;
    private Vector3 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("NO RIGIDBODY FOUND! Add Rigidbody component!");
            return;
        }

        // Make sure rigidbody is set up correctly
        rb.useGravity = false; // No gravity for top-down
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY; // Don't rotate, lock Y axis

        Debug.Log("PlayerController initialized! Rigidbody found.");
    }

    void Update()
    {
        // Get input (using X and Z for 3D movement)
        movement.x = Input.GetAxisRaw("Horizontal"); // A/D
        movement.z = Input.GetAxisRaw("Vertical");   // W/S
        movement.y = 0; // Keep on ground

        // Debug input
        if (movement.magnitude > 0)
        {
            Debug.Log("Input detected: " + movement);
        }
    }

    void FixedUpdate()
    {
        // Move the player
        if (rb != null)
        {
            Vector3 newPosition = rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);

            // Debug movement
            if (movement.magnitude > 0)
            {
                Debug.Log("Moving to: " + newPosition);
            }
        }
    }
}