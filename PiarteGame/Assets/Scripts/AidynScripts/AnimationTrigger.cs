using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTriggerName = "Play";
    [SerializeField] private string tagToDetect = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering has the correct tag
        if (other.CompareTag(tagToDetect))
        {
            // Trigger the animation
            if (animator != null)
            {
                animator.SetTrigger(animationTriggerName);
            }
        }
    }
}