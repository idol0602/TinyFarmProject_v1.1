using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class OpenDoor : MonoBehaviour
{
    public enum DoorType
    {
        MainDoor,
        NormalDoor
    }

    [Header("=== Door Mode ===")]
    public DoorType doorType = DoorType.MainDoor;

    [Header("=== Door Unique ID ===")]
    public string doorID;

    [Header("=== Animator ===")]
    public Animator doorAnimator;

    [Header("=== Player Spawn Position ===")]
    public Transform spawnPoint;

#if UNITY_EDITOR
    [Header("=== Scene Switching ===")]
    public SceneAsset outdoorScene;
    public SceneAsset indoorScene;
#endif

    [HideInInspector] public string outdoorSceneName;
    [HideInInspector] public string indoorSceneName;

    [Header("=== Timing ===")]
    public float loadDelay = 0.5f;
    public float autoCloseTime = 0f;

    private bool playerInside = false;
    private bool sceneLoading = false;
    private Collider2D playerCollider;

    public static string lastDoorID = "";

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

#if UNITY_EDITOR
        SyncSceneNames();
#endif
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || sceneLoading) return;

        playerInside = true;
        playerCollider = other;

        doorAnimator?.SetBool("isOpen", true);
        CancelInvoke(nameof(CloseDoor));

        if (doorType == DoorType.MainDoor)
            Invoke(nameof(LoadCorrectScene), loadDelay);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != playerCollider) return;

        playerInside = false;
        playerCollider = null;

        if (doorType == DoorType.NormalDoor && !sceneLoading)
        {
            CloseDoor();
            return;
        }

        if (autoCloseTime > 0f && !sceneLoading)
            Invoke(nameof(CloseDoor), autoCloseTime);
    }

    private void LoadCorrectScene()
    {
        if (sceneLoading || doorType != DoorType.MainDoor)
            return;

        lastDoorID = doorID;

        string current = SceneManager.GetActiveScene().name;
        string target = "";

        // ========================= FROM FARM → HOUSE =========================
        if (current == outdoorSceneName)
        {
            Debug.Log("🚪 Rời FARM → SAVE FARM TRƯỚC KHI VÀO NHÀ");

            // 👉 Không dùng Instance nữa, chỉ Find cho chắc
            var firebase = Object.FindObjectOfType<FirebaseDatabaseManager>();
            if (firebase != null)
            {
                firebase.SaveFarmToFirebase("Player1");
            }
            else
            {
                Debug.LogWarning("⚠ Không tìm thấy FirebaseDatabaseManager trong scene, bỏ qua SAVE farm.");
            }

            target = indoorSceneName;
        }
        // ========================= FROM HOUSE → FARM =========================
        else if (current == indoorSceneName)
        {
            Debug.Log("🏡 Rời HOUSE → Không cần save farm");
            target = outdoorSceneName;
        }
        else
        {
            Debug.LogError($"❌ Scene '{current}' không khớp với cấu hình cửa!");
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncSceneNames();
    }

    private void SyncSceneNames()
    {
        outdoorSceneName = outdoorScene != null ? outdoorScene.name : "";
        indoorSceneName = indoorScene != null ? indoorScene.name : "";
    }
#endif
}
