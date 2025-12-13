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

        Debug.Log($"[Shop] OnBuy called - Purchasing: {currentSeed.plantName}");

        // Lấy số tiền
        int price = currentSeed.price;

        // Kiểm tra đủ tiền không
        if (!PlayerMoney.Instance.Subtract(price))
        {
            Debug.LogWarning("❌ Không đủ tiền để mua!");
            return;
        }

        Debug.Log($"💰 Đã trừ {price}đ. Tiền còn lại: {PlayerMoney.Instance.GetCurrentMoney()}");

        // Convert SeedData → ItemData
        InventoryManager inv = InventoryManager.Instance;
        if (inv != null)
        {
            Debug.Log($"[Shop] InventoryManager found: {inv.name}");
            
            ItemData itemData = SeedToItemConverter.ConvertSeedToItem(currentSeed);
            Debug.Log($"[Shop] Converted to ItemData: {itemData.itemName}");
            
            bool success = inv.AddItemToSecond(itemData, currentSeed.numsSeed);

            if (success)
            {
                Debug.Log($"✅ Đã mua {currentSeed.plantName} và thêm vào Second Inventory");
                
                // ✅ LƯU INVENTORY NGAY SAU KHI MUA
                Debug.Log($"[Shop] Firebase ready: {FirebaseDatabaseManager.FirebaseReady}");
                
                if (FirebaseDatabaseManager.FirebaseReady)
                {
                    Debug.Log("[Shop] Saving inventory to Firebase after purchase...");
                    FirebaseDatabaseManager.Instance.SaveInventoryToFirebase("Player1");
                }
                else
                {
                    Debug.LogWarning("[Shop] Firebase NOT ready, inventory won't be saved immediately");
                    Debug.LogWarning($"[Shop] FirebaseReady={FirebaseDatabaseManager.FirebaseReady}, Instance={FirebaseDatabaseManager.Instance}");
                }
            }
            else
            {
                Debug.LogWarning("⚠ Inventory đầy → hoàn tiền lại");
                PlayerMoney.Instance.Add(price);   // Hoàn tiền nếu add item fail
                
                // ✅ LƯU TIỀN KHI HOÀN LẠI (vì PlayerMoney.Add() sẽ auto-save)
            }
        }
        else
        {
            Debug.LogError("[Shop] InventoryManager NOT found!");
        }
    }

}