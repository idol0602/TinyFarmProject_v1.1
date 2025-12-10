using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identification")]
    public string itemName;
    public Sprite icon;

    [Header("Category Type")]
    public ItemType itemType;

    [Tooltip("Specific subtype of item, for seeds/crops distinction")]
    public ItemSubtype itemSubtype;

    [Header("Stacking")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("Seed Info (if item is seed)")]
    public float growTimeDays;

    [Header("Sell Info (if item is crop)")]
    public int sellPrice;
}

public enum ItemType
{
    Seed,        // Hạt giống
    Crop,        // Cây đã thu hoạch / sản phẩm
    Tool,        // Dụng cụ
    Material,    // Nguyên liệu chế tạo
    Consumable,  // Vật phẩm tiêu thụ (HP, Energy,…)
    Other
}


public enum ItemSubtype
{
    None,

    // Seed types
    TomatoSeed,
    ChiliSeed,
    CornSeed,
    EggplantSeed,
    WatermelonSeed,

    // Harvest / Crop
    TomatoCrop,
    ChiliCrop,
    CornCrop,
    EggplantCrop,
    WatermelonCrop,
}