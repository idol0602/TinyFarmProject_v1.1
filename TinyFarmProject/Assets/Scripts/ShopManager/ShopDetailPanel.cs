using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopDetailPanel : MonoBehaviour
{
    [Header("=== UI References ===")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI growTimeText;
    public TextMeshProUGUI sellPrice;
    public TextMeshProUGUI numsSeed;
    public Button buyButton;

    private SeedData currentSeed;

    public void Show(SeedData seed)
    {
        currentSeed = seed;

        icon.sprite = seed.seedIcon;
        nameText.text = seed.plantName;
        priceText.text = $"Price: {seed.price:N0}d";
        sellPrice.text = $"Sell price: {seed.priceToSell:N0}d";
        numsSeed.text = $"Quantity: {seed.numsSeed:N0}";


        // Hiển thị số ngày phát triển (làm tròn lên)
        int days = Mathf.CeilToInt(seed.growTime);
        growTimeText.text = days <= 0
            ? "Days: right now"
            : $"Days: {days} day";

        buyButton.gameObject.SetActive(true);
    }

    // Gọi khi nhấn nút Mua
    public void OnBuy()
    {
        if (currentSeed == null)
        {
            Debug.LogWarning("Không có hạt giống nào được chọn!");
            return;
        }

        // Dùng hệ thống tiền để trừ
        if (PlayerMoney.Instance.Subtract(currentSeed.price))
        {
            // MUA THÀNH CÔNG
            Debug.Log($"Mua thành công: {currentSeed.plantName} (-{currentSeed.price:N0}đ)");

            // TODO: Thêm hạt giống vào túi đồ / inventory
            // Ví dụ:
            // InventoryManager.Instance.AddSeed(currentSeed);

            // Có thể đóng panel hoặc làm hiệu ứng đẹp ở đây
        }
        else
        {
            // KHÔNG ĐỦ TIỀN
            Debug.Log("Không đủ tiền để mua!");
            // Gợi ý: Thêm hiệu ứng rung panel, hiện popup "Thiếu tiền", âm thanh "lỗi", v.v.
        }
    }
}