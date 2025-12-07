using System.Collections.Generic;

public static class CropSaveSystem
{
    private static List<Crop> crops = new List<Crop>();

    public static void AddCrop(Crop c)
    {
        if (!crops.Contains(c))
            crops.Add(c);
    }

    public static void RemoveCrop(Crop c)
    {
        crops.Remove(c);
    }

    public static void ClearAll()
    {
        crops.Clear();   // ⭐ Quan trọng
    }

    public static List<CropData> GetAllCropData()
    {
        List<CropData> list = new List<CropData>();

        foreach (var c in crops)
        {
            if (c == null) continue;  // ⭐ Chống lỗi MissingReference

            CropData d = new CropData();
            d.cropID = c.CropID;
            d.stage = c.CurrentStage;
            d.isDead = c.IsDead;
            d.lastWaterDay = c.LastWaterDay;
            d.isWateredToday = c.IsWateredToday;
            d.position = c.transform.position;

            list.Add(d);
        }

        return list;
    }
    public static List<Crop> GetRuntimeCrops()
    {
        return new List<Crop>(crops);
    }

}
