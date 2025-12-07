using UnityEngine;
using System.Collections.Generic;

public static class FarmSaveSystem
{
    private const string KEY = "FARM_SAVE";

    // SAVE toàn bộ crop
    public static void SaveFarm()
    {
        List<CropData> allData = CropSaveSystem.GetAllCropData();

        Debug.Log("🔵 DEBUG SAVE: Saving " + allData.Count + " crops");

        string json = JsonUtility.ToJson(new CropListWrapper(allData));
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    // LOAD tất cả crop
    public static void LoadFarm()
    {
        CropSaveSystem.ClearAll();

        if (!PlayerPrefs.HasKey(KEY))
        {
            Debug.Log("🟡 DEBUG LOAD: No save found");
            return;
        }

        string json = PlayerPrefs.GetString(KEY);
        CropListWrapper wrapper = JsonUtility.FromJson<CropListWrapper>(json);

        Debug.Log("🟢 DEBUG LOAD: Loading " + wrapper.crops.Count + " crops");

        foreach (var d in wrapper.crops)
        {
            GameObject prefab = Resources.Load<GameObject>("CropPrefab");
            if (prefab == null)
            {
                Debug.LogError("❌ DEBUG LOAD: CropPrefab NOT FOUND!");
                return;
            }

            GameObject obj = GameObject.Instantiate(prefab, d.position, Quaternion.identity);
            obj.GetComponent<Crop>().LoadFromData(d);
        }
    }


    [System.Serializable]
    private class CropListWrapper
    {
        public List<CropData> crops;
        public CropListWrapper(List<CropData> list) { crops = list; }
    }
}
