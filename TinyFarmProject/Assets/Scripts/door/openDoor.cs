using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor; // để chọn SceneAsset
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

    [Header("=== Animator ===")]
    public Animator doorAnimator;

    [Header("=== Scene Switching (Only for MainDoor) ===")]
#if UNITY_EDITOR
    public SceneAsset outdoorScene;
    public SceneAsset indoorScene;
#endif

    [HideInInspector] public string outdoorSceneName;
    [HideInInspector] public string indoorSceneName;

    [Header("=== Timing ===")]
    public float loadDelay = 0.5f;
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

        // đảm bảo sceneName cập nhật khi play
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
        if (sceneLoading || doorType != DoorType.MainDoor) return;

        string current = SceneManager.GetActiveScene().name;
        string target = "";

        if (current == outdoorSceneName)
            target = indoorSceneName;
        else if (current == indoorSceneName)
            target = outdoorSceneName;
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
    // Tự động convert SceneAsset → tên scene runtime
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
