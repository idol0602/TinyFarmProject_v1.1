using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Database")]
    public SeedDatabase seedDatabase;

    [Header("Grid Shop")]
    public Transform gridParent;              // Parent chứa các icon ở giữa
    public GameObject gridItemPrefab;         // Prefab icon

    [Header("Detail Panel")]
    public ShopDetailPanel detailPanel;       // Kéo Panel phải vào đây

    void Start()
    {
        LoadShopGrid();
    }

    void LoadShopGrid()
    {
        foreach (var seed in seedDatabase.seeds)
        {
            GameObject obj = Instantiate(gridItemPrefab, gridParent);

            ShopGridItemUI itemUI = obj.GetComponent<ShopGridItemUI>();
            itemUI.Setup(seed, this);
        }

        // ✅ Tự động chọn item đầu tiên
        if (seedDatabase.seeds.Count > 0)
            ShowDetail(seedDatabase.seeds[0]);
    }

    public void ShowDetail(SeedData seed)
    {
        detailPanel.Show(seed);
    }
}
