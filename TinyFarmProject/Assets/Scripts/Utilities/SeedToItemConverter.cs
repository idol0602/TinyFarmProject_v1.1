using UnityEngine;

/// <summary>
/// Chuyển đổi SeedData sang ItemData để sử dụng trong Inventory
/// </summary>
public static class SeedToItemConverter
{
    /// <summary>
    /// Convert SeedData thành ItemData tạm thời
    /// </summary>
    public static ItemData ConvertSeedToItem(SeedData seed)
    {
        if (seed == null)
            return null;

        // Tạo ItemData tạm thời (không phải ScriptableObject)
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = seed.plantName;
        item.icon = seed.seedIcon;
        item.itemType = ItemType.Seed;
        item.itemSubtype = GetSeedSubtype(seed.plantName);
        item.stackable = true;
        item.maxStack = 99;
        item.growTimeDays = seed.growTime;
        item.sellPrice = seed.priceToSell;
        
        return item;
    }

    /// <summary>
    /// Lấy ItemSubtype từ tên cây
    /// </summary>
    private static ItemSubtype GetSeedSubtype(string plantName)
    {
        plantName = plantName.ToLower();

        if (plantName.Contains("tomato") || plantName.Contains("cà chua"))
            return ItemSubtype.TomatoSeed;
        if (plantName.Contains("chili") || plantName.Contains("ớt"))
            return ItemSubtype.ChiliSeed;
        if (plantName.Contains("corn") || plantName.Contains("ngô"))
            return ItemSubtype.CornSeed;
        if (plantName.Contains("eggplant") || plantName.Contains("cà tím"))
            return ItemSubtype.EggplantSeed;
        if (plantName.Contains("watermelon") || plantName.Contains("dưa"))
            return ItemSubtype.WatermelonSeed;

        return ItemSubtype.None;
    }
}
