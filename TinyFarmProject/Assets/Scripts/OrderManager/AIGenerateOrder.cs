using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class AIGenerateOrder : MonoBehaviour
{
    [Header("Gán Product Database ở đây")]
    [SerializeField] private ProductDatabase productDatabase;

    [Header("Gán SeedDatabase ở đây")]
    [SerializeField] private SeedDatabase seedDatabase;

    [Header("DEBUG: Tạo kèm order test Corn x10")]
    public bool orderTest = false;

    [Header("Gemini API Key")]
    [SerializeField] private string geminiApiKey = "AIzaSyARs632T5drQ7upT3Km6qlqywKIfMuMTg8";

    private const string apiUrl =
        "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent";

    // Lưu ngày cuối cùng tạo order để chỉ tạo 1 lần/ngày
    private static int lastOrderDay = -1;
    
    // Lưu orders được tạo
    private List<Order> generatedOrders = new List<Order>();

    private void OnValidate()
    {
        if (productDatabase == null)
            Debug.LogWarning("[AIGenerateOrder] Chưa gán ProductDatabase!");
        if (seedDatabase == null)
            Debug.LogWarning("[AIGenerateOrder] Chưa gán SeedDatabase!");
    }

    /// <summary>
    /// LẤY DANH SÁCH ORDERS VỪA TẠO
    /// </summary>
    public List<Order> GetGeneratedOrders()
    {
        var result = new List<Order>(generatedOrders);
        generatedOrders.Clear(); // Clear sau khi lấy
        return result;
    }

    public IEnumerator GenerateNewOrder()
    {
        // ✅ KIỂM TRA ĐÃ TẠO HÔM NAY CHƯA
        int currentDay = GetCurrentDay();
        if (lastOrderDay == currentDay)
        {
            Debug.LogWarning("[AIGenerateOrder] Đã tạo order hôm nay rồi! Chỉ 1 đơn/ngày");
            yield break;
        }
        lastOrderDay = currentDay;

        // ✅ LUÔN TẠO 1 ORDER AI
        yield return GenerateAIOrder();
        
        // ✅ NẾU DEBUG MODE, THÊM 1 ORDER TEST CORN X10 (REWARD = 300)
        if (orderTest)
        {
            Order testOrder = GenerateTestOrder();
            Debug.Log("<color=cyan>[DEBUG] Tạo 1 order AI + 1 order test Corn x10</color>");
        }
    }

    /// <summary>
    /// TẠO ORDER - ĐỂ AI TỰ TẠOORDER KHẢ THI
    /// </summary>
    public IEnumerator GenerateAIOrder()
    {
        if (productDatabase == null || productDatabase.products.Count == 0)
        {
            Debug.LogError("[AIGenerateOrder] ProductDatabase trống!");
            yield break;
        }

        int playerMoney = PlayerMoney.Instance?.CurrentMoney ?? 0;
        
        // ✅ GỌI AI ĐỂ TẠO ĐƠN HÀNG
        Order order = new Order
        {
            id = UnityEngine.Random.Range(10000, 99999),
            deadlineDays = UnityEngine.Random.Range(2, 5),
            isTestOrder = false
        };

        yield return CallGeminiCreateOrder(order, playerMoney);
    }

    /// <summary>
    /// GỌI AI GEMINI ĐỂ TẠO ĐƠN HÀNG KHẢ THI
    /// </summary>
    private IEnumerator CallGeminiCreateOrder(Order order, int playerMoney)
    {
        // ✅ TẠO PROMPT CHO AI
        string productList = BuildProductListForAI();
        
        string prompt = $@"
Bạn là AI tạo đơn hàng cho game nông trại.

CHỈ TRẢ VỀ JSON HỢP LỆ.
KHÔNG markdown, KHÔNG ```json, KHÔNG giải thích.

Tiền hiện có: {playerMoney}
Hạn giao: {order.deadlineDays} ngày

Danh sách sản phẩm (plant_name | price | seedCost):
{productList}

LUẬT BẮT BUỘC:
1. items PHẢI có ít nhất 2 và nhiều nhất 3 sản phẩm
2. quantity mỗi item: 1–20
3. Tổng (seedCost × quantity) của TẤT CẢ items ≤ {playerMoney}
4. Không được trùng product_name

FORMAT JSON DUY NHẤT:
{{
  ""items"": [
    {{ ""product_name"": ""Corn"", ""quantity"": 5 }},
    {{ ""product_name"": ""Tomato"", ""quantity"": 3 }}
  ]
}}
";
        Debug.Log($"prompt{prompt}");

        string jsonBody =
            $"{{\"contents\":[{{\"role\":\"user\",\"parts\":[{{\"text\":\"{EscapeJson(prompt)}\"}}]}}],\"generationConfig\":{{\"temperature\":0.7,\"maxOutputTokens\":1000}}}}";

        using (UnityWebRequest www = new UnityWebRequest(apiUrl + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string fullResponse = www.downloadHandler.text;
                Debug.Log($"[AIGenerateOrder] FULL RESPONSE: {fullResponse}");
                
                string aiResponse = ExtractGeminiText(fullResponse);
                Debug.Log($"[AIGenerateOrder] Extracted Response: {aiResponse}");
                
                bool success = ParseAIOrderResponse(order, aiResponse);
                Debug.Log($"[AIGenerateOrder] Parse success: {success}, items count: {order.items.Count}");
                
                if (success && order.items.Count > 0)
                {
                    // ✅ THÀNH CÔNG - TÍNH TỔNG THƯỞNG (seedCost × qty)
                    int totalReward = 0;
                    int totalSeedCost = 0;
                    
                    foreach (var item in order.items)
                    {
                        totalSeedCost += item.product.seedCost * item.quantity;
                        totalReward += item.product.seedCost * item.quantity; // Reward = seedCost × qty
                    }
                    
                    order.totalReward = totalReward;
                    Debug.Log($"[AIGenerateOrder] Order created: reward={totalReward}, seedCost={totalSeedCost}");
                    PrintFullOrderDetails(order, totalSeedCost);
                    
                    // ✅ LƯU VÀO DANH SÁCH
                    generatedOrders.Add(order);
                }
                else
                {
                    Debug.LogWarning($"[AIGenerateOrder] Parse failed or no items! items={order.items.Count}");
                    order.content = order.GenerateFallbackContent();
                    OrderManager.Instance.OnOrderContentReady(order);
                }
            }
            else
            {
                Debug.LogError($"[AIGenerateOrder] Gemini error {www.responseCode}: {www.error}");
                order.content = order.GenerateFallbackContent();
                OrderManager.Instance.OnOrderContentReady(order);
            }
        }
    }

    /// <summary>
    /// PARSE JSON RESPONSE TỪ AI VỀ ORDER
    /// </summary>
    private bool ParseAIOrderResponse(Order order, string jsonText)
    {
        try
        {
            Debug.Log($"[ParseAI] Input text length: {jsonText.Length}");
            Debug.Log($"[ParseAI] Input: {jsonText}");
            
            // ✅ TRY EXTRACT JSON OBJECT
            int jsonStart = jsonText.IndexOf('{');
            int jsonEnd = jsonText.LastIndexOf('}');
            
            if (jsonStart < 0 || jsonEnd < 0)
            {
                Debug.LogError("[ParseAI] No { or } found");
                return false;
            }
            
            string jsonPart = jsonText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            Debug.Log($"[ParseAI] Extracted JSON: {jsonPart}");
            
            int itemsStart = jsonPart.IndexOf("[");
            int itemsEnd = jsonPart.LastIndexOf("]");
            
            if (itemsStart < 0 || itemsEnd < 0)
            {
                Debug.LogError("[ParseAI] No [ or ] found in JSON");
                return false;
            }
                
            string itemsJson = jsonPart.Substring(itemsStart + 1, itemsEnd - itemsStart - 1);
            Debug.Log($"[ParseAI] Items JSON: {itemsJson}");
            
            // Debug: show DB product names to help diagnose matching issues
            Debug.Log("[ParseAI] ProductDatabase names: " + string.Join(", ", productDatabase.products.Select(p => p.plant_name)));

            // Better JSON parsing: extract each object properly without relying on },{
            List<string> items = new List<string>();
            int braceDepth = 0;
            string currentItem = "";
            
            foreach (char c in itemsJson)
            {
                if (c == '{')
                {
                    braceDepth++;
                    currentItem += c;
                }
                else if (c == '}')
                {
                    braceDepth--;
                    currentItem += c;
                    
                    if (braceDepth == 0 && !string.IsNullOrWhiteSpace(currentItem))
                    {
                        // Found complete object
                        items.Add(currentItem.Trim());
                        currentItem = "";
                    }
                }
                else if (braceDepth > 0)
                {
                    currentItem += c;
                }
            }
            
            Debug.Log($"[ParseAI] Parsed items count: {items.Count}");
            
            foreach (string item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    Debug.Log("[ParseAI] Skipping empty item");
                    continue;
                }
                    
                Debug.Log($"[ParseAI] Processing item: {item}");
                string cleanItem = item.Replace("{", "").Replace("}", "").Trim();
                
                // Extract product name
                int nameStart = cleanItem.IndexOf("\"product_name\"");
                if (nameStart < 0)
                {
                    Debug.LogWarning("[ParseAI] product_name not found");
                    continue;
                }
                
                int nameQuote1 = cleanItem.IndexOf("\"", nameStart + 16);
                int nameQuote2 = cleanItem.IndexOf("\"", nameQuote1 + 1);
                if (nameQuote1 < 0 || nameQuote2 < 0)
                {
                    Debug.LogWarning("[ParseAI] product_name quotes not found");
                    continue;
                }
                    
                string productName = cleanItem.Substring(nameQuote1 + 1, nameQuote2 - nameQuote1 - 1);
                Debug.Log($"[ParseAI] Product name: {productName}");
                
                // Extract quantity
                int qtyStart = cleanItem.IndexOf("\"quantity\"");
                if (qtyStart < 0)
                {
                    Debug.LogWarning("[ParseAI] quantity not found");
                    continue;
                }
                
                int qtyColon = cleanItem.IndexOf(":", qtyStart);
                int qtyEnd = cleanItem.IndexOf(",", qtyColon);
                if (qtyEnd < 0) qtyEnd = cleanItem.IndexOf("}", qtyColon);
                if (qtyEnd < 0) qtyEnd = cleanItem.Length;
                
                string qtyStr = cleanItem.Substring(qtyColon + 1, qtyEnd - qtyColon - 1).Trim();
                if (!int.TryParse(qtyStr, out int qty))
                {
                    Debug.LogWarning($"[ParseAI] Cannot parse quantity: {qtyStr}");
                    continue;
                }
                
                Debug.Log($"[ParseAI] Quantity: {qty}");
                
                // ✅ TÌM SẢN PHẨM TRONG DATABASE (thử nhiều chiến lược để khớp tên AI)
                string productNameNorm = productName.Trim().ToLower();

                // small synonyms map (lowercase keys)
                var synonyms = new Dictionary<string, string>()
                {
                    {"chili", "Chili"},
                    {"corn", "Corn"},
                    {"eggplant", "Eggplant"},
                    {"tomato", "Tomato"},
                    {"watermelon", "Watermelon"}
                };

                // 1) Exact match
                ProductData product = productDatabase.products.FirstOrDefault(p => p.plant_name.ToLower() == productNameNorm);

                // 2) Contains (product name contains AI name)
                if (product == null)
                    product = productDatabase.products.FirstOrDefault(p => p.plant_name.ToLower().Contains(productNameNorm));

                // 3) AI name contains product name (handles short names)
                if (product == null)
                    product = productDatabase.products.FirstOrDefault(p => productNameNorm.Contains(p.plant_name.ToLower()));

                // 4) Fallback: ignore non-alphanumeric chars (simple normalization)
                if (product == null)
                {
                    string cleanAI = System.Text.RegularExpressions.Regex.Replace(productNameNorm, "[^a-z0-9]", "");
                    product = productDatabase.products.FirstOrDefault(p =>
                        System.Text.RegularExpressions.Regex.Replace(p.plant_name.ToLower(), "[^a-z0-9]", "") == cleanAI);
                }

                if (product != null)
                {
                    order.items.Add(new OrderItem(product, qty));
                    Debug.Log($"[ParseAI] ✓ Added: {product.plant_name} (AI: {productName}) x{qty}");
                }
                else
                {
                    // Try synonyms fallback
                    if (synonyms.TryGetValue(productNameNorm, out string mappedName))
                    {
                        ProductData mapped = productDatabase.products.FirstOrDefault(p => p.plant_name == mappedName);
                        if (mapped != null)
                        {
                            order.items.Add(new OrderItem(mapped, qty));
                            Debug.Log($"[ParseAI] ✓ Added by synonym: {mapped.plant_name} (AI: {productName}) x{qty}");
                            continue;
                        }
                    }

                    Debug.LogWarning($"[ParseAI] Product not found in DB: {productName} -> skipped");
                }
            }
            
            Debug.Log($"[ParseAI] Final items count: {order.items.Count}");
            return order.items.Count > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ParseAI] Exception: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// XÂY DỰNG DANH SÁCH SẢN PHẨM ĐỂ GỬI CHO AI
    /// </summary>
    private string BuildProductListForAI()
    {
        StringBuilder sb = new StringBuilder();
        
        foreach (var product in productDatabase.products)
        {
            sb.AppendLine($"- {product.plant_name} | price: {product.price} | seedcost: {product.seedCost}");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// TẠO ORDER TEST - CORN x 10 (CHỈ DÙNG KHI ORDERTESDT = TRUE)
    /// </summary>
    private Order GenerateTestOrder()
    {
        if (productDatabase == null || productDatabase.products.Count == 0)
            return null;

        Order order = new Order
        {
            id = Random.Range(10000, 99999),
            deadlineDays = 3,
            isTestOrder = true
        };

        // Tìm corn trong ProductDatabase
        ProductData cornProduct = productDatabase.products.FirstOrDefault(p => 
            p.plant_name.ToLower().Contains("corn") || 
            p.plant_name.ToLower().Contains("ngô") ||
            p.plant_name.ToLower().Contains("bắp"));

        if (cornProduct == null)
        {
            // Nếu không có corn, lấy sản phẩm đầu tiên
            cornProduct = productDatabase.products[0];
        }

        int quantity = 10;
        order.items.Add(new OrderItem(cornProduct, quantity));
        
        // 🔧 REWARD = QUANTITY × SEEDCOST
        order.totalReward = quantity * cornProduct.seedCost;
        order.content = "Đây là đơn hàng test đầu tiên! Hoàn thành để nhận thưởng!";

        int seedCost = cornProduct.seedCost * quantity;
        PrintFullOrderDetails(order, seedCost);
        generatedOrders.Add(order);
        
        return order;
    }

    /// <summary>
    /// TÍNH THƯỞNG DỰA TRÊN PRICE/PRICETOSELL
    /// VÍ DỤ: CORN PRICE = 30, QTY = 10 → THƯỞNG = 300
    /// </summary>
    private int CalculateItemReward(ProductData product, int quantity)
    {
        if (product == null || quantity <= 0)
            return 0;

        // ✅ ƯUTIÊN DÙNG PRODUCT.PRICE (GIÁ BÁN TỪ PRODUCTDATA)
        if (product.price > 0)
        {
            int reward = product.price * quantity;
            Debug.Log($"[AIGenerateOrder] {product.plant_name} price={product.price} qty={quantity} → reward={reward}");
            return reward;
        }

        Debug.LogWarning($"[AIGenerateOrder] {product.plant_name} product.price=0, trying SeedDatabase...");

        // ✅ NẾU KHÔNG CÓ PRODUCT.PRICE, TRY SEEDDATABASE
        if (seedDatabase != null)
        {
            SeedData seedData = seedDatabase.seeds.FirstOrDefault(s => 
                s.plantName.ToLower() == product.plant_name.ToLower() ||
                s.seedName.ToLower().Contains(product.plant_name.ToLower()));

            if (seedData != null && seedData.priceToSell > 0)
            {
                // Thưởng = priceToSell * qty
                int reward = seedData.priceToSell * quantity;
                Debug.Log($"[AIGenerateOrder] Found in SeedDB: {product.plant_name} priceToSell={seedData.priceToSell} qty={quantity} → reward={reward}");
                return reward;
            }
        }

        // Backup: trả về 0 nếu không tìm được giá
        Debug.LogError($"[AIGenerateOrder] Không tìm được giá cho {product.plant_name}!");
        return 0;
    }

    /// <summary>
    /// LẤY NGÀY HIỆN TẠI (dùng game day counter)
    /// </summary>
    private int GetCurrentDay()
    {
        // Nếu có DayAndNightManager, dùng nó
        if (DayAndNightManager.Instance != null)
        {
            return DayAndNightManager.Instance.GetCurrentDay();
        }

        // Backup: trả về 0 (ngày đầu tiên)
        return 0;
    }

    // ✅ HÀM IN ĐẦY ĐỦ CHI TIẾT ĐƠN HÀNG
    private void PrintFullOrderDetails(Order order, int seedCost)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("════════════════════════════════════");
        sb.AppendLine("CHI TIẾT ĐƠN HÀNG");
        if (order.isTestOrder)
            sb.AppendLine("(⭐ ORDER TEST ĐẦU TIÊN)");
        sb.AppendLine("════════════════════════════════════");

        sb.AppendLine($"Mã đơn hàng : #{order.id}");
        sb.AppendLine($"Thời hạn    : {order.deadlineDays} ngày");
        sb.AppendLine($"Chi phí hạt : {seedCost} vàng");
        sb.AppendLine($"Thưởng      : {order.totalReward} vàng");

        if (PlayerMoney.Instance != null)
        sb.AppendLine($"Tiền hiện tại: {PlayerMoney.Instance.CurrentMoney} vàng");
        sb.AppendLine("------------------------------------");
        sb.AppendLine("DANH SÁCH SẢN PHẨM:");

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
            sb.AppendLine($"Lời NPC: \"{order.content}\"");
        else
            sb.AppendLine("Lời NPC: (chờ AI tạo...)");

        sb.AppendLine($"Thời gian tạo: {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("════════════════════════════════════");

        Debug.Log(sb.ToString());
    }


//     private IEnumerator CallGeminiAI(Order order, int seedCost)
//     {
//         string itemList = order.GetItemListString();

//         string prompt = $@"Bạn là NPC siêu lầy lội và dễ thương trong game nông trại 2D.
// Viết đúng 1 câu ngắn (dưới 80 ký tự), hài hước + ấm áp về đơn hàng này.
// CHỈ TRẢ VỀ ĐÚNG 1 CÂU DUY NHẤT!

// Đơn hàng: {itemList}
// Giao trong {order.deadlineDays} ngày
// Thưởng {order.totalReward} vàng";

//         string jsonBody =
//             $"{{\"contents\":[{{\"role\":\"user\",\"parts\":[{{\"text\":\"{EscapeJson(prompt)}\"}}]}}],\"generationConfig\":{{\"temperature\":0.9,\"maxOutputTokens\":100}}}}";

//         using (UnityWebRequest www = new UnityWebRequest(apiUrl + "?key=" + geminiApiKey, "POST"))
//         {
//             byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
//             www.uploadHandler = new UploadHandlerRaw(bodyRaw);
//             www.downloadHandler = new DownloadHandlerBuffer();
//             www.SetRequestHeader("Content-Type", "application/json");

//             yield return www.SendWebRequest();

//             // ✅ THÀNH CÔNG
//             if (www.result == UnityWebRequest.Result.Success)
//             {
//                 string aiText = ExtractGeminiText(www.downloadHandler.text);
//                 order.content = string.IsNullOrWhiteSpace(aiText)
//                     ? order.GenerateFallbackContent()
//                     : aiText.Trim();

//                 PrintFullOrderDetails(order, seedCost);
//             }
//             // ✅ HẾT QUOTA → FALLBACK → KHÔNG CHO LÀ LỖI
//             else if (www.responseCode == 429)
//             {
//                 Debug.LogWarning("<color=yellow>Gemini hết quota → dùng nội dung mặc định</color>");
//                 order.content = order.GenerateFallbackContent();
//             }
//             // ✅ LỖI KHÁC
//             else
//             {
//                 Debug.LogWarning("Gemini lỗi: " + www.responseCode);
//                 order.content = order.GenerateFallbackContent();
//             }

//             // ✅ BẮT BUỘC GỌI ĐỂ UI CẬP NHẬT
//             OrderManager.Instance.OnOrderContentReady(order);
//         }
//     }

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
        // Lấy đoạn "text" trong candidates[0].content.parts[0]
        int textIndex = jsonResponse.IndexOf("\"text\"");
        if (textIndex < 0) return "";

        int colonIndex = jsonResponse.IndexOf(":", textIndex);
        int startQuote = jsonResponse.IndexOf("\"", colonIndex + 1) + 1;

        int endQuote = jsonResponse.LastIndexOf("\"");
        if (startQuote < 0 || endQuote <= startQuote) return "";

        string rawText = jsonResponse.Substring(startQuote, endQuote - startQuote);

        return rawText
            .Replace("\\n", "\n")
            .Replace("\\\"", "\"")
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();
    }
    catch (System.Exception e)
    {
        Debug.LogError("[ExtractGeminiText] " + e.Message);
        return "";
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

#if UNITY_EDITOR
[ContextMenu("TEST: GỌI AI & LẤY ORDER")]
private void TestGetAIOrders()
{
    StartCoroutine(TestGetAIOrders_Coroutine());
}

private IEnumerator TestGetAIOrders_Coroutine()
{
    Debug.Log("<color=yellow>[TEST] Bắt đầu tạo order từ AI...</color>");

    // 1️⃣ Gọi tạo order (AI + test nếu bật)
    yield return StartCoroutine(GenerateNewOrder());

    // 2️⃣ Đợi thêm 1 frame cho chắc (AI callback xong)
    yield return null;

    // 3️⃣ Lấy danh sách order vừa tạo
    List<Order> orders = GetGeneratedOrders();

    if (orders == null || orders.Count == 0)
    {
        Debug.LogWarning("<color=red>[TEST] Không lấy được order nào!</color>");
        yield break;
    }

    Debug.Log($"<color=green>[TEST] Lấy được {orders.Count} order</color>");

    // 4️⃣ In chi tiết từng order
    foreach (var order in orders)
    {
        Debug.Log($"<b>ORDER #{order.id}</b> | Deadline: {order.deadlineDays} ngày | Reward: {order.totalReward}");

        foreach (var item in order.items)
        {
            Debug.Log($" → {item.product.plant_name} x{item.quantity} (seedCost={item.product.seedCost})");
        }
    }
}
#endif

}

