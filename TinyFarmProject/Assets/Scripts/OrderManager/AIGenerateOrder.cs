using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIGenerateOrder : MonoBehaviour
{
    [Header("Gán Product Database ở đây")]
    [SerializeField] private ProductDatabase productDatabase;

    [Header("Gemini API Key")]
    [SerializeField] private string geminiApiKey = "AIzaSyARs632T5drQ7upT3Km6qlqywKIfMuMTg8";

    private const string apiUrl =
        "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent";

    private void OnValidate()
    {
        if (productDatabase == null)
            Debug.LogWarning("[AIGenerateOrder] Chưa gán ProductDatabase!");
    }

    public Order GenerateNewOrder()
    {
        if (productDatabase == null || productDatabase.products.Count == 0)
        {
            Debug.LogError("[AIGenerateOrder] ProductDatabase trống!");
            return null;
        }

        Order order = new Order
        {
            id = Random.Range(10000, 99999),
            deadlineDays = Random.Range(1, 4)
        };

        int itemCount = Random.Range(1, Mathf.Min(5, productDatabase.products.Count + 1));
        var selectedProducts = productDatabase.GetRandomProducts(itemCount, false);

        int totalSeedCost = 0;
        foreach (var p in selectedProducts)
        {
            int qty = Random.Range(10, 41);
            order.items.Add(new OrderItem(p, qty));
            totalSeedCost += p.seedCost * qty;
        }

        order.totalReward = Mathf.RoundToInt(totalSeedCost * Random.Range(2.7f, 4.3f));

        // ✅ IN CHI TIẾT ĐƠN HÀNG (CHƯA CÓ LỜI NPC)
        PrintFullOrderDetails(order, totalSeedCost);

        if (!string.IsNullOrEmpty(geminiApiKey))
            StartCoroutine(CallGeminiAI(order, totalSeedCost));
        else
            order.content = order.GenerateFallbackContent();

        return order;
    }

    // ✅ HÀM IN ĐẦY ĐỦ CHI TIẾT ĐƠN HÀNG
    private void PrintFullOrderDetails(Order order, int seedCost)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("════════════════════════════════════");
        sb.AppendLine("🧾 CHI TIẾT ĐƠN HÀNG");
        sb.AppendLine("════════════════════════════════════");

        sb.AppendLine($"📌 Mã đơn hàng : #{order.id}");
        sb.AppendLine($"⏳ Thời hạn    : {order.deadlineDays} ngày");
        sb.AppendLine($"🌱 Chi phí hạt : {seedCost} vàng");
        sb.AppendLine($"💰 Thưởng      : {order.totalReward} vàng");

        sb.AppendLine("------------------------------------");
        sb.AppendLine("📦 DANH SÁCH SẢN PHẨM:");

        int index = 1;
        foreach (var item in order.items)
        {
            int itemCost = item.product.seedCost * item.quantity;

            sb.AppendLine(
                $"{index}. {item.product.plant_name} | SL: {item.quantity} | Giá hạt: {item.product.seedCost} | Tổng: {itemCost}"
            );
            index++;
        }

        sb.AppendLine("------------------------------------");

        if (!string.IsNullOrEmpty(order.content))
            sb.AppendLine($"🗣 Lời NPC: \"{order.content}\"");
        else
            sb.AppendLine("🗣 Lời NPC: (chưa có)");

        sb.AppendLine($"🕒 Thời gian tạo: {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    private IEnumerator CallGeminiAI(Order order, int seedCost)
    {
        string itemList = order.GetItemListString();

        string prompt = $@"Bạn là NPC siêu lầy lội và dễ thương trong game nông trại 2D.
Viết đúng 1 câu ngắn (dưới 80 ký tự), hài hước + ấm áp về đơn hàng này.
CHỈ TRẢ VỀ ĐÚNG 1 CÂU DUY NHẤT!

Đơn hàng: {itemList}
Giao trong {order.deadlineDays} ngày
Thưởng {order.totalReward} vàng";

        string jsonBody =
            $"{{\"contents\":[{{\"role\":\"user\",\"parts\":[{{\"text\":\"{EscapeJson(prompt)}\"}}]}}],\"generationConfig\":{{\"temperature\":0.9,\"maxOutputTokens\":100}}}}";

        using (UnityWebRequest www = new UnityWebRequest(apiUrl + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("<color=orange>Gửi Gemini...</color>");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string aiText = ExtractGeminiText(www.downloadHandler.text);
                order.content = string.IsNullOrWhiteSpace(aiText)
                    ? order.GenerateFallbackContent()
                    : aiText.Trim();

                Debug.Log($"<color=lime>Gemini nói: {order.content}</color>");

                // ✅ IN LẠI CHI TIẾT SAU KHI CÓ LỜI NPC
                PrintFullOrderDetails(order, seedCost);
            }
            else
            {
                Debug.LogError("Gemini lỗi: " + www.responseCode);
                Debug.LogError("Response: " + www.downloadHandler.text);
                order.content = order.GenerateFallbackContent();
            }
        }
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
    }

    private string ExtractGeminiText(string jsonResponse)
    {
        try
        {
            int start = jsonResponse.IndexOf("\"text\":\"") + 8;
            if (start == 7) return "";
            int end = jsonResponse.IndexOf("\"", start);
            return jsonResponse.Substring(start, end - start)
                              .Replace("\\n", " ")
                              .Replace("\\\"", "\"")
                              .Trim();
        }
        catch
        {
            return "NPC đang ngủ gật...";
        }
    }

#if UNITY_EDITOR
    [ContextMenu("TEST IN ĐƠN HÀNG NGAY")]
    private void TestGenerateOrder()
    {
        GenerateNewOrder();
        Debug.Log("<color=orange>ĐÃ TẠO 1 ĐƠN HÀNG MỚI ĐỂ TEST!</color>");
    }
#endif
}
