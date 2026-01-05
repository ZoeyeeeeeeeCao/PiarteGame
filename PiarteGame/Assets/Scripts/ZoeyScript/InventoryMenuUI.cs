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

    [Header("Columns (Clickable Tabs)")]
    public Button herbsButton;
    public Button stonesButton;
    public Button mapsButton; // 如果你暂时不要Maps，可以不拖，不会报错

    [Header("List")]
    public Transform contentRoot;          // ScrollView/Viewport/Content
    public InventoryRowButton rowPrefab;

    [Header("Detail")]
    public InventoryDetailPanel detailPanel;

    private PickUpItemCategory currentCategory = PickUpItemCategory.Herbs;
    private readonly List<GameObject> spawnedRows = new();

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

        if (herbsButton)
        {
            herbsButton.onClick.RemoveAllListeners();
            herbsButton.onClick.AddListener(() => SetCategory(PickUpItemCategory.Herbs));
        }

        if (stonesButton)
        {
            stonesButton.onClick.RemoveAllListeners();
            stonesButton.onClick.AddListener(() => SetCategory(PickUpItemCategory.Stones));
        }

        if (mapsButton)
        {
            mapsButton.onClick.RemoveAllListeners();
            mapsButton.onClick.AddListener(() => SetCategory(PickUpItemCategory.Maps));
        }

        Refresh();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool next = !(windowRoot && windowRoot.activeSelf);
            if (windowRoot) windowRoot.SetActive(next);

            if (next)
            {
                Refresh();
                if (detailPanel) detailPanel.Hide();
            }
        }
    }

    void SetCategory(PickUpItemCategory cat)
    {
        currentCategory = cat;
        Refresh();
        if (detailPanel) detailPanel.Hide();
        // 这里之后你想做“高亮当前column”，我也可以帮你补
    }

    public void Refresh()
    {
        if (!database || !contentRoot || !rowPrefab) return;

        // clear
        for (int i = 0; i < spawnedRows.Count; i++)
            if (spawnedRows[i]) Destroy(spawnedRows[i]);
        spawnedRows.Clear();

        // build list (only owned)
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

