using UnityEngine;

[CreateAssetMenu(fileName = "New Product", menuName = "Game Data/Product")]
public class ProductData : ScriptableObject
{
    public string plant_name = "Tên cây";
    public Sprite icon;
    public int price = 10;           // Giá bán 1 đơn vị (khi giao hàng)
    public int seedCost = 5;         // Giá mua hạt giống (tùy chọn, để tính lợi nhuận)

    // Dùng để hiển thị trong Order
    public override string ToString()
    {
        return $"{plant_name}";
    }
}