using UnityEngine;

[System.Serializable]
public class SlotData
{
    public ItemData item;
    public int quantity;
    public int slotIndex;  // Vị trí của slot trong inventory

    public bool IsEmpty => item == null;
}
