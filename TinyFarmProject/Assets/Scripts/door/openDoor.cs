using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class OpenDoor : MonoBehaviour
{
    public enum DoorType
    {
        MainDoor,   // ➜ Chuyển scene
        NormalDoor  // ➜ chỉ mở cửa, không load map
    }

    [Header("=== Main Settings ===")]
    public DoorType doorType = DoorType.MainDoor;

    [Header("=== Animator ===")]
    public Animator doorAnimator;

    [Header("=== Scene Switching (Only for MainDoor) ===")]
    public string outdoorSceneName = "mapSummer";
    public string indoorSceneName = "InHouseSence";

    [Header("=== Timing ===")]
    [Tooltip("Thời gian delay trước khi đổi scene (animation)")]
    public float loadDelay = 0.5f;

    [Tooltip("Thời gian tự động đóng cửa nếu KHÔNG đổi scene (nếu = 0 thì bỏ qua)")]
    public float autoCloseTime = 0f;

    // Internal states
    private bool playerInside = false;
    private bool sceneLoading = false;
    private Collider2D playerCollider = null;


    private void Reset()
    {
        doorAnimator = GetComponent<Animator>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        GetComponent<Collider2D>().isTrigger = true;
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (sceneLoading) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        playerCollider = other;

        // 👇 mở cửa
        doorAnimator?.SetBool("isOpen", true);

        // Hủy lệnh đóng nếu có
        CancelInvoke(nameof(CloseDoor));

        // Nếu là cửa chính → chuẩn bị load scene
        if (doorType == DoorType.MainDoor)
        {
            Invoke(nameof(LoadCorrectScene), loadDelay);
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != playerCollider) return;

        playerInside = false;
        playerCollider = null;

        // 🚪 Nếu là cửa thường → đóng NGAY
        if (doorType == DoorType.NormalDoor && !sceneLoading)
        {
            CloseDoor();
            return;
        }

        // ⏳ Nếu có autoCloseTime và không load scene → delay đóng
        if (autoCloseTime > 0f && !sceneLoading)
        {
            Invoke(nameof(CloseDoor), autoCloseTime);
        }
    }





    private void LoadCorrectScene()
    {
        if (sceneLoading || doorType != DoorType.MainDoor) return;

        string current = SceneManager.GetActiveScene().name;
        string target = "";

        if (current == outdoorSceneName)
            target = indoorSceneName;
        else if (current == indoorSceneName)
            target = outdoorSceneName;
        else
        {
            Debug.LogError($"❌ Scene '{current}' không nằm trong cấu hình cửa!");
            return;
        }

        sceneLoading = true;
        SceneManager.LoadScene(target);
    }


    private void CloseDoor()
    {
        if (playerInside || sceneLoading) return;

        doorAnimator?.SetBool("isOpen", false);
    }
}
