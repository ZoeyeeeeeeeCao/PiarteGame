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

    [Header("Inspect (optional but recommended)")]
    public InspectManager inspectManager;

    // ⭐ NEW: List of UI roots to hide while inventory is open
    [Header("Optional UI Lock (Hide While Inventory Open)")]
    [Tooltip("Any UI roots/canvases you want disabled when inventory opens (HUD, minimap, prompts, etc).")]
    public List<GameObject> uiToDisableWhenInventoryOpen = new();

    // ⭐ NEW: Cache each UI's previous active state so we restore correctly
    private bool[] _uiPrevActiveStates;

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

        // ✅ 初始化初始量（你之前要求的）
        StaticInventory.InitializeFromDatabase(database);

        // Make sure detail panel has the inspect manager reference
        if (detailPanel && detailPanel.inspectManager == null)
            detailPanel.inspectManager = inspectManager;

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

        HideBlockedUI();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
        if (detailPanel) detailPanel.Hide();
    }

    void CloseUI()
    {
        if (windowRoot) windowRoot.SetActive(false);

        // Stop inspection if it was open
        if (inspectManager) inspectManager.Hide();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (detailPanel) detailPanel.Hide();

        RestoreBlockedUI();
    }

    // =========================
    // Hide / Restore list of UI
    // =========================
    private void HideBlockedUI()
    {
        if (uiToDisableWhenInventoryOpen == null || uiToDisableWhenInventoryOpen.Count == 0)
            return;

        _uiPrevActiveStates = new bool[uiToDisableWhenInventoryOpen.Count];

        for (int i = 0; i < uiToDisableWhenInventoryOpen.Count; i++)
        {
            var go = uiToDisableWhenInventoryOpen[i];
            if (go == null)
            {
                _uiPrevActiveStates[i] = false;
                continue;
            }

            _uiPrevActiveStates[i] = go.activeSelf;

            // Only disable if it was enabled
            if (go.activeSelf)
                go.SetActive(false);
        }
    }

    private void RestoreBlockedUI()
    {
        if (uiToDisableWhenInventoryOpen == null || uiToDisableWhenInventoryOpen.Count == 0)
            return;

        // If not cached (or list size changed), safest fallback: enable them
        if (_uiPrevActiveStates == null || _uiPrevActiveStates.Length != uiToDisableWhenInventoryOpen.Count)
        {
            for (int i = 0; i < uiToDisableWhenInventoryOpen.Count; i++)
            {
                var go = uiToDisableWhenInventoryOpen[i];
                if (go != null) go.SetActive(true);
            }
            return;
        }

        for (int i = 0; i < uiToDisableWhenInventoryOpen.Count; i++)
        {
            var go = uiToDisableWhenInventoryOpen[i];
            if (go == null) continue;

            // Restore exactly what it was before inventory opened
            go.SetActive(_uiPrevActiveStates[i]);
        }
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
