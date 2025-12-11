using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OrderManager : MonoBehaviour
{
    // Singleton để gọi từ bất kỳ đâu trong game
    public static OrderManager Instance { get; private set; }

    [Header("References")]
    public AIGenerateOrder orderGenerator;

    [Header("Danh sách đơn hàng")]
    public List<Order> pendingOrders = new List<Order>();   // Chờ người chơi xem & chấp nhận
    public List<Order> acceptedOrders = new List<Order>();  // Đã nhận, đang chuẩn bị giao

    // Events để UI tự động cập nhật (rất quan trọng!)
    public UnityEvent<Order> onOrderAdded = new UnityEvent<Order>();
    public UnityEvent<Order> onOrderRemoved = new UnityEvent<Order>();
    public UnityEvent<Order> onOrderContentReady = new UnityEvent<Order>(); // Khi AI trả về nội dung đẹp
    public UnityEvent onOrdersListChanged = new UnityEvent(); // Khi danh sách thay đổi

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

        // Tạo sẵn 2-3 đơn hàng khi vào game (tùy bạn bật/tắt)
        RefreshDailyOrders();
    }

    /// <summary>
    /// Gọi mỗi ngày mới trong game hoặc khi người chơi mở bảng đơn hàng
    /// </summary>
    public void RefreshDailyOrders(int count = 3)
    {
        for (int i = 0; i < count; i++)
        {
            GenerateNewOrder();
        }
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
    /// Người chơi TỪ CHỐI đơn hàng
    /// </summary>
    public bool RejectOrder(Order order)
    {
        if (order == null || pendingOrders.Remove(order))
        {
            onOrderRemoved?.Invoke(order);
            onOrdersListChanged?.Invoke();
            Debug.Log($"[OrderManager] Đã từ chối đơn hàng #{order.id}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Người chơi bấm "GIAO HÀNG" → cộng tiền + trừ đồ (sẽ thêm sau)
    /// </summary>
    public bool DeliverOrder(Order order)
    {
        if (order == null || !acceptedOrders.Contains(order)) return false;

        // TODO: Sau này thêm:
        // - Kiểm tra túi đồ có đủ hàng không
        // - Trừ số lượng trong Inventory
        // - Cộng tiền vào PlayerMoney

        acceptedOrders.Remove(order);
        order.isCompleted = true;

        // Cộng tiền ngay (tạm thời log)
        Debug.Log($"[OrderManager] GIAO HÀNG THÀNH CÔNG] Đơn #{order.id} → +{order.totalReward} vàng!");

        onOrderRemoved?.Invoke(order);
        onOrdersListChanged?.Invoke();
        return true;
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

        Debug.Log("<color=red>[OrderManager] Đã xóa tất cả đơn hàng!</color>");
    }

}