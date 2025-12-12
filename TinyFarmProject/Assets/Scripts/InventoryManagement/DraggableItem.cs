using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private InventorySlot originalSlot;
    private Canvas canvas;
    private Image image;
    private Transform originalParent;
    private int originalSiblingIndex;
    private CanvasGroup canvasGroup;  // Để ẩn/hiện slot gốc
    private InventoryManager inventoryManager;  // Reference tới InventoryManager

    private bool isDragging = false;  // Cờ để kiểm tra có đang drag không

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

    /// <summary>
    /// Xử lý click chuột
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Nếu không phải drag, trigger click event cho slot
        if (!isDragging)
        {
            InventorySlot slot = GetComponentInParent<InventorySlot>();
            if (slot != null)
            {
                slot.OnSlotClicked();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;  // Đánh dấu đang drag
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
        isDragging = false;  // Kết thúc drag
        image.raycastTarget = true;

        // Tìm target slot ở vị trí thả hiện tại
        InventorySlot targetSlot = FindSlotUnderMouse();

        Debug.Log($"OnEndDrag: originalSlot={originalSlot.name}, targetSlot={targetSlot?.name ?? "NULL"}");

        // Nếu thả vào slot khác
        if (targetSlot != null && targetSlot != originalSlot)
        {
            ItemData draggedItem = originalSlot.slotData.item;
            ItemData targetItem = targetSlot.slotData.item;
            int draggedQty = originalSlot.slotData.quantity;

            Debug.Log($"Dragged: {draggedItem?.itemName ?? "NULL"} (qty={draggedQty}), Target: {targetItem?.itemName ?? "EMPTY"}");

            // ⭐ Nếu target slot TRỐNG -> Kiểm tra inventory đã đầy chưa
            if (targetItem == null && draggedItem != null)
            {
                // Kiểm tra xem slot gốc có từ inventory nào không
                bool isFromFirstInventory = IsSlotInFirstInventory(originalSlot);
                bool isTargetInFirstInventory = IsSlotInFirstInventory(targetSlot);

                // Nếu kéo từ second sang first, và first đã đầy -> không move, đẩy vào second
                if (!isFromFirstInventory && isTargetInFirstInventory && IsFirstInventoryFull())
                {
                    Debug.Log("First inventory đầy, không move được");
                }
                // Nếu kéo từ first sang second, và first có chỗ -> move bình thường
                else if (isFromFirstInventory && !isTargetInFirstInventory)
                {
                    Debug.Log("MOVE: Chuyển item từ first vào second");
                    targetSlot.slotData.item = draggedItem;
                    targetSlot.slotData.quantity = draggedQty;

                    originalSlot.slotData.item = null;
                    originalSlot.slotData.quantity = 0;

                    targetSlot.Refresh();
                    originalSlot.Refresh();
                }
                // Nếu cả hai cùng inventory -> move bình thường
                else if (isFromFirstInventory == isTargetInFirstInventory)
                {
                    Debug.Log("MOVE: Chuyển item trong cùng inventory");
                    targetSlot.slotData.item = draggedItem;
                    targetSlot.slotData.quantity = draggedQty;

                    originalSlot.slotData.item = null;
                    originalSlot.slotData.quantity = 0;

                    targetSlot.Refresh();
                    originalSlot.Refresh();
                }
            }
            // ⭐ Nếu cùng type + subtype -> MERGE (cộng stack)
            else if (draggedItem != null && targetItem != null &&
                draggedItem.itemType == targetItem.itemType &&
                draggedItem.itemSubtype == targetItem.itemSubtype &&
                draggedItem.stackable && targetItem.stackable)
            {
                Debug.Log("MERGE: Cộng stack");
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
            // ⭐ Nếu khác type -> SWAP
            else if (draggedItem != null && targetItem != null)
            {
                Debug.Log("SWAP: Đổi 2 item");
                SwapSlots(originalSlot, targetSlot);
                targetSlot.Refresh();
                originalSlot.Refresh();
            }
        }
        else if (targetSlot == null && originalSlot != null && originalSlot.slotData.item != null)
        {
            // ⭐ Nếu không tìm được target slot bằng raycast, thử tìm slot trống trong inventory khác
            ItemData draggedItem = originalSlot.slotData.item;
            int draggedQty = originalSlot.slotData.quantity;
            bool isFromFirstInventory = IsSlotInFirstInventory(originalSlot);

            Debug.Log($"targetSlot=NULL: draggedItem={draggedItem.itemName}, isFromFirst={isFromFirstInventory}, thử tìm slot trống");

            // Nếu kéo từ second inventory, tìm slot trống của first inventory
            if (!isFromFirstInventory && inventoryManager != null)
            {
                InventorySlot emptySlotInFirst = FindEmptySlotInFirstInventory();
                if (emptySlotInFirst != null)
                {
                    Debug.Log("Tìm được slot trống trong first inventory, MOVE item vào");
                    emptySlotInFirst.slotData.item = draggedItem;
                    emptySlotInFirst.slotData.quantity = draggedQty;
                    
                    originalSlot.slotData.item = null;
                    originalSlot.slotData.quantity = 0;
                    
                    emptySlotInFirst.Refresh();
                    originalSlot.Refresh();
                }
            }
            // Nếu kéo từ first inventory, tìm slot trống của second inventory
            else if (isFromFirstInventory && inventoryManager != null)
            {
                InventorySlot emptySlotInSecond = FindEmptySlotInSecondInventory();
                if (emptySlotInSecond != null)
                {
                    Debug.Log("Tìm được slot trống trong second inventory, MOVE item vào");
                    emptySlotInSecond.slotData.item = draggedItem;
                    emptySlotInSecond.slotData.quantity = draggedQty;
                    
                    originalSlot.slotData.item = null;
                    originalSlot.slotData.quantity = 0;
                    
                    emptySlotInSecond.Refresh();
                    originalSlot.Refresh();
                }
            }
        }

        // ⭐ LUÔN refresh cả 2 inventory để đảm bảo UI cập nhật
        if (inventoryManager != null)
        {
            inventoryManager.RefreshInventoryUI();
            inventoryManager.RefreshSecondInventoryUI();
            
            // 🔧 SAVE lên Firebase sau khi thay đổi inventory
            if (FirebaseDatabaseManager.FirebaseReady)
            {
                Debug.Log("[DraggableItem] Saving inventory to Firebase after drag...");
                FirebaseDatabaseManager.Instance.SaveInventoryToFirebase("Player1");
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
    /// Kiểm tra slot có thuộc first inventory không
    /// </summary>
    private bool IsSlotInFirstInventory(InventorySlot slot)
    {
        if (inventoryManager == null) return true; // Mặc định là first inventory
        
        // Tìm xem slot này có trong inventoryPanel không
        return slot.transform.IsChildOf(inventoryManager.inventoryPanel);
    }

    /// <summary>
    /// Kiểm tra first inventory đã đầy chưa
    /// </summary>
    private bool IsFirstInventoryFull()
    {
        if (inventoryManager == null) return false;
        
        // Duyệt qua tất cả slot của first inventory, nếu tất cả có item -> đầy
        foreach (Transform child in inventoryManager.inventoryPanel)
        {
            InventorySlot slot = child.GetComponent<InventorySlot>();
            if (slot != null && slot.slotData.IsEmpty)
            {
                return false; // Còn slot trống
            }
        }
        return true; // Đầy
    }

    /// <summary>
    /// Tìm slot trống đầu tiên trong first inventory
    /// </summary>
    private InventorySlot FindEmptySlotInFirstInventory()
    {
        if (inventoryManager == null) return null;
        
        foreach (Transform child in inventoryManager.inventoryPanel)
        {
            InventorySlot slot = child.GetComponent<InventorySlot>();
            if (slot != null && slot.slotData.IsEmpty)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// Tìm slot trống đầu tiên trong second inventory
    /// </summary>
    private InventorySlot FindEmptySlotInSecondInventory()
    {
        if (inventoryManager == null || inventoryManager.secondInventoryPanel == null) return null;
        
        foreach (Transform child in inventoryManager.secondInventoryPanel)
        {
            InventorySlot slot = child.GetComponent<InventorySlot>();
            if (slot != null && slot.slotData.IsEmpty)
            {
                return slot;
            }
        }
        return null;
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
