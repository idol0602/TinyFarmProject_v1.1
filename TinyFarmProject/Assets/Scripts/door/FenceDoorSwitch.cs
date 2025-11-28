using UnityEngine;

public class FenceDoorSwitch : MonoBehaviour
{
    public Transform player;

    public GameObject closedDoorMap; // cửa hàng rào
    public GameObject openDoorMap;   // cửa hàng rào mở

    public float openDistance = 1.5f;

    void Update()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist < openDistance)
        {
            closedDoorMap.SetActive(false);
            openDoorMap.SetActive(true);
        }
        else
        {
            closedDoorMap.SetActive(true);
            openDoorMap.SetActive(false);
        }
    }
}
