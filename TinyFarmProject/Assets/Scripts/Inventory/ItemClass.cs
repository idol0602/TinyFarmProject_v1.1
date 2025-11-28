using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item", fileName = "New Item")]
public class ItemClass : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    public Sprite icon;
    public bool isStackable = true;
    public int maxStack = 99;
    [TextArea] public string description;

    [Header("Tùy chọn (không bắt buộc)")]
    public float buyPrice = 10;
    public float sellPrice = 5;
}