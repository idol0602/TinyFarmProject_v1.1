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
    public Button btnClearAll;

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
        txtReward.text = $"💰 {order.totalReward}";
        txtDeadline.text = $"⏳ {order.deadlineDays} ngày";
        txtNPC.text = string.IsNullOrEmpty(order.content)
            ? "Đang chờ NPC nói..."
            : order.content;

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
            OrderManager.Instance.DeliverOrder(order);
            if (truckMove != null && truckMove.CanRun())
            {
                truckMove.Run();
            }
            Clear();
        });
        btnClearAll.onClick.AddListener(() =>
        {
            OrderManager.Instance.ClearAllOrdersFull();
        });
    }

    public void Clear()
    {
        txtOrderID.text = "Chọn 1 đơn hàng";
        txtItems.text = "";
        txtReward.text = "";
        txtDeadline.text = "";
        txtNPC.text = "";
    }
}
