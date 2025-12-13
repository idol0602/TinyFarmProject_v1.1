using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderDetailUI : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text txtOrderID;
    public TMP_Text txtItems;
    public TMP_Text txtReward;
    public TMP_Text txtDeadline;
    public TMP_Text txtNPC;
    
    [Header("Error Messages")]
    public TMP_Text txtErrorMessage; // Hiển thị lỗi kiểm tra inventory
    
    [Header("Not Enough Items Panel")]
    public GameObject panelNotEnough; // Panel hiển thị khi không đủ hàng
    public TMP_Text txtNotEnoughMessage; // Text ghi chú số lượng không đủ
    public Button btnClosePanel; // Button close panel

    [Header("Buttons")]
    public Button btnAccept;
    public Button btnReject;
    public Button btnDeliver; // Nút "Send" (giao hàng)

    public Move truckMove;
    private Order currentOrder;

    private void Start()
    {
        Debug.Log("[OrderDetailUI.Start] ===== START INIT =====");
        Debug.Log($"[OrderDetailUI.Start] btnAccept = {btnAccept}");
        Debug.Log($"[OrderDetailUI.Start] btnReject = {btnReject}");
        Debug.Log($"[OrderDetailUI.Start] btnDeliver = {btnDeliver}");
        Debug.Log($"[OrderDetailUI.Start] OrderManager.Instance = {OrderManager.Instance}");
        
        Clear();
        
        // Setup panel không đủ hàng
        if (panelNotEnough != null)
        {
            panelNotEnough.SetActive(false);
        }
        if (btnClosePanel != null)
        {
            btnClosePanel.onClick.RemoveAllListeners();
            btnClosePanel.onClick.AddListener(CloseNotEnoughPanel);
        }
        
        // Subscribe tới event kiểm tra inventory thất bại
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.onInventoryCheckFailed.AddListener(OnInventoryCheckFailed);
            Debug.Log("[OrderDetailUI.Start] Đã subscribe onInventoryCheckFailed event");
        }
        else
        {
            Debug.LogError("[OrderDetailUI.Start] OrderManager.Instance = NULL!");
        }
        
        Debug.Log("[OrderDetailUI.Start] ===== INIT DONE =====");
    }

    public void ShowOrder(Order order)
    {
        Debug.Log($"[OrderDetailUI.ShowOrder] START - Order #{order.id}");
        
        if (order == null)
        {
            Debug.LogError("[OrderDetailUI.ShowOrder] Order is NULL!");
            return;
        }
        
        if (btnAccept == null || btnReject == null || btnDeliver == null)
        {
            Debug.LogError($"[OrderDetailUI.ShowOrder] Button is NULL! btnAccept={btnAccept}, btnReject={btnReject}, btnDeliver={btnDeliver}");
            return;
        }
        
        currentOrder = order;
        ClearErrorMessage();

        txtOrderID.text = $"ORDER #{order.id}";
        txtItems.text = order.GetItemListString();
        txtReward.text = $"{order.totalReward}";
        txtDeadline.text = $"{order.deadlineDays} ngày";
        
        if (txtNPC != null)
            txtNPC.text = string.IsNullOrEmpty(order.content)
                ? "Đang chờ NPC nói..."
                : order.content;

        // ✅ BAN ĐẦU: Hiển thị Accept/Reject, ẩn Send
        // Chỉ hiển thị Send khi user click Accept thành công
        Debug.Log($"[OrderDetailUI.ShowOrder] order.isAccepted = {order.isAccepted}");
        btnAccept.gameObject.SetActive(!order.isAccepted);
        btnReject.gameObject.SetActive(!order.isAccepted);
        
        // Chỉ show Send (btnDeliver) khi đã accept
        bool shouldShowDeliver = order.isAccepted;
        if (OrderManager.Instance != null)
        {
            shouldShowDeliver = order.isAccepted && !OrderManager.Instance.pendingOrders.Contains(order);
        }
        btnDeliver.gameObject.SetActive(shouldShowDeliver);
        Debug.Log($"[OrderDetailUI.ShowOrder] btnAccept.Active={!order.isAccepted}, btnReject.Active={!order.isAccepted}, btnDeliver.Active={shouldShowDeliver}");

        btnAccept.interactable = true;
        btnReject.interactable = true;
        btnDeliver.interactable = true;

        // Clear old listeners
        btnAccept.onClick.RemoveAllListeners();
        btnReject.onClick.RemoveAllListeners();
        btnDeliver.onClick.RemoveAllListeners();

        Debug.Log($"[OrderDetailUI.ShowOrder] Setup listeners cho Order #{order.id}");
        Debug.Log($"[OrderDetailUI.ShowOrder] btnAccept.gameObject.SetActive = {!order.isAccepted}");
        Debug.Log($"[OrderDetailUI.ShowOrder] btnReject.gameObject.SetActive = {!order.isAccepted}");
        Debug.Log($"[OrderDetailUI.ShowOrder] btnDeliver.gameObject.SetActive = {order.isAccepted}");

        // ACCEPT: Kiểm tra inventory trước
        btnAccept.onClick.AddListener(() => 
        {
            Debug.Log("[OrderDetailUI] 🔘 btnAccept clicked!");
            OnAcceptButtonClicked(order);
        });

        // REJECT: Từ chối đơn hàng
        btnReject.onClick.AddListener(() =>
        {
            Debug.Log("[OrderDetailUI] 🔘 btnReject clicked!");
            OrderManager.Instance.RejectOrder(order);
            Clear();
        });

        // DELIVER (Send): Giao hàng
        btnDeliver.onClick.AddListener(() =>
        {
            Debug.Log("[OrderDetailUI] 🔘 btnDeliver clicked!");
            OnDeliverButtonClicked(order);
        });
    }

    /// <summary>
    /// Xử lý khi bấm nút Accept
    /// Kiểm tra inventory, nếu không đủ hàng thì hiển thị panel
    /// </summary>
    private void OnAcceptButtonClicked(Order order)
    {
        Debug.Log($"[OrderDetailUI] Click Accept - Order #{order.id}");
        
        // Kiểm tra túi đồ
        string missingInfo = "";
        bool hasEnough = OrderManager.Instance.CheckInventoryForOrder(order, out missingInfo);
        
        Debug.Log($"[OrderDetailUI] CheckInventory result: {hasEnough}, Missing: {missingInfo}");
        
        if (!hasEnough)
        {
            // ❌ KHÔNG ĐỦ HÀNG - Hiển thị panel
            ShowNotEnoughPanel(missingInfo);
            Debug.Log("[OrderDetailUI] ❌ Không đủ hàng, hiển thị panel thông báo");
            return;
        }

        // ✅ ĐỦ HÀNG - Chấp nhận order
        Debug.Log($"[OrderDetailUI] ✅ Đủ hàng! Gọi AcceptOrder cho Order #{order.id}");
        OrderManager.Instance.AcceptOrder(order);
        Debug.Log($"[OrderDetailUI] ✅ AcceptOrder xong! Order.isAccepted = {order.isAccepted}");
        ShowOrder(order); // Refresh UI (ẩn Accept/Reject, hiển thị Send)
        Debug.Log($"[OrderDetailUI] ShowOrder xong, button đã refresh");
    }
    
    /// <summary>
    /// Hiển thị panel thông báo không đủ hàng
    /// </summary>
    private void ShowNotEnoughPanel(string missingInfo)
    {
        if (panelNotEnough == null)
        {
            Debug.LogWarning("[OrderDetailUI] panelNotEnough chưa được assign!");
            return;
        }
        
        if (txtNotEnoughMessage != null)
        {
            txtNotEnoughMessage.text = $"KHÔNG ĐỦ HÀNG:\n\n{missingInfo}";
        }
        
        panelNotEnough.SetActive(true);
        Debug.Log("[OrderDetailUI] Đã hiển thị panelNotEnough");
    }
    
    /// <summary>
    /// Đóng panel thông báo không đủ hàng
    /// </summary>
    private void CloseNotEnoughPanel()
    {
        if (panelNotEnough != null)
        {
            panelNotEnough.SetActive(false);
            Debug.Log("[OrderDetailUI] Đã đóng panelNotEnough");
        }
    }
    
    /// <summary>
    /// Hiển thị panel thông báo truck đang bận
    /// </summary>
    private void ShowTruckBusyPanel()
    {
        if (panelNotEnough != null)
        {
            if (txtNotEnoughMessage != null)
            {
                txtNotEnoughMessage.text = "TRUCK ĐANG GIAO ĐƠN KHÁC\n\nVui lòng chờ truck quay trở về...";
            }
            panelNotEnough.SetActive(true);
            Debug.Log("[OrderDetailUI] Đã hiển thị panelNotEnough (truck busy)");
        }
        else
        {
            Debug.LogWarning("[OrderDetailUI] panelNotEnough chưa được assign!");
        }
    }

    /// <summary>
    /// Xử lý khi bấm nút Deliver (Send)
    /// </summary>
    private void OnDeliverButtonClicked(Order order)
    {
        ClearErrorMessage();
        
        // ✅ CHECK 1: Kiểm tra truck có đang chạy không
        if (truckMove != null && truckMove.IsRunning)
        {
            Debug.Log("[OrderDetailUI] ❌ Truck đang giao đơn khác, hiển thị panel chờ");
            ShowTruckBusyPanel();
            return;
        }
        
        // ✅ CHECK 2: Kiểm tra lần cuối trước khi giao
        if (!OrderManager.Instance.CheckInventoryForOrder(order, out string missingInfo))
        {
            ShowErrorMessage($"❌ Lỗi giao hàng:\n{missingInfo}");
            return;
        }

        // ✅ GIAO HÀNG
        if (!OrderManager.Instance.DeliverOrder(order))
        {
            ShowErrorMessage("❌ Lỗi giao hàng!");
            return;
        }

        // Cộng tiền
        if (PlayerMoney.Instance != null)
        {
            PlayerMoney.Instance.Add(order.totalReward);
            Debug.Log($"[OrderDetailUI] +{order.totalReward} vàng!");
        }

        // Save Firebase
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.SaveMoneyToFirebase(PlayerSession.GetCurrentUserId());
        }

        // Xe chạy
        if (truckMove != null && truckMove.CanRun())
        {
            truckMove.Run();
        }

        // Disable nút
        btnAccept.interactable = false;
        btnReject.interactable = false;
        btnDeliver.interactable = false;

        // Clear UI
        Clear();
    }

    /// <summary>
    /// Hiển thị thông báo lỗi
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        if (txtErrorMessage != null)
        {
            txtErrorMessage.text = message;
            txtErrorMessage.gameObject.SetActive(true);
        }
        Debug.LogWarning($"[OrderDetailUI] {message}");
    }

    /// <summary>
    /// Xóa thông báo lỗi
    /// </summary>
    private void ClearErrorMessage()
    {
        if (txtErrorMessage != null)
        {
            txtErrorMessage.text = "";
            txtErrorMessage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Callback khi inventory check thất bại (từ OrderManager event)
    /// </summary>
    private void OnInventoryCheckFailed(string missingInfo)
    {
        ShowErrorMessage($"⚠️ KHÔNG ĐỦ HÀNG:\n{missingInfo}");
    }

    public void Clear()
    {
        currentOrder = null;
        ClearErrorMessage();
        
        txtOrderID.text = "Chọn 1 đơn hàng";
        txtItems.text = "";
        txtReward.text = "";
        txtDeadline.text = "";
        if (txtNPC != null)
            txtNPC.text = "";

        // Tắt nút để tránh click nhầm
        if (btnAccept != null) btnAccept.interactable = false;
        if (btnReject != null) btnReject.interactable = false;
        if (btnDeliver != null) btnDeliver.interactable = false;
        
        // Ẩn tất cả nút
        if (btnAccept != null) btnAccept.gameObject.SetActive(false);
        if (btnReject != null) btnReject.gameObject.SetActive(false);
        if (btnDeliver != null) btnDeliver.gameObject.SetActive(false);
    }
}
