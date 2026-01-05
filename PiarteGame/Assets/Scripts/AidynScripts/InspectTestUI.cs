using UnityEngine;

public class InspectTestUI : MonoBehaviour
{
    [Header("References")]
    public InspectManager inspectManager;

    [Header("Test Prefabs")]
    public GameObject stone01;
    public GameObject stone02;
    public GameObject sword;

    void Start()
    {
        //inspectManager.Show(stone01);
    }

    // These are called by UI Buttons
    public void InspectStone01()
    {
        inspectManager.Show(stone01);
    }

    public void InspectStone02()
    {
        inspectManager.Show(stone02);
    }

    public void InspectSword()
    {
        inspectManager.Show(sword);
    }

    public void CloseInspect()
    {
        inspectManager.Hide();
    }
}
