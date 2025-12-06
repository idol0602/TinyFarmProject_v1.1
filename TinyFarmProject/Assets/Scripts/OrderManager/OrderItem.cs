using UnityEngine;

[System.Serializable]
public class OrderItem
{
    public Product product;
    public int quantity;

    public OrderItem(Product product, int quantity)
    {
        this.product = product;
        this.quantity = quantity;
    }

    // Dùng cho UI hiển thị nhanh
    public override string ToString()
    {
        return $"{quantity} {product.productName}";
    }

    // Tính chi phí hạt giống của item này
    public int GetSeedCost()
    {
        return product.seedPrice * quantity;
    }
}