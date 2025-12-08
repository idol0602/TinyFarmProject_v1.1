using UnityEngine;
using MapSummer;

[System.Serializable]
public class CropData
{
    public string cropID;
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
        stage = crop.CurrentStage;
        isDead = crop.IsDead;
        lastWaterDay = crop.LastWaterDay;
        isWateredToday = crop.IsWateredToday;

        posX = crop.transform.position.x;
        posY = crop.transform.position.y;
    }
}
