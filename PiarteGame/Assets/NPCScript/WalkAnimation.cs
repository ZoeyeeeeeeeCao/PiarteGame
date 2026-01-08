using UnityEngine;

public class WalkForward : MonoBehaviour
{
    public float speed = 2.0f; // Adjust this to match the feet speed
    public Animator anim;

    void Update()
    {
        // If the "Walk" animation is playing, move the transform
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Walk"))
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }
}