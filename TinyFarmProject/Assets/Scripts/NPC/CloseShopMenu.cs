using UnityEngine;

public class CloseShopMenu : MonoBehaviour
{
    public GameObject shopMenu;
    public PlayerHandler player;

    public void Close()
    {
        shopMenu.SetActive(false);
        player.LockPlayerMovement(false);
    }
}
