using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI detailText;      // Hiển thị chi tiết (giá bán, thời gian grow, v.v.)
    public TextMeshProUGUI subTypeText;     // Hiển thị loại item (subtype)
    public TextMeshProUGUI quantityText;    // Hiển thị số lượng

    // Ẩn panel khi start
    private void Start()
    {
        Hide();
    }

    // Hàm show chi tiết item
    public void Show(ItemData item, int quantity)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        // Hiển thị thông tin item
        icon.sprite = item.icon;
        nameText.text = item.itemName;
        
        // Hiển thị chi tiết dựa trên loại item
        if (detailText != null)
        {
            string detail = "";
            string subtypeStr = item.itemSubtype.ToString();
            
            // Check xem subtype có chứa "Crop" không
            if (subtypeStr.Contains("Crop"))
            {
                detail = $"Giá bán: {item.sellPrice}g";
            }
            // Check xem subtype có chứa "Seed" không
            else if (subtypeStr.Contains("Seed"))
            {
                detail = $"Thời gian trồng: {item.growTimeDays} ngày";
            }
            else
            {
                detail = $"Loại: {item.itemType}";
            }
            
            detailText.text = detail;
        }
        
        // Hiển thị số lượng
        if (quantityText != null)
        {
            quantityText.text = $"Số lượng: {quantity}";
        }

        // Hiển thị loại item (subtype)
        if (subTypeText != null)
        {
            subTypeText.text = $"{item.itemSubtype}";
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
