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

    [Tooltip("Drag the RawImage RectTransform that displays InspectRT here.")]
    public RectTransform inspectDragArea;

    // Keeping these fields so your script name/structure doesn’t “break?
    // You can leave them unassigned or delete the UI objects.
    public Button inspectButton;
    public Button closeInspectButton; // optional

    ItemData currentItem;

    void Awake()
    {
        // We no longer use inspectButton (auto-inspect on Show)
        // But we can still allow a close button if you want.
        if (closeInspectButton)
        {
            closeInspectButton.onClick.RemoveAllListeners();
            closeInspectButton.onClick.AddListener(CloseInspect);
        }

        if (inspectButton)
            inspectButton.gameObject.SetActive(false);
    }

    public void Hide()
    {
        if (!root) root = gameObject;
        root.SetActive(false);

        CloseInspect();
    }

    public void Show(ItemData item)
    {
        if (!root) root = gameObject;
        root.SetActive(true);

        currentItem = item;

        if (titleText) titleText.text = item ? item.displayName : "";
        if (descText) descText.text = item ? item.description : "";

        // Auto inspect immediately
        if (inspectManager == null)
            return;

        if (item != null && item.inspectPrefab != null)
        {
            inspectManager.Show(item.inspectPrefab);

            // Restrict rotation to the preview area
            if (inspectDragArea != null && inspectManager.CurrentRotator != null)
                inspectManager.CurrentRotator.dragArea = inspectDragArea;
        }
        else
        {
            // No prefab: clear the inspect view
            inspectManager.Hide();
        }
    }

    public void CloseInspect()
    {
        if (inspectManager) inspectManager.Hide();
    }
}
