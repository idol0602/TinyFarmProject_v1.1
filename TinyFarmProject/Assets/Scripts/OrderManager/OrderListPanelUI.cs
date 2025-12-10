using System.Collections.Generic;
using UnityEngine;

public class OrderListPanelUI : MonoBehaviour
{
    public Transform contentParent;
    public OrderTabUI tabPrefab;
    public OrderDetailUI detailUI;

    private Dictionary<int, OrderTabUI> tabMap = new();

    private void Start()
    {
        OrderManager.Instance.onOrderAdded.AddListener(AddTab);
        OrderManager.Instance.onOrderRemoved.AddListener(RemoveTab);

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
}
