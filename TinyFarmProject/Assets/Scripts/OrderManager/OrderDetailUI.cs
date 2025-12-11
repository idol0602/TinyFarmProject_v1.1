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

    [Header("Buttons")]
    public Button btnAccept;
    public Button btnReject;
    public Button btnDeliver;

    public Move truckMove;
    private Order currentOrder;

    private void Start()
    {
        Clear();
    }

    public void ShowOrder(Order order)
    {
        currentOrder = order;

        txtOrderID.text = $"ORDER #{order.id}";
        txtItems.text = order.GetItemListString();
        txtReward.text = $"{order.totalReward}";
        txtDeadline.text = $"{order.deadlineDays} ngày";
        txtNPC.text = string.IsNullOrEmpty(order.content)
            ? "Đang chờ NPC nói..."
            : order.content;

        // Khôi phục trạng thái nút khi show đơn mới
        btnAccept.interactable = true;
        btnReject.interactable = true;
        btnDeliver.interactable = true;

        btnAccept.gameObject.SetActive(!order.isAccepted);
        btnReject.gameObject.SetActive(!order.isAccepted);
        btnDeliver.gameObject.SetActive(order.isAccepted);

        btnAccept.onClick.RemoveAllListeners();
        btnReject.onClick.RemoveAllListeners();
        btnDeliver.onClick.RemoveAllListeners();

        btnAccept.onClick.AddListener(() =>
        {
            OrderManager.Instance.AcceptOrder(order);
            ShowOrder(order);
        });

        btnReject.onClick.AddListener(() =>
        {
            OrderManager.Instance.RejectOrder(order);
            Clear();
        });

        btnDeliver.onClick.AddListener(() =>
        {
            // *
            // * KIỂM TRA VÀ XỬ LÝ TRỪ SỐ VẬT PHẨM TƯƠNG ỨNG TRONG TÚI ĐỒ
            // *
            Debug.Log("Reward: " + currentOrder.totalReward);

            OrderManager.Instance.DeliverOrder(order);

            // + tiền
            PlayerMoney.Instance.Add(order.totalReward);

            // save firebase
            FirebaseDatabaseManager.Instance.SaveMoneyToFirebase("Player1");

            // xe chạy
            if (truckMove != null && truckMove.CanRun())
                truckMove.Run();

            // disable nút sau giao hàng
            btnAccept.interactable = false;
            btnReject.interactable = false;
            btnDeliver.interactable = false;

            // clear UI
            Clear();
        });
    }

    public void Clear()
    {
        txtOrderID.text = "Chọn 1 đơn hàng";
        txtItems.text = "";
        txtReward.text = "";
        txtDeadline.text = "";
        txtNPC.text = "";

        // Tắt nút để tránh click nhầm
        btnAccept.interactable = false;
        btnReject.interactable = false;
        btnDeliver.interactable = false;
    }
}
