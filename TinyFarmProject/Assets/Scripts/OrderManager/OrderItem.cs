using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OrderItem
{
    public ProductData product;
    public int quantity;

    public OrderItem(ProductData product, int quantity)
    {
        this.product = product;
        this.quantity = quantity;
    }

    public override string ToString() => $"{quantity} {product.plant_name}";
}