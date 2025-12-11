using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject slotPrefabPanelBackground; // prefab slot UI (inventory 1)
    public Transform inventoryPanel;              // grid container (trên)
    
    public GameObject secondSlotPrefab;           // prefab slot UI khác (inventory 2) - tùy chọn
    public Transform secondInventoryPanel;        // grid container thứ 2 (dưới, ngang)

    [Header("Inventory Settings")]
    public int inventorySize = 20;
    public int secondInventorySize = 20;          // kích thước inventory thứ 2

    [System.Serializable]
    public class InventoryItem
    {
        public ItemData item;
        public int quantity;
    }

    [Header("Initial Inventory Setup")]
    public InventoryItem[] slotSlot;
    public InventoryItem[] secondSlotSlot;        // dữ liệu khởi tạo cho inventory thứ 2

    private SlotData[] slotDataList;
    private InventorySlot[] uiSlots;
    
    private SlotData[] secondSlotDataList;        // dữ liệu slot inventory thứ 2
    private InventorySlot[] secondUiSlots;        // UI reference inventory thứ 2

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
        InitSecondInventory();
    }

    private void InitInventory()
    {
        slotDataList = new SlotData[inventorySize];
        uiSlots = new InventorySlot[inventorySize];

        for (int i = 0; i < inventorySize; i++)
        {
            // tạo dữ liệu slot
            slotDataList[i] = new SlotData();
            slotDataList[i].slotIndex = i;  // Gán vị trí slot

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

    private void InitSecondInventory()
    {
        if (secondInventoryPanel == null)
        {
            Debug.LogWarning("Second Inventory Panel not assigned!");
            return;
        }

        // Nếu không gán secondSlotPrefab, dùng lại slotPrefabPanelBackground
        GameObject slotPrefabToUse = secondSlotPrefab != null ? secondSlotPrefab : slotPrefabPanelBackground;

        secondSlotDataList = new SlotData[secondInventorySize];
        secondUiSlots = new InventorySlot[secondInventorySize];

        for (int i = 0; i < secondInventorySize; i++)
        {
            // tạo dữ liệu slot
            secondSlotDataList[i] = new SlotData();
            secondSlotDataList[i].slotIndex = i;

            // tạo UI slot và gán component (dùng prefab khác hoặc mặc định)
            var obj = Instantiate(slotPrefabToUse, secondInventoryPanel);
            InventorySlot slotUI = obj.GetComponent<InventorySlot>();

            slotUI.slotData = secondSlotDataList[i];
            secondUiSlots[i] = slotUI;
        }

        // load dữ liệu Inspector cho inventory thứ 2
        if (secondSlotSlot != null)
        {
            for (int i = 0; i < secondSlotSlot.Length && i < secondInventorySize; i++)
            {
                if (secondSlotSlot[i] != null && secondSlotSlot[i].item != null)
                {
                    AddItemToSecond(secondSlotSlot[i].item, secondSlotSlot[i].quantity);
                }
            }
        }

        RefreshSecondInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].Refresh();
        }
    }

    public void RefreshSecondInventoryUI()
    {
        if (secondUiSlots == null) return;
        
        for (int i = 0; i < secondUiSlots.Length; i++)
        {
            secondUiSlots[i].Refresh();
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

    public bool AddItemToSecond(ItemData item, int qty = 1)
    {
        if (secondSlotDataList == null)
        {
            Debug.LogWarning("Second inventory not initialized!");
            return false;
        }

        // stack slot nếu có item cùng type và subtype
        if (item.stackable)
        {
            for (int i = 0; i < secondInventorySize; i++)
            {
                if (CanStack(secondSlotDataList[i], item))
                {
                    int space = item.maxStack - secondSlotDataList[i].quantity;
                    int addAmount = Mathf.Min(space, qty);

                    secondSlotDataList[i].quantity += addAmount;
                    qty -= addAmount;

                    secondUiSlots[i].Refresh();

                    if (qty <= 0)
                        return true;
                }
            }
        }

        // tìm slot trống
        for (int i = 0; i < secondInventorySize; i++)
        {
            if (secondSlotDataList[i].IsEmpty)
            {
                secondSlotDataList[i].item = item;
                secondSlotDataList[i].quantity = qty;

                secondUiSlots[i].Refresh();
                return true;
            }
        }

        Debug.Log("Second Inventory FULL");
        return false;
    }
}
