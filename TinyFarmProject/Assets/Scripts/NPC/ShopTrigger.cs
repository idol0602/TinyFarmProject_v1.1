using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    public GameObject shopMenu;
    public PlayerHandler player; // Reference Player Controller

    private void OnMouseDown()
    {
        shopMenu.SetActive(true);
        player.ForceStopAllActions();
        player.LockPlayerMovement(true);
    }
}
