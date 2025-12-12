using UnityEngine;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    // ✅ Singleton Pattern
    public static InventoryManager Instance { get; private set; }

    [Header("Firebase Load Settings")]
    public bool loadFromFirebase = true;  // Bật này để load từ Firebase thay vì Inspector

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitSecondInventory();  // Init second trước
        InitInventory();        // Sau đó mới init first
    }

    private void Start()
    {
        // Load từ Firebase sau khi tất cả UI đã init xong
        if (loadFromFirebase)
        {
            Debug.Log($"[InventoryManager] Start() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}");
            
            if (FirebaseDatabaseManager.FirebaseReady)
            {
                Debug.Log("[InventoryManager] Firebase ready, loading inventory from Firebase...");
                FirebaseDatabaseManager.Instance.LoadInventoryFromFirebase("Player1");
            }
            else
            {
                Debug.LogWarning("[InventoryManager] Firebase NOT ready, retrying in 1 second...");
                Invoke(nameof(TryLoadInventoryFromFirebase), 1f);
            }
        }
    }

    private void TryLoadInventoryFromFirebase()
    {
        Debug.Log($"[InventoryManager] TryLoadInventoryFromFirebase() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}");
        
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[InventoryManager] Retrying Firebase load...");
            FirebaseDatabaseManager.Instance.LoadInventoryFromFirebase("Player1");
        }
        else
        {
            Debug.LogError("[InventoryManager] Firebase still NOT ready after retry!");
        }
    }

    [Header("UI Settings")]
    public GameObject slotPrefabPanelBackground; // prefab slot UI (inventory 1)
    public Transform inventoryPanel;              // grid container (trên)
    
    public GameObject secondSlotPrefab;           // prefab slot UI khác (inventory 2) - tùy chọn
    public Transform secondInventoryPanel;        // grid container thứ 2 (dưới, ngang)

    [Header("Detail Panel")]
    public ItemDetailPanel detailPanel;           // Panel hiển thị chi tiết item (dùng chung cho 2 inventory)

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
            if (detailPanel != null)
            {
                slotUI.detailPanel = detailPanel;  // Gán panel duy nhất cho tất cả slots
            }
            uiSlots[i] = slotUI;                 // lưu UI reference
        }

        // ⚠️ CHỈ load từ Inspector nếu loadFromFirebase = false
        if (!loadFromFirebase)
        {
            Debug.Log($"[InventoryManager] Loading {slotSlot.Length} items from Inspector (slotSlot)");
            for (int i = 0; i < slotSlot.Length && i < inventorySize; i++)
            {
                if (slotSlot[i] != null && slotSlot[i].item != null)
                {
                    Debug.Log($"  → Slot {i}: {slotSlot[i].item.itemName} x{slotSlot[i].quantity}");
                    AddItem(slotSlot[i].item, slotSlot[i].quantity);
                }
            }
        }
        else
        {
            Debug.Log("[InventoryManager] loadFromFirebase = TRUE → Inventory cleared, waiting for Firebase load");
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
            if (detailPanel != null)
            {
                slotUI.detailPanel = detailPanel;  // Gán panel duy nhất cho tất cả slots
            }
            secondUiSlots[i] = slotUI;
        }

        // ⚠️ CHỈ load từ Inspector nếu loadFromFirebase = false
        if (!loadFromFirebase && secondSlotSlot != null)
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
        // Nếu qty vượt maxStack, tách thành nhiều item
        while (qty > item.maxStack)
        {
            AddItem(item, item.maxStack);  // Thêm 1 stack đầy
            qty -= item.maxStack;           // Giảm qty còn lại
        }

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

        Debug.Log("Inventory FULL - Overflow to second");
        // Nếu inventory 1 full, thêm vào inventory 2
        if (qty > 0)
        {
            AddItemToSecond(item, qty);
        }
        return false;
    }

    public bool AddItemToSecond(ItemData item, int qty = 1)
    {
        if (secondSlotDataList == null)
        {
            Debug.LogWarning("Second inventory not initialized!");
            return false;
        }

        // Nếu qty vượt maxStack, tách thành nhiều item
        while (qty > item.maxStack)
        {
            AddItemToSecond(item, item.maxStack);  // Thêm 1 stack đầy
            qty -= item.maxStack;                   // Giảm qty còn lại
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

    // ============================================================
    // Getter methods for Firebase Save/Load
    // ============================================================
    public SlotData GetSlotData(int index)
    {
        if (slotDataList != null && index >= 0 && index < slotDataList.Length)
            return slotDataList[index];
        return null;
    }

    public SlotData GetSecondSlotData(int index)
    {
        if (secondSlotDataList != null && index >= 0 && index < secondSlotDataList.Length)
            return secondSlotDataList[index];
        return null;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < slotDataList.Length; i++)
        {
            slotDataList[i].item = null;
            slotDataList[i].quantity = 0;
            if (uiSlots[i] != null)
                uiSlots[i].Refresh();
        }
    }

    public void ClearSecondInventory()
    {
        if (secondSlotDataList == null) return;
        
        for (int i = 0; i < secondSlotDataList.Length; i++)
        {
            secondSlotDataList[i].item = null;
            secondSlotDataList[i].quantity = 0;
            if (secondUiSlots[i] != null)
                secondUiSlots[i].Refresh();
        }
    }

    /// <summary>
    /// Tìm ItemData từ itemName string
    /// Dùng cho Firebase load khi cần tìm ItemData từ tên lưu trong database
    /// </summary>
    public ItemData GetItemDataByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        // Tìm trong slotSlot array (main inventory initial data)
        if (slotSlot != null)
        {
            foreach (var inventoryItem in slotSlot)
            {
                if (inventoryItem != null && inventoryItem.item != null && inventoryItem.item.itemName == itemName)
                {
                    Debug.Log($"[InventoryManager] Found ItemData for '{itemName}' in slotSlot");
                    return inventoryItem.item;
                }
            }
        }

        // Tìm trong secondSlotSlot array (second inventory initial data)
        if (secondSlotSlot != null)
        {
            foreach (var inventoryItem in secondSlotSlot)
            {
                if (inventoryItem != null && inventoryItem.item != null && inventoryItem.item.itemName == itemName)
                {
                    Debug.Log($"[InventoryManager] Found ItemData for '{itemName}' in secondSlotSlot");
                    return inventoryItem.item;
                }
            }
        }

        // 🔍 Fallback: Tìm trong tất cả ItemData assets trong project
        Debug.Log($"[InventoryManager] '{itemName}' not found in slotSlot/secondSlotSlot, searching all ItemData assets...");
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && item.itemName == itemName)
            {
                Debug.Log($"[InventoryManager] ✅ Found ItemData for '{itemName}' in project assets");
                return item;
            }
        }

        Debug.LogWarning($"[InventoryManager] ❌ ItemData NOT found for name: '{itemName}' (checked slotSlot, secondSlotSlot, and all project assets)");
        return null;
    }
}
