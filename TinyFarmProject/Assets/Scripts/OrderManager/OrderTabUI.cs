using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderTabUI : MonoBehaviour
{
    public TMP_Text orderIdText;
    public Button button;

    private Order order;
    private OrderDetailUI detailUI;

    public void Setup(Order orderData, OrderDetailUI detail)
    {
        order = orderData;
        detailUI = detail;

        orderIdText.text = $"Order #{order.id}";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Debug.Log("ĐÃ CLICK ORDER: " + order.id);
        detailUI.ShowOrder(order);
    }

}
