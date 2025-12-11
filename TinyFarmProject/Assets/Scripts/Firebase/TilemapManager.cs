using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    public Tilemap tm_Ground;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WriteAllTileMapToFirebase()
    {
        for(int x = tm_Ground.cellBounds.min.x; x < tm_Ground.cellBounds.max.x; x++)
        {
            for(int y = tm_Ground.cellBounds.min.y; y < tm_Ground.cellBounds.max.y; y++)
            {
                TilemapDetail tilemapDetail = new TilemapDetail();
            }
        }
    }
}
