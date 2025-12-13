using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấu trúc dữ liệu Order để lưu lên Firebase
/// Tương ứng với cấu trúc ảnh: cropID, cropType, stage, isDead, lastWaterDay, isWateredToday
/// Nhưng sử dụng để lưu Order data
/// </summary>
[System.Serializable]
public class OrderFirebaseData
{
    public int id;
    public int deadlineDays;
    public int totalReward;
    public string content = "";
    public List<OrderItemData> items = new List<OrderItemData>();
    public bool isAccepted = false;
    public bool isCompleted = false;
    public bool isTestOrder = false;

    // Constructor rỗng cho JSON deserialization
    public OrderFirebaseData() { }

    // Constructor từ Order object
    public OrderFirebaseData(Order order)
    {
        if (order == null)
            return;

        id = order.id;
        deadlineDays = order.deadlineDays;
        totalReward = order.totalReward;
        content = order.content ?? "";
        isAccepted = order.isAccepted;
        isCompleted = order.isCompleted;
        isTestOrder = order.isTestOrder;

        // Convert OrderItem to OrderItemData
        foreach (var item in order.items)
        {
            if (item != null && item.product != null)
            {
                items.Add(new OrderItemData
                {
                    productName = item.product.plant_name,
                    quantity = item.quantity,
                    seedCost = item.product.seedCost
                });
            }
        }
    }

    /// <summary>
    /// Convert từ OrderFirebaseData về Order object
    /// </summary>
    public Order ToOrder(ProductDatabase productDatabase)
    {
        if (productDatabase == null)
        {
            Debug.LogWarning("[OrderFirebaseData] ProductDatabase is null, cannot convert to Order");
            return null;
        }

        Order order = new Order
        {
            id = id,
            deadlineDays = deadlineDays,
            totalReward = totalReward,
            content = content,
            isAccepted = isAccepted,
            isCompleted = isCompleted,
            isTestOrder = isTestOrder
        };

        // Convert OrderItemData back to OrderItem
        foreach (var itemData in items)
        {
            // Tìm ProductData theo tên
            ProductData product = null;
            foreach (var p in productDatabase.products)
            {
                if (p.plant_name == itemData.productName)
                {
                    product = p;
                    break;
                }
            }

            if (product != null)
            {
                order.items.Add(new OrderItem(product, itemData.quantity));
            }
            else
            {
                Debug.LogWarning($"[OrderFirebaseData] Không tìm thấy product: {itemData.productName}");
            }
        }

        return order;
    }
}

/// <summary>
/// Dữ liệu 1 item trong order để lưu Firebase
/// </summary>
[System.Serializable]
public class OrderItemData
{
    public string productName;
    public int quantity;
    public int seedCost;

    public OrderItemData() { }

    public OrderItemData(string productName, int quantity, int seedCost)
    {
        this.productName = productName;
        this.quantity = quantity;
        this.seedCost = seedCost;
    }
}
