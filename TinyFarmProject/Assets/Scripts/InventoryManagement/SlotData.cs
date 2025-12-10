using UnityEngine;

[System.Serializable]
public class SlotData
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null;
}
