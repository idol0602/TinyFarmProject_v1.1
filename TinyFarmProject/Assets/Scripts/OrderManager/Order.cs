using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Order
{
    public int id;
    public List<OrderItem> items = new List<OrderItem>();

    public int totalReward;      // Tiền thưởng khi hoàn thành
    public int deadlineDays;     // Số ngày còn lại
    public string content = "";  // Nội dung do AI tạo: "Ông chủ quán rượu cần 20 táo và 15 trứng gà để làm bánh!"

    public bool isAccepted = false;
    public bool isCompleted = false;

    // Tính tổng chi phí hạt giống toàn bộ đơn hàng
    public int TotalSeedCost()
    {
        int total = 0;
        foreach (var item in items)
            total += item.GetSeedCost();
        return total;
    }

    // Tạo nội dung dự phòng nếu AI lỗi hoặc chưa trả về
    public string GenerateFallbackContent()
    {
        if (items.Count == 0) return "Đơn hàng trống";

        string result = items[0].ToString();
        for (int i = 1; i < items.Count; i++)
        {
            if (i == items.Count - 1)
                result += " và " + items[i].ToString();
            else
                result += ", " + items[i].ToString();
        }
        return result;
    }
}