using UnityEngine;

public class DoorSpawnOnStart : MonoBehaviour
{
    private void Start()
    {
        // Nếu không có door nào được lưu thì thôi
        if (string.IsNullOrEmpty(OpenDoor.lastDoorID))
            return;

        // Tìm tất cả cửa trong scene hiện tại
        OpenDoor[] doors = FindObjectsOfType<OpenDoor>();

        foreach (var d in doors)
        {
            if (d.doorID == OpenDoor.lastDoorID)
            {
                if (d.spawnPoint != null)
                {
                    // ⭐ TELEPORT PLAYER TỚI ĐÚNG VỊ TRÍ CỬA
                    transform.position = d.spawnPoint.position;
                }
                else
                {
                    Debug.LogWarning($"Door '{d.doorID}' chưa gán SpawnPoint.");
                }

                break;
            }
        }

        // Reset để lần sau không bị xài lại
        OpenDoor.lastDoorID = "";
    }
}
