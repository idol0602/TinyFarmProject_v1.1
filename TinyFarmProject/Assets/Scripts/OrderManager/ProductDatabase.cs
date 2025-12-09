using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProductDatabase", menuName = "Game Data/Product Database")]
public class ProductDatabase : ScriptableObject
{
    public List<ProductData> products = new List<ProductData>();

    // Dễ lấy sản phẩm theo tên (nếu cần)
    public ProductData GetProductByName(string productName)
    {
        return products.Find(p => p.plant_name == productName);
    }

    // Lấy ngẫu nhiên 1 sản phẩm
    public ProductData GetRandomProduct()
    {
        if (products.Count == 0) return null;
        return products[Random.Range(0, products.Count)];
    }

    // Lấy nhiều sản phẩm ngẫu nhiên không trùng (dùng cho order)
    public List<ProductData> GetRandomProducts(int count, bool allowDuplicate = false)
    {
        List<ProductData> result = new List<ProductData>();
        List<ProductData> pool = new List<ProductData>(products);

        count = Mathf.Clamp(count, 1, pool.Count);

        if (!allowDuplicate)
        {
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                result.Add(pool[Random.Range(0, pool.Count)]);
            }
        }

        return result;
    }
}