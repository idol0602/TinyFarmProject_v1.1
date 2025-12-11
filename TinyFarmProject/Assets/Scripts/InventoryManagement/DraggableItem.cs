using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventorySlot originalSlot;
    private Canvas canvas;
    private Image image;
    private Transform originalParent;
    private int originalSiblingIndex;
    private CanvasGroup canvasGroup;  // Để ẩn/hiện slot gốc
    private InventoryManager inventoryManager;  // Reference tới InventoryManager

    private void Awake()
    {
        image = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Tìm InventoryManager trong scene
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalSlot = GetComponentInParent<InventorySlot>();
        
        // ⭐ Lưu parent gốc và vị trí trong hierarchy
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // ⭐ Ẩn slot gốc nhưng vẫn giữ lại vị trí
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;

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

        // Tìm target slot ở vị trí thả hiện tại
        InventorySlot targetSlot = FindSlotUnderMouse();

        // Nếu thả vào slot khác
        if (targetSlot != null && targetSlot != originalSlot)
        {
            ItemData draggedItem = originalSlot.slotData.item;
            ItemData targetItem = targetSlot.slotData.item;
            int draggedQty = originalSlot.slotData.quantity;

            // ⭐ Nếu cùng type + subtype -> MERGE (cộng stack)
            if (draggedItem != null && targetItem != null &&
                draggedItem.itemType == targetItem.itemType &&
                draggedItem.itemSubtype == targetItem.itemSubtype &&
                draggedItem.stackable && targetItem.stackable)
            {
                // Cộng số lượng
                int availableSpace = targetItem.maxStack - targetSlot.slotData.quantity;
                int amountToAdd = Mathf.Min(availableSpace, draggedQty);

                targetSlot.slotData.quantity += amountToAdd;
                originalSlot.slotData.quantity -= amountToAdd;

                // Nếu hết item ở slot gốc thì xóa
                if (originalSlot.slotData.quantity <= 0)
                {
                    originalSlot.slotData.item = null;
                    originalSlot.slotData.quantity = 0;
                }

                targetSlot.Refresh();
                originalSlot.Refresh();
            }
            // ⭐ Nếu khác type hoặc một trong hai null -> SWAP
            else
            {
                SwapSlots(originalSlot, targetSlot);
                targetSlot.Refresh();
                originalSlot.Refresh();
            }

            // ⭐ Refresh cả 2 inventory nếu có InventoryManager
            if (inventoryManager != null)
            {
                inventoryManager.RefreshInventoryUI();
                inventoryManager.RefreshSecondInventoryUI();
            }
        }

        // ⭐ Hiển thị lại slot gốc
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // ⭐ LUÔN LUÔN trả icon về đúng parent ban đầu
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Tìm slot nằm dưới vị trí con chuột hiện tại
    /// </summary>
    private InventorySlot FindSlotUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Tìm InventorySlot đầu tiên trong kết quả raycast
        foreach (RaycastResult result in results)
        {
            InventorySlot slot = result.gameObject.GetComponentInParent<InventorySlot>();
            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Swap dữ liệu giữa 2 slot (chỉ đổi item + quantity, giữ nguyên vị trí)
    /// </summary>
    private void SwapSlots(InventorySlot slotA, InventorySlot slotB)
    {
        // Swap dữ liệu
        ItemData tempItem = slotA.slotData.item;
        int tempQty = slotA.slotData.quantity;
        int tempIndex = slotA.slotData.slotIndex;

        slotA.slotData.item = slotB.slotData.item;
        slotA.slotData.quantity = slotB.slotData.quantity;
        slotA.slotData.slotIndex = slotB.slotData.slotIndex;

        slotB.slotData.item = tempItem;
        slotB.slotData.quantity = tempQty;
        slotB.slotData.slotIndex = tempIndex;

        // ⭐ KHÔNG SWAP SIBLING INDEX - chỉ để dữ liệu đổi chỗ
    }
}
