using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject slotPrefabPanelBackground; // prefab slot UI
    public Transform inventoryPanel;              // grid container

    [Header("Inventory Settings")]
    public int inventorySize = 20;

    [System.Serializable]
    public class InventoryItem
    {
        public ItemData item;
        public int quantity;
    }

    [Header("Initial Inventory Setup")]
    public InventoryItem[] slotSlot;

    private SlotData[] slotDataList;
    private InventorySlot[] uiSlots;

    /// <summary>
    /// Kiểm tra xem slot có thể stack với item mới không (check type + subtype)
    /// </summary>
    private bool CanStack(SlotData slot, ItemData newItem)
    {
        if (slot.item == null) return false;
        if (!slot.item.stackable || !newItem.stackable) return false;

        // Chỉ cho stack nếu cùng type + cùng subtype
        if (slot.item.itemType != newItem.itemType) return false;
        if (slot.item.itemSubtype != newItem.itemSubtype) return false;

        return slot.quantity < newItem.maxStack;
    }

    private void Awake()
    {
        InitInventory();
    }

    private void InitInventory()
    {
        slotDataList = new SlotData[inventorySize];
        uiSlots = new InventorySlot[inventorySize];

        for (int i = 0; i < inventorySize; i++)
        {
            // tạo dữ liệu slot
            slotDataList[i] = new SlotData();

            // tạo UI slot và gán component
            var obj = Instantiate(slotPrefabPanelBackground, inventoryPanel);
            InventorySlot slotUI = obj.GetComponent<InventorySlot>();

            slotUI.slotData = slotDataList[i];  // liên kết Data ↔ UI
            uiSlots[i] = slotUI;                 // lưu UI reference
        }

        // load dữ liệu Inspector slotSlot vào SlotData
        for (int i = 0; i < slotSlot.Length && i < inventorySize; i++)
        {
            if (slotSlot[i] != null && slotSlot[i].item != null)
            {
                AddItem(slotSlot[i].item, slotSlot[i].quantity);
            }
        }

        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].Refresh();
        }
    }

    public bool AddItem(ItemData item, int qty = 1)
    {
        // stack slot nếu có item cùng type và subtype
        if (item.stackable)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                // ✅ Sử dụng CanStack để kiểm tra type + subtype
                if (CanStack(slotDataList[i], item))
                {
                    int space = item.maxStack - slotDataList[i].quantity;
                    int addAmount = Mathf.Min(space, qty);

                    slotDataList[i].quantity += addAmount;
                    qty -= addAmount;

                    uiSlots[i].Refresh();

                    if (qty <= 0)
                        return true;
                }
            }
        }

        // tìm slot trống
        for (int i = 0; i < inventorySize; i++)
        {
            if (slotDataList[i].IsEmpty)
            {
                slotDataList[i].item = item;
                slotDataList[i].quantity = qty;

                uiSlots[i].Refresh();
                return true;
            }
        }

        Debug.Log("Inventory FULL");
        return false;
    }
}
