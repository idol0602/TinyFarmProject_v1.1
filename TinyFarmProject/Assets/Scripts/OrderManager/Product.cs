using UnityEngine;

[CreateAssetMenu(fileName = "New Product", menuName = "Farm/Product")]
public class Product : ScriptableObject
{
    public string productName;
    public int seedPrice = 10;
    public int baseSellPrice = 25;
    public Sprite icon;
}