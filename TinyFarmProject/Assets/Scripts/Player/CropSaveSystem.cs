using System.Collections.Generic;
using UnityEngine;

public class CropSaveSystem : MonoBehaviour
{
    public static List<CropData> allCrops = new List<CropData>();

    [Header("Prefab cây để spawn lại")]
    public GameObject cropPrefab;

    // Khi scene FARM load → spawn lại toàn bộ cây
    private void Start()
    {
        LoadAllCrops();
    }

    public void LoadAllCrops()
    {
        foreach (var d in allCrops)
        {
            GameObject obj = Instantiate(cropPrefab, d.position, Quaternion.identity);
            Crop c = obj.GetComponent<Crop>();
            c.LoadFromData(d);
        }
    }

    // ====================== ADD CROP =======================
    public static void AddCrop(Crop crop)
    {
        CropData d = new CropData();

        d.cropID = crop.CropID;
        d.position = crop.transform.position;
        d.stage = crop.CurrentStage;
        d.isDead = crop.IsDead;
        d.lastWaterDay = crop.LastWaterDay;
        d.isWateredToday = crop.IsWateredToday;

        allCrops.Add(d);
    }

    // ====================== REMOVE CROP =====================
    public static void RemoveCrop(Crop crop)
    {
        allCrops.RemoveAll(x => x.cropID == crop.CropID);
    }
}
