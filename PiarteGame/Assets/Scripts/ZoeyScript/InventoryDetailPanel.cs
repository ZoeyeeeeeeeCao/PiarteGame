using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDetailPanel : MonoBehaviour
{
    public GameObject root;
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Inspect Integration")]
    public InspectManager inspectManager;
    public Button inspectButton;
    public Button closeInspectButton; // optional
    public RectTransform inspectDragArea; // drag your RawImage rect here

    ItemData currentItem;

    void Awake()
    {
        if (inspectButton)
        {
            inspectButton.onClick.RemoveAllListeners();
            inspectButton.onClick.AddListener(OnInspectClicked);
        }

        if (closeInspectButton)
        {
            closeInspectButton.onClick.RemoveAllListeners();
            closeInspectButton.onClick.AddListener(CloseInspect);
        }
    }

    public void Hide()
    {
        if (!root) root = gameObject;
        root.SetActive(false);

        // optional but recommended: stop inspecting when panel hides
        CloseInspect();
    }

    public void Show(ItemData item)
    {
        if (!root) root = gameObject;
        root.SetActive(true);

        currentItem = item;

        if (titleText) titleText.text = item ? item.displayName : "";
        if (descText) descText.text = item ? item.description : "";

        bool canInspect = (item != null && item.inspectPrefab != null && inspectManager != null);
        if (inspectButton) inspectButton.gameObject.SetActive(canInspect);
    }

    void OnInspectClicked()
    {
        if (currentItem == null || inspectManager == null) return;
        if (currentItem.inspectPrefab == null) return;

        inspectManager.Show(currentItem.inspectPrefab);

        // Restrict rotation to the RawImage preview area (optional)
        if (inspectDragArea != null && inspectManager.CurrentRotator != null)
        {
            inspectManager.CurrentRotator.dragArea = inspectDragArea;
        }
    }

    public void CloseInspect()
    {
        if (inspectManager) inspectManager.Hide();
    }
}
