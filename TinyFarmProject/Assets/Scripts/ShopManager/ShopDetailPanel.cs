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
    private InventoryManager inventoryManager;

    private void Start()
    {
        // Lấy InventoryManager từ scene
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
        }
    }

    private InventoryManager GetInventoryManager()
    {
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
        }
        return inventoryManager;
    }

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

        Debug.Log($"Mua: {currentSeed.plantName}");

        // ✅ Quy trình: SeedData → ItemData → Add to SecondInventory
        InventoryManager invManager = GetInventoryManager();
        if (invManager != null)
        {
            // 1. Convert SeedData → ItemData
            ItemData itemData = SeedToItemConverter.ConvertSeedToItem(currentSeed);

            // 2. Thêm ItemData vào SecondInventory (inventory dưới)
            bool success = invManager.AddItemToSecond(itemData, 1);
            
            if (success)
            {
                Debug.Log($"✅ Thêm {currentSeed.plantName} vào second inventory thành công");
            }
            else
            {
                Debug.LogWarning($"⚠️ Second inventory đầy, không thể thêm {currentSeed.plantName}!");
            }
        }
        else
        {
            Debug.LogError("❌ InventoryManager không được gán!");
        }
    }
}