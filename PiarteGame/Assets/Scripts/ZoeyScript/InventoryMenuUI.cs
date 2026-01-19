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
    public Button mapsButton; // optional

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

    // ✅ UPDATED: Support multiple UI roots to hide while inventory is open
    [Header("Optional UI Lock (Hide While Inventory Open)")]
    [Tooltip("Drag any number of UI roots/Canvases you want to disable when the inventory is open.")]
    public List<GameObject> uiToDisableWhenInventoryOpen = new List<GameObject>();

    // ✅ Cache initial active states for correct restore
    private readonly Dictionary<GameObject, bool> initialUIActiveStates = new Dictionary<GameObject, bool>();

    PickUpItemCategory currentCategory = PickUpItemCategory.Herbs;
    readonly List<GameObject> spawnedRows = new List<GameObject>();

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

        // ✅ Initialize initial items
        StaticInventory.InitializeFromDatabase(database);

        // ✅ Cache initial active states for all UI roots
        CacheInitialUIStates();

        // Ensure detail panel has inspect manager
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

    void CacheInitialUIStates()
    {
        initialUIActiveStates.Clear();

        if (uiToDisableWhenInventoryOpen == null) return;

        for (int i = 0; i < uiToDisableWhenInventoryOpen.Count; i++)
        {
            var go = uiToDisableWhenInventoryOpen[i];
            if (!go) continue;

            // Avoid duplicates
            if (!initialUIActiveStates.ContainsKey(go))
                initialUIActiveStates.Add(go, go.activeSelf);
        }
    }

    void SetOtherUIActive(bool active)
    {
        if (uiToDisableWhenInventoryOpen == null) return;

        for (int i = 0; i < uiToDisableWhenInventoryOpen.Count; i++)
        {
            var go = uiToDisableWhenInventoryOpen[i];
            if (!go) continue;

            if (active)
            {
                // Restore only if it was originally active
                if (initialUIActiveStates.TryGetValue(go, out bool wasActive) && wasActive)
                    go.SetActive(true);
            }
            else
            {
                // Disable regardless (if it's already off, no harm)
                go.SetActive(false);
            }
        }
    }

    void OpenUI()
    {
        if (windowRoot) windowRoot.SetActive(true);

        // ✅ Hide multiple UI roots while inventory is open
        SetOtherUIActive(false);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
        if (detailPanel) detailPanel.Hide();
    }

    void CloseUI()
    {
        if (windowRoot) windowRoot.SetActive(false);

        if (inspectManager) inspectManager.Hide();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (detailPanel) detailPanel.Hide();

        // ✅ Restore multiple UI roots to original state
        SetOtherUIActive(true);
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

        for (int i = 0; i < spawnedRows.Count; i++)
            if (spawnedRows[i]) Destroy(spawnedRows[i]);
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
