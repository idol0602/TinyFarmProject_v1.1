using Newtonsoft.Json;
using UnityEngine;

public enum TilemapState
{
    Ground,
    Grass,
    Forest
}

[System.Serializable]
public class TilemapDetail
{
    public int x { get; set; }
    public int y { get; set; }
    public TilemapState tilemapState { get; set; }

    public TilemapDetail() { }

    public TilemapDetail(int x, int y, TilemapState tilemapState)
    {
        this.x = x;
        this.y = y;
        this.tilemapState = tilemapState;
    }

    // Dùng để debug nhanh
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}