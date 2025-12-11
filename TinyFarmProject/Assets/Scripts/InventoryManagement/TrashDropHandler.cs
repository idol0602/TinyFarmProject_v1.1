using UnityEngine;
using UnityEngine.EventSystems;

public class TrashDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // Lấy draggable item từ drag icon
        DraggableItem draggable = eventData.pointerDrag?.GetComponent<DraggableItem>();
        if (draggable == null) return;

        // Lấy slot gốc từ DraggableItem (GetComponentInParent)
        InventorySlot originalSlot = draggable.GetComponentInParent<InventorySlot>();
        if (originalSlot == null) return;

        Debug.Log($"Xóa item {originalSlot.slotData.item?.itemName ?? "NULL"} từ slot");

        // Xóa item trong slot
        originalSlot.slotData.item = null;
        originalSlot.slotData.quantity = 0;

        // Refresh UI slot
        originalSlot.Refresh();

        // Refresh cả 2 inventory
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.RefreshInventoryUI();
            inventoryManager.RefreshSecondInventoryUI();
        }

        Debug.Log("Item deleted!");
    }
}
