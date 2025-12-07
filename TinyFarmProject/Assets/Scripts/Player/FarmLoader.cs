using UnityEngine;

public class FarmLoader : MonoBehaviour
{
    void Start()
    {
        // ⭐ XÓA CÂY ĐÃ ĐƯỢC LOAD TỪ LẦN TRƯỚC (CÓ TRONG CropSaveSystem)
        //foreach (var crop in CropSaveSystem.GetRuntimeCrops())
        //{
        //    if (crop != null)
        //        GameObject.Destroy(crop.gameObject);
        //}

        CropSaveSystem.ClearAll();

        // ⭐ LOAD LẠI CÂY TỪ SAVE
        FarmSaveSystem.LoadFarm();
    }
}
