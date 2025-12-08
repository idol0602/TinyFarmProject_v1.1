using UnityEngine;
using UnityEngine.UI;
public class CloseShopMenu : MonoBehaviour
{
    [Header("References")]
    public GameObject shopMenu;
    public PlayerHandler player;

    [Header("Settings")]
    public float delayBeforeMoveAgain = 0.5f;

    // GỌI TỪ BUTTON → DÙ OBJECT CÓ BỊ TẮT THÌ VẪN CHẠY ĐƯỢC
    public void Close()
    {
        if (shopMenu != null)
        {
            shopMenu.SetActive(false);
        }
    }
}