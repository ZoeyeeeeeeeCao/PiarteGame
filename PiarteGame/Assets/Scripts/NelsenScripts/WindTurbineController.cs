using UnityEngine;

public class WindTurbineController : MonoBehaviour
{
    [SerializeField] public Animator windAnimation;

    private void Start()
    {
        windAnimation = GetComponent<Animator>();
    }
    public void ActivateWind()
    {
        windAnimation.SetBool("WindOn", true);
    }
}
