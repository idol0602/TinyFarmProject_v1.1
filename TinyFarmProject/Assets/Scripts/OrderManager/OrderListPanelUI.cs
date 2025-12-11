using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderListPanelUI : MonoBehaviour
{
    public Transform contentParent;
    public OrderTabUI tabPrefab;
    public OrderDetailUI detailUI;

    [Header("Button Clear All")]
    public Button clearAllBtn;   // 👈 Gán vào Inspector

    private Dictionary<int, OrderTabUI> tabMap = new();

    private void Start()
    {
        OrderManager.Instance.onOrderAdded.AddListener(AddTab);
        OrderManager.Instance.onOrderRemoved.AddListener(RemoveTab);

        // Gán sự kiện click
        if (clearAllBtn != null)
            clearAllBtn.onClick.AddListener(ClearAll);

        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        tabMap.Clear();

        foreach (var order in OrderManager.Instance.GetAllActiveOrders())
            AddTab(order);
    }

    private void AddTab(Order order)
    {
        if (tabMap.ContainsKey(order.id)) return;

        var tab = Instantiate(tabPrefab, contentParent);
        tab.Setup(order, detailUI);
        tabMap.Add(order.id, tab);
    }

    private void RemoveTab(Order order)
    {
        if (!tabMap.ContainsKey(order.id)) return;

        Destroy(tabMap[order.id].gameObject);
        tabMap.Remove(order.id);
    }

    // 👇 Hàm xóa tất cả
    public void ClearAll()
    {
        // Xóa trong UI
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        tabMap.Clear();

        // Xóa trong OrderManager
        OrderManager.Instance.ClearAllOrders();

        Debug.Log("<color=red>Đã xóa toàn bộ đơn hàng!</color>");
    }
}
