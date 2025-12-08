using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    [Header("References")]
    public GameObject shopMenu;
    public PlayerHandler player; // Kéo Player vào Inspector
    private void OnMouseDown()
    {
        if (shopMenu == null || player == null) return;
        shopMenu.SetActive(true);
    }
}