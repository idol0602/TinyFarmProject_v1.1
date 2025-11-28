using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float holdTimeToDrag = 0.18f;

    [Header("References")]
    [SerializeField] private GameObject slotsHolder;
    [SerializeField] private ItemClass[] startingItems = new ItemClass[0];
    public Image itemCursor; // Kéo thả theo chuột

    private GameObject[] slots;
    private SlotClass[] inventorySlots;

    // Drag system
    private SlotClass movingSlot = new SlotClass();
    private SlotClass tempSlot = new SlotClass();
    private SlotClass originalSlot = null;

    private bool isMoving = false;
    private float mouseDownTime = 0f;
    private SlotClass potentialDragSlot = null;

    void Start()
    {
        // Tạo slots
        slots = new GameObject[slotsHolder.transform.childCount];
        inventorySlots = new SlotClass[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = slotsHolder.transform.GetChild(i).gameObject;
            inventorySlots[i] = new SlotClass();
        }

        // Thêm item khởi tạo
        for (int i = 0; i < startingItems.Length && i < inventorySlots.Length; i++)
        {
            if (startingItems[i] != null)
                AddItem(startingItems[i], 10);
        }

        RefreshUI();
    }

    void Update()
    {
        SlotClass hoveredSlot = GetClosestSlot();

        // 1. Bắt đầu nhấn chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            if (hoveredSlot != null && hoveredSlot.GetItem() != null)
            {
                mouseDownTime = Time.time;
                potentialDragSlot = hoveredSlot;
            }
        }

        // 2. Nhấn giữ đủ lâu → bắt đầu kéo
        if (Input.GetMouseButton(0) && !isMoving && potentialDragSlot != null)
        {
            if (Time.time - mouseDownTime >= holdTimeToDrag)
            {
                BeginDrag(potentialDragSlot);
            }
        }

        // 3. Nhả chuột nhanh → chỉ hiện info
        if (Input.GetMouseButtonUp(0))
        {
            if (!isMoving && potentialDragSlot != null && hoveredSlot == potentialDragSlot)
            {
                if (ItemInfoUI.Instance != null && potentialDragSlot.GetItem() != null)
                    ItemInfoUI.Instance.Show(potentialDragSlot.GetItem());
            }
            potentialDragSlot = null;
        }

        // 4. Thả chuột khi đang kéo → kết thúc
        if (isMoving && Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }

        // 5. Nhấn chuột phải → tách stack
        if (Input.GetMouseButtonDown(1) && !isMoving)
        {
            if (hoveredSlot != null && hoveredSlot.GetQuantity() > 1)
            {
                SplitStack(hoveredSlot);
            }
        }

        // 6. Hiển thị icon theo chuột khi kéo
        //if (isMoving && movingSlot.GetItem() != null && itemCursor != null)
        //{
        //    itemCursor.enabled = true;
        //    itemCursor.transform.position = Input.mousePosition;
        //    itemCursor.sprite = movingSlot.GetItem().icon;
        //}
        //else
        //{
        //    if (itemCursor != null)
        //        itemCursor.enabled = false;
        //}
        // Trong Update() - chỉ cập nhật vị trí
        if (isMoving && itemCursor != null)
        {
            itemCursor.transform.position = Input.mousePosition;
        }
        else if (itemCursor != null)
        {
            itemCursor.enabled = false;
        }
    }

    //private void BeginDrag(SlotClass slot)
    //{
    //    originalSlot = slot;
    //    movingSlot.Clear();
    //    movingSlot.AddItem(slot.GetItem(), slot.GetQuantity());
    //    slot.Clear(); // Quan trọng: xóa khỏi slot gốc

    //    isMoving = true;
    //    if (ItemInfoUI.Instance != null) ItemInfoUI.Instance.Hide();
    //    RefreshUI();
    //}

    // Trong BeginDrag
    private void BeginDrag(SlotClass slot)
    {
        originalSlot = slot;
        movingSlot.Clear();
        movingSlot.AddItem(slot.GetItem(), slot.GetQuantity());
        slot.Clear();

        isMoving = true;

        if (itemCursor != null && movingSlot.GetItem() != null)
        {
            itemCursor.sprite = movingSlot.GetItem().icon;
            itemCursor.enabled = true;
        }

        if (ItemInfoUI.Instance != null) ItemInfoUI.Instance.Hide();
        RefreshUI();
    }

    private void EndDrag()
    {
        SlotClass target = GetClosestSlot();

        // Thả ra ngoài hoặc thả lại chính nó
        if (target == null || target == originalSlot)
        {
            ReturnToOriginal();
            return;
        }

        // 1. Target trống → đặt vào
        if (target.GetItem() == null)
        {
            target.AddItem(movingSlot.GetItem(), movingSlot.GetQuantity());
        }
        // 2. Cùng loại + stackable → gộp
        else if (target.IsSameItem(movingSlot.GetItem()) && movingSlot.GetItem().isStackable)
        {
            int total = target.GetQuantity() + movingSlot.GetQuantity();
            int max = movingSlot.GetItem().maxStack;

            if (total <= max)
            {
                target.SetQuantity(total);
            }
            else
            {
                int overflow = total - max;
                target.SetQuantity(max);
                movingSlot.SetQuantity(overflow);

                if (!TryPlaceOverflow())
                {
                    ReturnToOriginal();
                    return;
                }
            }
        }
        // 3. Khác loại → swap
        else
        {
            tempSlot.Clear();
            tempSlot.AddItem(target.GetItem(), target.GetQuantity());

            target.Clear();
            target.AddItem(movingSlot.GetItem(), movingSlot.GetQuantity());

            originalSlot.Clear();
            if (tempSlot.GetItem() != null)
                originalSlot.AddItem(tempSlot.GetItem(), tempSlot.GetQuantity());
        }

        movingSlot.Clear();
        isMoving = false;
        RefreshUI();
        originalSlot = null;

        // Trong EndDrag và ReturnToOriginal
        if (itemCursor != null)
            itemCursor.enabled = false;
    }

    private void ReturnToOriginal()
    {
        if (originalSlot != null && movingSlot.GetItem() != null)
        {
            originalSlot.AddItem(movingSlot.GetItem(), movingSlot.GetQuantity());
        }
        movingSlot.Clear();
        isMoving = false;
        RefreshUI();
        originalSlot = null;

        // Trong EndDrag và ReturnToOriginal
        if (itemCursor != null)
            itemCursor.enabled = false;
    }

    private bool TryPlaceOverflow()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.GetItem() == null)
            {
                slot.AddItem(movingSlot.GetItem(), movingSlot.GetQuantity());
                movingSlot.Clear();
                return true;
            }
        }
        return false;
    }

    private void SplitStack(SlotClass slot)
    {
        int half = Mathf.CeilToInt(slot.GetQuantity() / 2f);
        originalSlot = slot;

        movingSlot.Clear();
        movingSlot.AddItem(slot.GetItem(), half);
        slot.SubQuantity(half);

        isMoving = true;
        RefreshUI();
    }

    private SlotClass GetClosestSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform rect = slots[i].GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
                return inventorySlots[i];
        }
        return null;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Transform slot = slots[i].transform;
            Image img = slot.GetChild(0).GetComponent<Image>();
            TextMeshProUGUI txt = slot.GetChild(1).GetComponent<TextMeshProUGUI>();

            if (inventorySlots[i].GetItem() != null)
            {
                img.enabled = true;
                img.sprite = inventorySlots[i].GetItem().icon;
                txt.text = inventorySlots[i].GetItem().isStackable ? inventorySlots[i].GetQuantity().ToString() : "";
            }
            else
            {
                img.enabled = false;
                img.sprite = null;
                txt.text = "";
            }
        }
    }

    // Thêm item từ bên ngoài (ví dụ: nhặt đồ)
    public void AddItem(ItemClass item, int quantity = 1)
    {
        if (item == null) return;

        // Gộp vào stack cũ
        if (item.isStackable)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot.IsSameItem(item) && slot.GetQuantity() < item.maxStack)
                {
                    int space = item.maxStack - slot.GetQuantity();
                    int add = Mathf.Min(space, quantity);
                    slot.AddQuantity(add);
                    quantity -= add;
                    if (quantity <= 0)
                    {
                        RefreshUI();
                        return;
                    }
                }
            }
        }

        // Tìm slot trống
        foreach (var slot in inventorySlots)
        {
            if (slot.GetItem() == null)
            {
                slot.AddItem(item, quantity);
                RefreshUI();
                return;
            }
        }
    }

    public void RemoveItem(ItemClass item, int quantity = 1)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.IsSameItem(item))
            {
                slot.SubQuantity(quantity);
                if (slot.GetQuantity() <= 0) slot.Clear();
                RefreshUI();
                return;
            }
        }
    }
}