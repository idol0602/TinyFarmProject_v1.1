using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;                 // icon hiển thị vật phẩm
    public TextMeshProUGUI amountText;     // hiển thị số lượng stack

    [Header("Slot Data")]
    public SlotData slotData;

    [Header("Detail Panel")]
    public ItemDetailPanel detailPanel;    // Reference tới detail panel

    private Button slotButton;             // Button component của slot

    private void Awake()
    {
        // Tìm Button component
        slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        // Tìm DetailPanel nếu chưa gán (tìm trong cùng Canvas)
        if (detailPanel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                detailPanel = canvas.GetComponentInChildren<ItemDetailPanel>();
            }
            
            // Nếu vẫn không tìm được, tìm trong toàn scene
            if (detailPanel == null)
            {
                detailPanel = FindObjectOfType<ItemDetailPanel>();
            }
        }
    }

    private void Start()
    {
        RefreshDisplay(); // load dữ liệu ban đầu
    }

    /// <summary>
    /// Gọi khi click vào slot
    /// </summary>
    public void OnSlotClicked()
    {
        if (slotData.item != null && detailPanel != null)
        {
            detailPanel.Show(slotData.item, slotData.quantity);
        }
        else if (detailPanel != null)
        {
            detailPanel.Hide();
        }
    }

    /// <summary>
    /// Gán item mới vào slot
    /// </summary>
    public void SetItem(ItemData newItem, int quantity = 1)
    {
        slotData.item = newItem;
        slotData.quantity = quantity;

        RefreshDisplay();
    }

    /// <summary>
    /// Xóa item khỏi slot
    /// </summary>
    public void Clear()
    {
        slotData.item = null;
        slotData.quantity = 0;

        RefreshDisplay();
    }

    /// <summary>
    /// Cập nhật UI theo dữ liệu slot
    /// </summary>
    private void RefreshDisplay()
    {
        if (slotData == null || slotData.item == null)
        {
            itemIcon.enabled = false;
            amountText.enabled = false;
            return;
        }

        // Hiển thị Icon
        itemIcon.sprite = slotData.item.icon;
        itemIcon.enabled = true;

        // Hiển thị số lượng stack nếu item stackable
        if (slotData.item.stackable && slotData.quantity > 1)
        {
            amountText.text = slotData.quantity.ToString();
            amountText.enabled = true;
        }
        else
        {
            amountText.enabled = false;
        }
    }

    /// <summary>
    /// Thêm item vào stack trong slot (nếu cùng type + subtype)
    /// </summary>
    public bool TryAddToStack(ItemData newItem)
    {
        // Kiểm tra item hiện tại và item mới
        if (slotData.item == null || newItem == null) return false;
        
        // Kiểm tra stackable
        if (!slotData.item.stackable || !newItem.stackable) return false;

        // ✅ Kiểm tra cùng ItemType và ItemSubtype
        if (slotData.item.itemType != newItem.itemType) return false;
        if (slotData.item.itemSubtype != newItem.itemSubtype) return false;

        // Kiểm tra chưa đầy stack
        if (slotData.quantity < slotData.item.maxStack)
        {
            slotData.quantity++;
            RefreshDisplay();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Public method để refresh UI từ bên ngoài
    /// </summary>
    public void Refresh()
    {
        RefreshDisplay();
    }
}
