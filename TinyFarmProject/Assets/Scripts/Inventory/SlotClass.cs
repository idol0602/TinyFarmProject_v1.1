using UnityEngine;

[System.Serializable]
public class SlotClass
{
    [SerializeField] private ItemClass item;
    [SerializeField] private int quantity;

    public SlotClass()
    {
        Clear();
    }

    public SlotClass(ItemClass item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public ItemClass GetItem() => item;
    public int GetQuantity() => quantity;

    public void AddQuantity(int amount) => quantity += amount;
    public void SubQuantity(int amount) => quantity = Mathf.Max(0, quantity - amount);
    public void SetQuantity(int amount) => quantity = amount;

    public void AddItem(ItemClass item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    // Quan trọng: so sánh item theo tên asset (đảm bảo cùng loại)
    public bool IsSameItem(ItemClass other)
    {
        if (item == null || other == null) return false;
        return item.name == other.name;
    }
}