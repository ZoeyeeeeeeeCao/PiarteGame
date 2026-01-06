using TMPro;
using UnityEngine;

public class InventoryDetailPanel : MonoBehaviour
{
    public GameObject root;
    public TMP_Text titleText;
    public TMP_Text descText;

    public void Hide()
    {
        if (!root) root = gameObject;
        root.SetActive(false);
    }

    public void Show(ItemData item)
    {
        if (!root) root = gameObject;
        root.SetActive(true);

        if (titleText) titleText.text = item ? item.displayName : "";
        if (descText) descText.text = item ? item.description : "";
    }
}
