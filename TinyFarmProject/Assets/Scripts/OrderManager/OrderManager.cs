using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Firebase.Database;

public class OrderManager : MonoBehaviour
{
    // Singleton để gọi từ bất kỳ đâu trong game
    public static OrderManager Instance { get; private set; }

    [Header("References")]
    public AIGenerateOrder orderGenerator;
    public ProductDatabase productDatabase;

    [Header("Danh sách đơn hàng")]
    public List<Order> pendingOrders = new List<Order>();   // Chờ người chơi xem & chấp nhận
    public List<Order> acceptedOrders = new List<Order>();  // Đã nhận, đang chuẩn bị giao

    // Kiểm tra orders đã tạo trong ngày
    private int lastDayOrdersCreated = -1; // Ngày cuối cùng tạo orders (-1 = chưa tạo)

    // Events để UI tự động cập nhật (rất quan trọng!)
    public UnityEvent<Order> onOrderAdded = new UnityEvent<Order>();
    public UnityEvent<Order> onOrderRemoved = new UnityEvent<Order>();
    public UnityEvent<Order> onOrderContentReady = new UnityEvent<Order>(); // Khi AI trả về nội dung đẹp
    public UnityEvent onOrdersListChanged = new UnityEvent(); // Khi danh sách thay đổi
    public UnityEvent<string> onInventoryCheckFailed = new UnityEvent<string>(); // Khi kiểm tra inventory thất bại

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (orderGenerator == null)
            orderGenerator = FindObjectOfType<AIGenerateOrder>();
        
        // Tìm ProductDatabase (ScriptableObject asset)
        if (productDatabase == null)
        {
            Debug.Log("[OrderManager] 🔍 ProductDatabase chưa assign, tìm kiếm...");
            
            // Cách 1: Cố gắng tìm từ Resources (nếu có copy ở đó)
            productDatabase = Resources.Load<ProductDatabase>("ProductDatabase");
            
            // Cách 2: Nếu không, tìm bằng cách khác (dùng Awake hoặc direct path)
            // Lưu ý: FindObjectsOfType không dùng được vì SO không nằm trong scene
            
            if (productDatabase != null)
            {
                Debug.Log("[OrderManager] ✅ Tìm thấy ProductDatabase trong Resources: " + productDatabase.name);
            }
            else
            {
                Debug.LogError("[OrderManager] ❌ KHÔNG TÌM THẤY ProductDatabase!");
                Debug.LogError("[OrderManager] ⚠️ FIX: ");
                Debug.LogError("[OrderManager]    1. Gán ProductDatabase vào field trong Inspector");
                Debug.LogError("[OrderManager]    2. Hoặc copy ProductDatabase.asset vào Assets/Resources");
                Debug.LogError("[OrderManager]    3. Hoặc gọi RefreshDailyOrders() sau khi gán");
            }
        }
        else
        {
            Debug.Log("[OrderManager] ✅ ProductDatabase đã gán trong Inspector: " + productDatabase.name);
        }

        Debug.Log($"[OrderManager] Start() called. Firebase Ready: {FirebaseDatabaseManager.FirebaseReady}");
        Debug.Log($"[OrderManager] ProductDatabase: {(productDatabase != null ? productDatabase.name : "NULL ❌")}");
        Debug.Log($"[OrderManager] AIGenerateOrder: {(orderGenerator != null ? "Found" : "NULL")}");

        // Load orders từ Firebase khi vào game
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[OrderManager] ✅ Firebase ready, load orders...");
            LoadOrdersFromFirebase();
        }
        else
        {
            // Nếu Firebase chưa ready, chờ rồi thử lại
            Debug.LogWarning("[OrderManager] Firebase chưa ready, thử lại sau 2 giây...");
            Invoke(nameof(TryLoadOrdersWhenFirebaseReady), 2f);
        }
    }

    /// <summary>
    /// Thử load orders khi Firebase đã ready
    /// </summary>
    private void TryLoadOrdersWhenFirebaseReady()
    {
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[OrderManager] ✅ Firebase ready, load orders...");
            LoadOrdersFromFirebase();
        }
        else
        {
            Debug.LogWarning("[OrderManager] Firebase vẫn chưa ready, thử lại sau 2 giây...");
            Invoke(nameof(TryLoadOrdersWhenFirebaseReady), 2f);
        }
    }

    /// <summary>
    /// Thử tạo orders khi Firebase đã ready
    /// </summary>
    private void TryRefreshOrdersWhenFirebaseReady()
    {
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[OrderManager] ✅ Firebase ready, tạo orders...");
            RefreshDailyOrders();
        }
        else
        {
            Debug.LogWarning("[OrderManager] Firebase vẫn chưa ready, thử lại sau 2 giây...");
            Invoke(nameof(TryRefreshOrdersWhenFirebaseReady), 2f);
        }
    }

    /// <summary>
    /// Gọi mỗi ngày mới trong game để tạo 1 order mới
    /// Tạo 1 order test + 1 order từ AI mỗi ngày (chỉ tạo 1 lần/ngày)
    /// </summary>
    public void RefreshDailyOrders()
    {
        // Lấy ngày hiện tại từ DayAndNightManager hoặc TimeManager
        int currentDay = GetCurrentGameDay();
        
        // Kiểm tra xem orders đã được tạo hôm nay chưa
        if (lastDayOrdersCreated == currentDay)
        {
            Debug.LogWarning($"[OrderManager] ⚠️ Orders đã được tạo hôm nay (Day {currentDay}), bỏ qua!");
            return;
        }
        
        Debug.Log($"[OrderManager] 🔄 RefreshDailyOrders() called - Day {currentDay}");

        // Kiểm tra ProductDatabase, nếu chưa có thì tìm lại
        if (productDatabase == null)
        {
            Debug.LogWarning("[OrderManager] ⚠️ ProductDatabase null, tìm lại từ Resources...");
            
            // Tìm từ Resources
            productDatabase = Resources.Load<ProductDatabase>("ProductDatabase");
            
            if (productDatabase != null)
            {
                Debug.Log("[OrderManager] ✅ Tìm thấy ProductDatabase từ Resources");
            }
            else
            {
                Debug.LogError("[OrderManager] ❌ KHÔNG TÌM THẤY ProductDatabase!");
                Debug.LogError("[OrderManager] ⚠️ GIẢI PHÁP:");
                Debug.LogError("[OrderManager]    A) Gán ProductDatabase vào field trong Inspector");
                Debug.LogError("[OrderManager]    B) Copy ProductDatabase.asset vào Assets/Resources folder");
                return;
            }
        }

        // Tạo 1 order test (10 CornCrop) mỗi ngày
        var testOrder = GenerateTestOrder();
        if (testOrder == null)
        {
            Debug.LogError("[OrderManager] ❌ Tạo test order thất bại!");
            return;
        }
        
        // Tạo 1 order từ AI mỗi ngày
        var aiOrder = GenerateNewOrder();
        if (aiOrder == null)
        {
            Debug.LogError("[OrderManager] ❌ Tạo AI order thất bại!");
            return;
        }

        // ✅ Đánh dấu đã tạo orders hôm nay
        lastDayOrdersCreated = currentDay;
        
        Debug.Log($"[OrderManager] ✅ Tạo thành công: Test Order #{testOrder.id} + AI Order #{aiOrder.id}");
        Debug.Log($"[OrderManager] ✅ Đánh dấu đã tạo orders cho Day {currentDay}");

        // Lưu lên Firebase
        string userId = PlayerSession.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[OrderManager] ❌ PlayerSession.GetCurrentUserId() trả về null!");
            return;
        }

        SaveOrdersToFirebase(userId);
    }

    /// <summary>
    /// Lấy ngày hiện tại từ DayAndNightManager (hoặc TimeManager)
    /// </summary>
    private int GetCurrentGameDay()
    {
        DayAndNightManager dayMgr = DayAndNightManager.Instance;
        if (dayMgr != null)
        {
            // Dùng reflection để lấy private field currentDay
            var field = dayMgr.GetType().GetField("currentDay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                int day = (int)field.GetValue(dayMgr);
                return day;
            }
        }
        
        Debug.LogWarning("[OrderManager] ⚠️ Không tìm thấy ngày hiện tại, dùng default day = 0");
        return 0;
    }

    /// <summary>
    /// HÀM CHÍNH: Tạo đơn hàng mới từ AI và đẩy vào pending
    /// </summary>
    public Order GenerateNewOrder()
    {
        if (orderGenerator == null)
        {
            Debug.LogError("Không tìm thấy AIGenerateOrder!");
            return null;
        }

        Order newOrder = orderGenerator.GenerateNewOrder();
        if (newOrder != null)
        {
            pendingOrders.Add(newOrder);
            onOrderAdded?.Invoke(newOrder);
            onOrdersListChanged?.Invoke();
            Debug.Log($"[OrderManager] Đơn hàng mới #{newOrder.id} đã được tạo!");
        }
        return newOrder;
    }

    /// <summary>
    /// Tạo order test với 10 CornCrop để kiểm tra logic accept/reject/send
    /// </summary>
    public Order GenerateTestOrder()
    {
        Debug.Log($"[OrderManager] 🧪 GenerateTestOrder() called. ProductDatabase: {productDatabase}");

        if (productDatabase == null)
        {
            Debug.LogError("[OrderManager] ❌ ProductDatabase là null! Không tạo test order!");
            return null;
        }

        // Tìm CornCrop Product
        ProductData cornProduct = null;
        if (productDatabase.products.Count > 0)
        {
            // Tìm sản phẩm Corn trong database
            foreach (var product in productDatabase.products)
            {
                if (product.plant_name.ToLower().Contains("corn"))
                {
                    cornProduct = product;
                    Debug.Log($"[OrderManager] ✅ Tìm thấy Corn: {product.plant_name}");
                    break;
                }
            }
        }

        // Nếu không tìm thấy, tìm sản phẩm đầu tiên (fallback)
        if (cornProduct == null && productDatabase.products.Count > 0)
        {
            cornProduct = productDatabase.products[0];
            Debug.LogWarning($"[OrderManager] ⚠️ Không tìm thấy Corn, dùng sản phẩm đầu tiên: {cornProduct.plant_name}");
        }

        if (cornProduct == null)
        {
            Debug.LogError("[OrderManager] ❌ ProductDatabase trống, không có sản phẩm nào!");
            return null;
        }

        Order testOrder = new Order
        {
            id = Random.Range(50000, 59999), // ID khác nhau để phân biệt order test
            deadlineDays = 3
        };

        // Thêm 10 CornCrop
        testOrder.items.Add(new OrderItem(cornProduct, 10));

        // Tính reward dựa trên giá
        int seedCost = cornProduct.seedCost * 10;
        testOrder.totalReward = Mathf.RoundToInt(seedCost * 3.5f);

        // Content cho order test
        testOrder.content = "🧪 ĐỂ KIỂM TRA LOGIC - Order Test!";
        testOrder.isTestOrder = true; // Đánh dấu đây là order test

        pendingOrders.Add(testOrder);
        onOrderAdded?.Invoke(testOrder);
        onOrdersListChanged?.Invoke();
        Debug.Log($"[OrderManager] ✅ Test Order #{testOrder.id} tạo thành công - 10 {cornProduct.plant_name}");

        return testOrder;
    }

    /// <summary>
    /// Người chơi ĐỒNG Ý đơn hàng
    /// </summary>
    public bool AcceptOrder(Order order)
    {
        if (order == null || !pendingOrders.Contains(order)) return false;

        pendingOrders.Remove(order);
        order.isAccepted = true;
        acceptedOrders.Add(order);

        onOrderRemoved?.Invoke(order);
        onOrdersListChanged?.Invoke();
        Debug.Log($"[OrderManager] Đã chấp nhận đơn hàng #{order.id}");
        return true;
    }

    /// <summary>
    /// Kiểm tra túi đồ có đủ sản phẩm để hoàn thành order không
    /// Nếu không đủ → log số lượng thiếu và return false
    /// </summary>
    public bool CheckInventoryForOrder(Order order, out string missingInfo)
    {
        missingInfo = "";
        
        if (order == null || order.items.Count == 0)
        {
            return true;
        }

        InventoryManager inventoryMgr = InventoryManager.Instance;
        if (inventoryMgr == null)
        {
            Debug.LogError("[OrderManager] Không tìm thấy InventoryManager!");
            missingInfo = "❌ Lỗi: Không tìm thấy Inventory!";
            return false;
        }

        StringBuilder missingList = new StringBuilder();
        bool allItemsAvailable = true;

        // Kiểm tra từng sản phẩm trong order
        foreach (var orderItem in order.items)
        {
            if (orderItem == null || orderItem.product == null)
                continue;

            // Tìm ItemData tương ứng với ProductData
            ItemData requiredItem = FindItemDataForProduct(orderItem.product);
            
            if (requiredItem == null)
            {
                Debug.LogWarning($"[OrderManager] Không tìm thấy ItemData cho sản phẩm: {orderItem.product.plant_name}");
                missingList.AppendLine($"❌ Không tìm thấy sản phẩm '{orderItem.product.plant_name}' trong túi!");
                allItemsAvailable = false;
                continue;
            }

            // Đếm số lượng hiện có trong inventory
            int currentQuantity = GetItemQuantityInInventory(requiredItem);
            int needed = orderItem.quantity;

            if (currentQuantity < needed)
            {
                int missing = needed - currentQuantity;
                missingList.AppendLine($"📦 {orderItem.product.plant_name}: Có {currentQuantity}/{needed} (thiếu {missing})");
                allItemsAvailable = false;
            }
            else
            {
                Debug.Log($"[OrderManager] ✅ {orderItem.product.plant_name}: Có {currentQuantity} (cần {needed}) - Đủ!");
            }
        }

        if (!allItemsAvailable)
        {
            missingInfo = missingList.ToString();
            Debug.Log($"[OrderManager] ⚠️ KHÔNG ĐỦ HÀNG:\n{missingInfo}");
            onInventoryCheckFailed?.Invoke(missingInfo);
        }

        return allItemsAvailable;
    }

    /// <summary>
    /// Tìm ItemData tương ứng với ProductData
    /// ProductData từ Order → ItemData trong Inventory
    /// </summary>
    private ItemData FindItemDataForProduct(ProductData product)
    {
        if (product == null)
            return null;

        ItemDatabase itemDb = ItemDatabase.Instance;
        if (itemDb == null)
            return null;

        // Tìm ItemData có tên trùng với ProductData
        string productName = product.plant_name.ToLower();
        
        // Tìm crop tương ứng (ví dụ: "Corn" → "CornCrop")
        ItemSubtype targetSubtype = ItemSubtype.None;
        
        if (productName.Contains("corn"))
            targetSubtype = ItemSubtype.CornCrop;
        else if (productName.Contains("tomato"))
            targetSubtype = ItemSubtype.TomatoCrop;
        else if (productName.Contains("chili"))
            targetSubtype = ItemSubtype.ChiliCrop;
        else if (productName.Contains("eggplant"))
            targetSubtype = ItemSubtype.EggplantCrop;
        else if (productName.Contains("watermelon"))
            targetSubtype = ItemSubtype.WatermelonCrop;

        if (targetSubtype == ItemSubtype.None)
        {
            Debug.LogWarning($"[OrderManager] Không biết loại crop cho: {product.plant_name}");
            return null;
        }

        // Tìm item có subtype tương ứng
        return itemDb.GetItemBySubtype(targetSubtype);
    }

    /// <summary>
    /// Đếm số lượng của 1 item trong inventory (cả inventory 1 và 2)
    /// </summary>
    private int GetItemQuantityInInventory(ItemData item)
    {
        InventoryManager invMgr = InventoryManager.Instance;
        if (invMgr == null)
            return 0;

        int totalQty = 0;

        // Kiểm tra inventory 1
        for (int i = 0; i < 20; i++) // Giả sử size là 20
        {
            SlotData slot = invMgr.GetSlotData(i);
            if (slot != null && slot.item == item)
            {
                totalQty += slot.quantity;
            }
        }

        // Kiểm tra inventory 2
        for (int i = 0; i < 20; i++)
        {
            SlotData slot = invMgr.GetSecondSlotData(i);
            if (slot != null && slot.item == item)
            {
                totalQty += slot.quantity;
            }
        }

        return totalQty;
    }

    /// <summary>
    /// Người chơi TỪ CHỐI đơn hàng
    /// </summary>
    public bool RejectOrder(Order order)
    {
        if (order == null || pendingOrders.Remove(order))
        {
            onOrderRemoved?.Invoke(order);
            onOrdersListChanged?.Invoke();
            Debug.Log($"[OrderManager] Đã từ chối đơn hàng #{order.id}");

            // ✅ XÓA ORDER KHỎI FIREBASE
            SaveOrdersToFirebase(PlayerSession.GetCurrentUserId());

            return true;
        }
        return false;
    }

    /// <summary>
    /// Người chơi bấm "GIAO HÀNG" → cộng tiền + trừ đồ
    /// </summary>
    public bool DeliverOrder(Order order)
    {
        if (order == null || !acceptedOrders.Contains(order)) return false;

        // Kiểm tra đủ hàng chưa
        if (!CheckInventoryForOrder(order, out string missingInfo))
        {
            Debug.LogError($"[OrderManager] ❌ KHÔNG ĐỦ HÀNG ĐỂ GIAO: {missingInfo}");
            return false;
        }

        // Trừ items từ inventory
        RemoveItemsFromInventory(order);

        acceptedOrders.Remove(order);
        order.isCompleted = true;

        Debug.Log($"[OrderManager] ✅ GIAO HÀNG THÀNH CÔNG - Đơn #{order.id} → +{order.totalReward} vàng!");

        onOrderRemoved?.Invoke(order);
        onOrdersListChanged?.Invoke();

        // ✅ XÓA ORDER KHỎI FIREBASE
        SaveOrdersToFirebase(PlayerSession.GetCurrentUserId());

        return true;
    }

    /// <summary>
    /// Trừ các items trong order khỏi inventory
    /// </summary>
    private void RemoveItemsFromInventory(Order order)
    {
        InventoryManager invMgr = InventoryManager.Instance;
        if (invMgr == null)
        {
            Debug.LogError("[OrderManager] Không tìm thấy InventoryManager!");
            return;
        }

        foreach (var orderItem in order.items)
        {
            if (orderItem == null || orderItem.product == null)
                continue;

            ItemData item = FindItemDataForProduct(orderItem.product);
            if (item == null)
                continue;

            // Trừ số lượng từ inventory
            RemoveItemQuantity(item, orderItem.quantity);
            Debug.Log($"[OrderManager] Đã trừ {orderItem.quantity}x {orderItem.product.plant_name} khỏi túi");
        }
    }

    /// <summary>
    /// Trừ 1 lượng nhất định của item khỏi inventory (từ slot nào có)
    /// </summary>
    private void RemoveItemQuantity(ItemData item, int quantityToRemove)
    {
        InventoryManager invMgr = InventoryManager.Instance;
        if (invMgr == null || quantityToRemove <= 0)
            return;

        int remaining = quantityToRemove;

        // Trừ từ inventory 1 trước
        for (int i = 0; i < 20 && remaining > 0; i++)
        {
            SlotData slot = invMgr.GetSlotData(i);
            if (slot != null && slot.item == item && slot.quantity > 0)
            {
                int removeAmount = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= removeAmount;
                remaining -= removeAmount;

                if (slot.quantity <= 0)
                {
                    slot.item = null;
                    slot.quantity = 0;
                }

                // Refresh UI sẽ được gọi từ invMgr.RefreshInventoryUI()
            }
        }

        // Trừ từ inventory 2 nếu còn
        for (int i = 0; i < 20 && remaining > 0; i++)
        {
            SlotData slot = invMgr.GetSecondSlotData(i);
            if (slot != null && slot.item == item && slot.quantity > 0)
            {
                int removeAmount = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= removeAmount;
                remaining -= removeAmount;

                if (slot.quantity <= 0)
                {
                    slot.item = null;
                    slot.quantity = 0;
                }
            }
        }

        invMgr.RefreshInventoryUI();
        invMgr.RefreshSecondInventoryUI();

        Debug.Log($"[OrderManager] Đã trừ {quantityToRemove - remaining} items (target: {quantityToRemove})");
    }

    /// <summary>
    /// Gọi từ AIGenerateOrder khi AI trả nội dung → cập nhật UI
    /// </summary>
    public void OnOrderContentReady(Order order)
    {
        onOrderContentReady?.Invoke(order);
        onOrdersListChanged?.Invoke(); // Đảm bảo UI refresh text
    }

    // Helper: Lấy tất cả đơn hàng đang active
    public List<Order> GetAllActiveOrders()
    {
        var list = new List<Order>();
        list.AddRange(pendingOrders);
        list.AddRange(acceptedOrders);
        return list;
    }

    // Helper: Tìm đơn hàng theo ID
    public Order FindOrderById(int id)
    {
        return GetAllActiveOrders().FirstOrDefault(o => o.id == id);
    }

    // Xóa hết đơn hàng (dùng khi test hoặc đổi ngày mới)
    public void ClearAllOrdersFull()
    {
        List<Order> allOrders = new List<Order>();
        allOrders.AddRange(pendingOrders);
        allOrders.AddRange(acceptedOrders);

        foreach (var order in allOrders)
        {
            onOrderRemoved?.Invoke(order);
        }

        pendingOrders.Clear();
        acceptedOrders.Clear();

        onOrdersListChanged?.Invoke();

        Debug.Log("[OrderManager] ĐÃ CLEAR TOÀN BỘ ĐƠN HÀNG!");
    }

    public void ClearAllOrders()
    {
        // Gọi sự kiện xóa UI:
        foreach (var order in GetAllActiveOrders())
        {
            onOrderRemoved?.Invoke(order);
        }

        // Xóa sạch data
        pendingOrders.Clear();
        acceptedOrders.Clear();

        // Báo UI cập nhật
        onOrdersListChanged?.Invoke();

        // ✅ CLEAR ORDERS TRÊN FIREBASE (LƯU MẢNG RỖNG)
        SaveOrdersToFirebase(PlayerSession.GetCurrentUserId());

        Debug.Log("<color=red>[OrderManager] Đã xóa tất cả đơn hàng và cập nhật Firebase!</color>");
    }

    /// <summary>
    /// Lưu tất cả pending orders lên Firebase
    /// Cấu trúc: /{userId}/Orders/[{order data}, ...]
    /// </summary>
    public void SaveOrdersToFirebase(string userId)
    {
        Debug.Log($"[OrderManager] 💾 SaveOrdersToFirebase() called for user: {userId}");
        Debug.Log($"[OrderManager] Firebase Ready: {FirebaseDatabaseManager.FirebaseReady}");

        if (!FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.LogError("[OrderManager] ❌ Firebase chưa sẵn sàng → KHÔNG SAVE ORDERS!");
            return;
        }

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[OrderManager] ❌ userId trống → KHÔNG SAVE ORDERS!");
            return;
        }

        try
        {
            // Chuyển danh sách orders thành JSON array
            var ordersData = new List<OrderFirebaseData>();
            foreach (var order in pendingOrders)
            {
                ordersData.Add(new OrderFirebaseData(order));
            }

            string json = JsonConvert.SerializeObject(ordersData, Formatting.Indented);
            
            Debug.Log($"[OrderManager] 📝 JSON to save ({ordersData.Count} orders):\n{json}");

            // Lưu lên: /userId/Orders
            if (FirebaseDatabaseManager.Instance == null)
            {
                Debug.LogError("[OrderManager] ❌ FirebaseDatabaseManager.Instance là null!");
                return;
            }

            FirebaseDatabaseManager.Instance.SaveOrdersToDatabase(userId, json);
            Debug.Log($"[OrderManager] ✅ Gọi SaveOrdersToDatabase() thành công");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OrderManager] ❌ Lỗi save orders: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Load orders từ Firebase và đưa vào pendingOrders
    /// </summary>
    public void LoadOrdersFromFirebase()
    {
        string userId = PlayerSession.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[OrderManager] ❌ PlayerSession.GetCurrentUserId() trả về null!");
            return;
        }

        Debug.Log($"[OrderManager] 📥 LoadOrdersFromFirebase() for user: {userId}");

        if (FirebaseDatabaseManager.Instance == null)
        {
            Debug.LogError("[OrderManager] ❌ FirebaseDatabaseManager.Instance là null!");
            return;
        }

        try
        {
            // Lấy JSON từ Firebase path: /userId/Orders
            FirebaseDatabase.DefaultInstance
                .GetReference($"{userId}/Orders")
                .GetValueAsync()
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"[OrderManager] ❌ Lỗi load orders từ Firebase: {task.Exception}");
                        return;
                    }

                    DataSnapshot snapshot = task.Result;
                    
                    if (!snapshot.Exists)
                    {
                        Debug.LogWarning("[OrderManager] ⚠️ Không có orders trên Firebase (path /Orders rỗng)");
                        return;
                    }

                    string json = snapshot.GetRawJsonValue();
                    if (string.IsNullOrEmpty(json) || json == "null")
                    {
                        Debug.LogWarning("[OrderManager] ⚠️ Orders JSON rỗng hoặc null");
                        return;
                    }

                    Debug.Log($"[OrderManager] 📝 JSON loaded từ Firebase:\n{json}");

                    try
                    {
                        // Nếu JSON là string (bị wrap), cần unwrap
                        if (json.StartsWith("\"") && json.EndsWith("\""))
                        {
                            Debug.Log("[OrderManager] ⚠️ JSON bị wrap trong quotes, unwrap...");
                            json = json.Substring(1, json.Length - 2);
                            // Unescape special characters
                            json = System.Text.RegularExpressions.Regex.Unescape(json);
                        }
                        
                        Debug.Log($"[OrderManager] 📝 JSON sau unwrap:\n{json}");
                        
                        // Parse JSON thành list of OrderFirebaseData
                        var ordersData = JsonConvert.DeserializeObject<List<OrderFirebaseData>>(json);
                        
                        if (ordersData == null || ordersData.Count == 0)
                        {
                            Debug.LogWarning("[OrderManager] ⚠️ Không có orders trong JSON");
                            return;
                        }

                        // Clear pending orders hiện tại
                        pendingOrders.Clear();

                        // Convert từ OrderFirebaseData về Order
                        foreach (var orderData in ordersData)
                        {
                            Order order = orderData.ToOrder(productDatabase);
                            if (order != null)
                            {
                                pendingOrders.Add(order);
                                Debug.Log($"[OrderManager] ✅ Loaded Order #{order.id}");
                            }
                        }

                        Debug.Log($"[OrderManager] ✅ Đã load {pendingOrders.Count} orders từ Firebase");
                        onOrdersListChanged?.Invoke();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[OrderManager] ❌ Lỗi parse JSON: {ex.Message}\n{ex.StackTrace}");
                    }
                });
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OrderManager] ❌ Lỗi load orders: {ex.Message}\n{ex.StackTrace}");
        }
    }

}
