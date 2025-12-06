using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIGenerateOrder : MonoBehaviour
{
    [Header("Danh sách sản phẩm có thể xuất hiện")]
    public List<Product> availableProducts = new List<Product>();

    [Header("API Settings (để trống nếu chỉ test offline)")]
    public string apiKey = "";
    public string apiUrl = "https://api.x.ai/v1/chat/completions";

    // HÀM PUBLIC CHÍNH – GỌI TỪ BÊN NGOÀI ĐỂ TẠO ĐƠN
    public Order GenerateNewOrder()
    {
        if (availableProducts.Count == 0)
        {
            Debug.LogError("Chưa gán sản phẩm nào trong AIGenerateOrder!");
            return null;
        }

        Order order = new Order
        {
            id = Random.Range(10000, 99999),
            deadlineDays = Random.Range(1, 4)
        };

        // Random số loại hàng: 1 đến 4
        int itemCount = Random.Range(1, Mathf.Min(5, availableProducts.Count + 1));
        int totalSeedCost = 0;

        for (int i = 0; i < itemCount; i++)
        {
            Product p = availableProducts[Random.Range(0, availableProducts.Count)];
            int qty = Random.Range(10, 41);
            order.items.Add(new OrderItem(p, qty));
            totalSeedCost += p.seedPrice * qty;
        }

        // Đảm bảo lợi nhuận: reward từ 2.3x đến 4x chi phí hạt giống
        order.totalReward = Mathf.RoundToInt(totalSeedCost * Random.Range(2.3f, 4f));

        // Nếu có API key → gọi AI thật, không thì dùng fallback
        if (!string.IsNullOrEmpty(apiKey))
            StartCoroutine(CallAIForContent(order));
        else
            order.content = order.GenerateFallbackContent() + " (có thể thay bằng AI sau)";

        return order;
    }

    private IEnumerator CallAIForContent(Order order)
    {
        string productList = "";
        for (int i = 0; i < order.items.Count; i++)
        {
            productList += order.items[i].ToString();
            if (i < order.items.Count - 2) productList += ", ";
            else if (i == order.items.Count - 2) productList += " và ";
        }

        string prompt = $@"
Bạn là NPC vui tính trong game nông trại 2D. Viết 1 câu ngắn (dưới 90 ký tự), ấm áp, hài hước, tự nhiên về đơn hàng sau.
CHỈ TRẢ VỀ ĐÚNG 1 CÂU, không thêm dấu ngoặc, không giải thích.

Sản phẩm: {productList}
Thời hạn: {order.deadlineDays} ngày
Phần thưởng: {order.totalReward} vàng

Ví dụ:
- 'Chị bán hoa đang khóc huhu vì thiếu {productList} làm bó cưới đó!'
- 'Ông chủ tiệm bánh gầm lên: “{productList} đâu hết rồi hả trời!”'
- 'Thương nhân cười toe: “Cho tôi {productList} đi, trả hậu đây!”'";

        string json = $"{{\"model\":\"grok-beta\",\"messages\":[{{\"role\":\"user\",\"content\":\"{prompt}\"}}],\"temperature\":0.95,\"max_tokens\":100}}";

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string text = ExtractContentFromResponse(www.downloadHandler.text);
                order.content = string.IsNullOrEmpty(text) ? order.GenerateFallbackContent() : text;
            }
            else
            {
                Debug.LogWarning("AI lỗi: " + www.error);
                order.content = order.GenerateFallbackContent();
            }

            // Thông báo UI cập nhật lại nội dung
            Debug.Log(order);
        }
    }

    private string ExtractContentFromResponse(string jsonResponse)
    {
        try
        {
            int start = jsonResponse.IndexOf("\"content\":\"") + 11;
            int end = jsonResponse.IndexOf("\"", start);
            string raw = jsonResponse.Substring(start, end - start);
            return raw.Replace("\\n", "").Replace("\\\"", "\"").Trim();
        }
        catch
        {
            return "";
        }
    }

    // Tự động tạo demo products nếu chưa có
    private void Awake()
    {
        if (availableProducts.Count == 0)
        {
            Debug.Log("[AIGenerateOrder] Tạo sản phẩm demo...");
            availableProducts.Add(CreateProduct("Cà rốt", 10));
            availableProducts.Add(CreateProduct("Táo", 15));
            availableProducts.Add(CreateProduct("Trứng gà", 5));
            availableProducts.Add(CreateProduct("Bí đỏ", 20));
            availableProducts.Add(CreateProduct("Cà chua", 12));
        }
    }

    private Product CreateProduct(string name, int seedPrice)
    {
        Product p = ScriptableObject.CreateInstance<Product>();
        p.productName = name;
        p.seedPrice = seedPrice;
        p.baseSellPrice = seedPrice * 3;
        return p;
    }
}