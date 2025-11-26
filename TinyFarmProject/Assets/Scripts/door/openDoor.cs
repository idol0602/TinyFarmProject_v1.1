using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class openDoor : MonoBehaviour
{
    [Header("=== Animator ===")]
    public Animator doorAnimator;

    [Header("=== Tên 2 Scene (CHÍNH XÁC NHƯ TRONG BUILD SETTINGS) ===")]
    public string outdoorSceneName = "mapSummer";     // Scene ngoài trời
    public string indoorSceneName = "InHouseSence";  // Scene trong nhà

    [Header("=== Thời gian ===")]
    [Tooltip("Chờ bao lâu sau khi mở cửa mới chuyển scene (thời gian animation)")]
    public float loadDelay = 0.5f;

    [Tooltip("Tự động đóng cửa nếu không chuyển scene (đặt 0 nếu luôn chuyển)")]
    public float autoCloseTime = 0f;

    private bool playerInside = false;
    private Collider2D playerCollider = null;
    private bool sceneLoading = false;

    private void Reset()
    {
        // Tự động gán khi kéo script vào
        doorAnimator = GetComponent<Animator>();
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Debug.Log($"Cửa sẵn sàng | Scene hiện tại: <b>{SceneManager.GetActiveScene().name}</b> " +
                  $"(Outdoor: {outdoorSceneName} | Indoor: {indoorSceneName})");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (sceneLoading) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        playerCollider = other;

        // Mở cửa
        if (doorAnimator != null)
            doorAnimator.SetBool("isOpen", true);

        Debug.Log("Cửa MỞ!");

        // Hủy đóng tự động (vì sẽ chuyển scene)
        CancelInvoke(nameof(CloseDoor));

        // Chuyển scene đúng chiều
        Invoke(nameof(LoadCorrectScene), loadDelay);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != playerCollider) return;

        playerInside = false;
        playerCollider = null;

        if (autoCloseTime > 0f && !sceneLoading)
            Invoke(nameof(CloseDoor), autoCloseTime);
    }

    // HÀM QUAN TRỌNG NHẤT – TỰ ĐỘNG CHUYỂN ĐÚNG CHIỀU
    private void LoadCorrectScene()
    {
        if (sceneLoading) return;

        string current = SceneManager.GetActiveScene().name;
        string target = "";

        if (current == outdoorSceneName)
        {
            target = indoorSceneName;   // Từ ngoài → vào nhà
            Debug.Log($"Từ <b>{outdoorSceneName}</b> → VÀO NHÀ <b>{indoorSceneName}</b>");
        }
        else if (current == indoorSceneName)
        {
            target = outdoorSceneName;  // Từ trong nhà → ra ngoài
            Debug.Log($"Từ <b>{indoorSceneName}</b> → RA NGOÀI <b>{outdoorSceneName}</b>");
        }
        else
        {
            Debug.LogError($"Scene hiện tại '{current}' không phải outdoor hoặc indoor! Không chuyển scene.");
            CloseDoor();
            return;
        }

        sceneLoading = true;
        SceneManager.LoadScene(target);
    }

    private void CloseDoor()
    {
        if (playerInside || sceneLoading) return;

        if (doorAnimator != null)
            doorAnimator.SetBool("isOpen", false);

        Debug.Log("Cửa ĐÓNG!");
    }

    // Gizmo để thấy vùng trigger
    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = sceneLoading ? Color.magenta :
                       playerInside ? Color.green : Color.red;

        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }

    // Debug nhanh bằng phím T (nếu muốn)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && doorAnimator != null)
        {
            bool open = doorAnimator.GetBool("isOpen");
            var clip = doorAnimator.GetCurrentAnimatorClipInfo(0);
            string clipName = clip.Length > 0 ? clip[0].clip.name : "None";
            Debug.Log($"[TEST] isOpen={open} | Clip: {clipName} | Scene: {SceneManager.GetActiveScene().name}");
        }
    }
}