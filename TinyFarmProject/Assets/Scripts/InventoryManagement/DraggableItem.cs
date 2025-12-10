using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventorySlot originalSlot;
    private Canvas canvas;
    private Image image;
    private Transform originalParent;
    private int originalSiblingIndex;

    private void Awake()
    {
        image = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalSlot = GetComponentInParent<InventorySlot>();
        
        // ⭐ Lưu parent gốc và vị trí trong hierarchy
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Tạm thời chuyển lên canvas để render trên cùng khi drag
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.raycastTarget = true;

        // ⭐ FIX: Tìm targetSlot từ gameObject được raycast (có thể là child của slot)
        InventorySlot targetSlot = null;
        
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            // Thử lấy InventorySlot từ chính object đó hoặc từ parent của nó
            targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<InventorySlot>();
        }

        // ⭐ LUÔN LUÔN trả icon về đúng parent ban đầu
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.localPosition = Vector3.zero;

        // Nếu thả vào slot khác -> swap data
        if (targetSlot != null && targetSlot != originalSlot)
        {
            if (CanSwapOrMerge(originalSlot, targetSlot))
            {
                SwapData(originalSlot, targetSlot);
                
                // Refresh cả 2 slot để cập nhật UI
                targetSlot.Refresh();
                originalSlot.Refresh();
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem có thể swap hoặc merge 2 slot không
    /// </summary>
    private bool CanSwapOrMerge(InventorySlot slotA, InventorySlot slotB)
    {
        // Nếu 1 trong 2 slot rỗng -> cho phép swap
        if (slotA.slotData.IsEmpty || slotB.slotData.IsEmpty)
            return true;

        // Nếu cả 2 đều có item
        ItemData itemA = slotA.slotData.item;
        ItemData itemB = slotB.slotData.item;

        // ✅ Nếu cùng type + subtype + stackable -> cho phép merge
        if (itemA.itemType == itemB.itemType &&
            itemA.itemSubtype == itemB.itemSubtype &&
            itemA.stackable && itemB.stackable)
        {
            return true;
        }

        // ✅ Khác type hoặc subtype -> chỉ cho swap (không merge)
        // Tools không stack với Seeds, Crops không stack với Seeds, etc.
        return true; // Vẫn cho swap vị trí
    }

    private void SwapData(InventorySlot a, InventorySlot b)
    {
        // ✅ Nếu cùng item type + subtype và stackable -> thử merge
        if (!a.slotData.IsEmpty && !b.slotData.IsEmpty)
        {
            ItemData itemA = a.slotData.item;
            ItemData itemB = b.slotData.item;

            if (itemA.itemType == itemB.itemType &&
                itemA.itemSubtype == itemB.itemSubtype &&
                itemA.stackable && itemB.stackable)
            {
                // Merge vào slot B
                int totalQty = a.slotData.quantity + b.slotData.quantity;

                if (totalQty <= itemB.maxStack)
                {
                    // Merge hết vào B, xóa A
                    b.slotData.quantity = totalQty;
                    a.slotData.item = null;
                    a.slotData.quantity = 0;
                    return;
                }
                else
                {
                    // B đầy, A giữ phần dư
                    b.slotData.quantity = itemB.maxStack;
                    a.slotData.quantity = totalQty - itemB.maxStack;
                    return;
                }
            }
        }

        // Swap thông thường nếu không merge được
        var tempItem = a.slotData.item;
        var tempQty = a.slotData.quantity;

        a.slotData.item = b.slotData.item;
        a.slotData.quantity = b.slotData.quantity;

        b.slotData.item = tempItem;
        b.slotData.quantity = tempQty;
    }
}
