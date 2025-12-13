using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Hiển thị Username (từ email) + Day (từ DayAndNightManager)
/// Persistent across scenes - tương tự PlayerMoney
/// </summary>
public class GameHeaderUI : MonoBehaviour
{
    public static GameHeaderUI Instance { get; private set; }

    [Header("=== UI References ===")]
    [SerializeField] private TextMeshProUGUI usernameTextUI;

    private string currentUsername = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[GameHeaderUI] Awake - Singleton created");
    }

    // 🔧 Subscribe to scene load event (tương tự PlayerMoney)
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[GameHeaderUI] OnEnable - Subscribed to sceneLoaded");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[GameHeaderUI] OnDisable - Unsubscribed from sceneLoaded");
    }

    // 🔧 Gọi khi scene load - tìm lại UI
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameHeaderUI] Scene loaded: {scene.name}");
        StartCoroutine(RefindUIAfterSceneLoad());
    }

    // 🔧 Coroutine đợi scene setup xong rồi tìm UI
    private IEnumerator RefindUIAfterSceneLoad()
    {
        yield return null;  // Đợi 1 frame để scene setup hoàn toàn

        Debug.Log("[GameHeaderUI] Refinding UI in new scene...");
        RefindUIElements();
        UpdateUI();
    }

    private void Start()
    {
        Debug.Log("[GameHeaderUI] Start() called");
        
        // Lấy username từ Firebase Auth
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
            currentUsername = ExtractUsername(user.Email);
            Debug.Log($"[GameHeaderUI] Username from Firebase: {currentUsername}");
        }
        else
        {
            Debug.LogWarning("[GameHeaderUI] ⚠️ Firebase user not found!");
            currentUsername = "Guest";
        }

        RefindUIElements();
        UpdateUI();
    }

    private void Update()
    {
        // Không cần update (day được xử lý bởi DayAndNightManager)
    }

    /// <summary>
    /// Tìm lại UI elements trong scene mới
    /// </summary>
    private void RefindUIElements()
    {
        // Kiểm tra xem reference còn valid không
        if (usernameTextUI != null && usernameTextUI.gameObject.activeInHierarchy)
        {
            Debug.Log("[GameHeaderUI] ✅ usernameTextUI reference still valid");
        }
        else
        {
            usernameTextUI = null;
            Debug.Log("[GameHeaderUI] Searching for UsernameText...");

            // Tìm theo tag
            GameObject usernameGO = GameObject.FindWithTag("UsernameText");
            if (usernameGO != null)
            {
                usernameTextUI = usernameGO.GetComponent<TextMeshProUGUI>();
                Debug.Log("[GameHeaderUI] ✅ Found UsernameText via tag");
            }
            else
            {
                // Tìm trong Canvas
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    Transform usernameTransform = canvas.transform.Find("UsernameText");
                    if (usernameTransform != null)
                    {
                        usernameTextUI = usernameTransform.GetComponent<TextMeshProUGUI>();
                        Debug.Log("[GameHeaderUI] ✅ Found UsernameText in Canvas");
                    }
                }
            }

            if (usernameTextUI == null)
            {
                Debug.LogWarning("[GameHeaderUI] ⚠️ Could not find UsernameText UI");
            }
        }
    }

    /// <summary>
    /// Cập nhật UI text (chỉ username)
    /// </summary>
    private void UpdateUI()
    {
        // Update username
        if (usernameTextUI != null)
        {
            usernameTextUI.text = currentUsername;
            Debug.Log($"[GameHeaderUI] Updated Username UI: {currentUsername}");
        }
    }

    /// <summary>
    /// Tách username từ email (phần trước @)
    /// Ví dụ: abc@iuh.com → abc
    /// </summary>
    private string ExtractUsername(string email)
    {
        if (string.IsNullOrEmpty(email)) return "Unknown";
        
        int atIndex = email.IndexOf('@');
        if (atIndex > 0)
        {
            return email.Substring(0, atIndex);
        }
        return email;
    }

    /// <summary>
    /// Static helper - lấy username hiện tại
    /// </summary>
    public static string GetCurrentUsername()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
            int atIndex = user.Email.IndexOf('@');
            if (atIndex > 0)
            {
                return user.Email.Substring(0, atIndex);
            }
            return user.Email;
        }
        return "Unknown";
    }
}
