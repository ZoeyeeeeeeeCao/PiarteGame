using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryMenuUI : MonoBehaviour
{
    [Header("Toggle Window")]
    public KeyCode toggleKey = KeyCode.Tab;
    public GameObject windowRoot;

    [Header("Data")]
    public ItemDatabase database;

    [Header("Column Buttons")]
    public Button herbsButton;
    public Button stonesButton;
    public Button mapsButton; // 可选，不用可以不拖

    [Header("Column Highlight")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.25f);
    public Color selectedColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("List")]
    public Transform contentRoot;
    public InventoryRowButton rowPrefab;

    [Header("Detail Panel")]
    public InventoryDetailPanel detailPanel;

    PickUpItemCategory currentCategory = PickUpItemCategory.Herbs;
    readonly List<GameObject> spawnedRows = new();

    void OnEnable()
    {
        StaticInventory.OnChanged += Refresh;
    }

    void OnDisable()
    {
        StaticInventory.OnChanged -= Refresh;
    }

    void Start()
    {
        if (windowRoot) windowRoot.SetActive(false);
        if (detailPanel) detailPanel.Hide();

        // Column button bindings
        if (herbsButton)
            herbsButton.onClick.AddListener(() => SetCategory(PickUpItemCategory.Herbs));

        if (stonesButton)
            stonesButton.onClick.AddListener(() => SetCategory(PickUpItemCategory.Stones));

        if (mapsButton)
            mapsButton.onClick.AddListener(() => SetCategory(PickUpItemCategory.Maps));

        UpdateColumnHighlight();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (windowRoot && windowRoot.activeSelf)
                CloseUI();
            else
                OpenUI();
        }
    }

    void OpenUI()
    {
        if (windowRoot) windowRoot.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
        if (detailPanel) detailPanel.Hide();
    }

    void CloseUI()
    {
        if (windowRoot) windowRoot.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SetCategory(PickUpItemCategory cat)
    {
        currentCategory = cat;
        Refresh();
        if (detailPanel) detailPanel.Hide();
        UpdateColumnHighlight();
    }

    void UpdateColumnHighlight()
    {
        if (herbsButton)
            SetButtonColor(herbsButton, currentCategory == PickUpItemCategory.Herbs);

        if (stonesButton)
            SetButtonColor(stonesButton, currentCategory == PickUpItemCategory.Stones);

        if (mapsButton)
            SetButtonColor(mapsButton, currentCategory == PickUpItemCategory.Maps);
    }

    void SetButtonColor(Button btn, bool selected)
    {
        var img = btn.GetComponent<Image>();
        if (!img) return;
        img.color = selected ? selectedColor : normalColor;
    }

    public void Refresh()
    {
        if (!database || !contentRoot || !rowPrefab) return;

        foreach (var go in spawnedRows)
            if (go) Destroy(go);
        spawnedRows.Clear();

        foreach (var item in database.items)
        {
            if (!item) continue;
            if (item.category != currentCategory) continue;

            int count = StaticInventory.Count(item);
            if (count <= 0) continue;

            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(item, count, OnRowClicked);
            spawnedRows.Add(row.gameObject);
        }
    }

    void OnRowClicked(ItemData item)
    {
        if (detailPanel) detailPanel.Show(item);
    }
}
