using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    private ItemData itemData;
    private int quantity;

    /// <summary>
    /// Thiết lập hiển thị item và số lượng
    /// </summary>
    public void Setup(ItemData item, int qty)
    {
        itemData = item;
        quantity = qty;

        // Hiển thị icon
        if (itemIcon != null && item != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
        }

        // Hiển thị số lượng
        if (quantityText != null)
        {
            if (item != null && item.stackable && qty > 1)
            {
                quantityText.text = qty.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
    }

    /// <summary>
    /// Lấy dữ liệu item
    /// </summary>
    public ItemData GetItemData()
    {
        return itemData;
    }

    /// <summary>
    /// Lấy số lượng
    /// </summary>
    public int GetQuantity()
    {
        return quantity;
    }
}
