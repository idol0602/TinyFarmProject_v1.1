using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Order
{
    public int id;
    public int deadlineDays;
    public int totalReward;
    public string content = "";
    public List<OrderItem> items = new List<OrderItem>();

    // THÊM CÁC FIELD NÀY
    public bool isAccepted = false;   // đã nhận đơn chưa
    public bool isCompleted = false;  // đã giao xong chưa
    public bool isTestOrder = false;  // đây có phải order test không
    public float remainingTime;       // thời gian còn lại (nếu bạn có hệ thống đếm ngược)

    // Hàm tiện ích (tùy chọn)
    public void Accept() => isAccepted = true;
    public void Complete() => isCompleted = true;

    public string GetItemListString()
    {
        // ... code cũ của bạn giữ nguyên
        if (items.Count == 0) return "";
        List<string> list = new List<string>();
        foreach (var item in items)
            list.Add($"{item.quantity} {item.product.plant_name}");

        if (list.Count == 1) return list[0];
        if (list.Count == 2) return $"{list[0]} và {list[1]}";

        string result = string.Join(", ", list.Take(list.Count - 1));
        return result + " và " + list[^1];
    }

    public string GenerateFallbackContent()
    {
        string[] texts = {
            "Khách đang sốt ruột chờ đơn hàng đây!",
            "Giao nhanh kẻo họ giận đó nha!",
            "Đơn VIP nè, làm tốt có thưởng thêm!"
        };
        return texts[Random.Range(0, texts.Length)];
    }
}