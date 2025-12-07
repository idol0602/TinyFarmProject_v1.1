using UnityEngine;

[System.Serializable]
public class CropData
{
    public string cropID;          // ID duy nhất
    public Vector3 position;       // vị trí cây
    public int stage;              // stage hiện tại
    public bool isDead;            // cây đã chết chưa
    public int lastWaterDay;       // ngày tưới lần cuối
    public bool isWateredToday;    // hôm nay đã tưới chưa
}
