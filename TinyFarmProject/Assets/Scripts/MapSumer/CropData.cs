using UnityEngine;
using MapSummer;

[System.Serializable]
public class CropData
{
    public string cropID;
    public string cropType;   // ⭐ LOẠI CÂY (Corn, Chili, Tomato...)

    public int stage;
    public bool isDead;
    public int lastWaterDay;
    public bool isWateredToday;

    public float posX;
    public float posY;

    public CropData() { }

    public CropData(Crop crop)
    {
        cropID = crop.CropID;
        cropType = crop.cropType;    // ⭐ LƯU LOẠI CÂY TẠI ĐÂY

        stage = crop.CurrentStage;
        isDead = crop.IsDead;
        lastWaterDay = crop.LastWaterDay;
        isWateredToday = crop.IsWateredToday;

        posX = crop.transform.position.x;
        posY = crop.transform.position.y;
    }
}
