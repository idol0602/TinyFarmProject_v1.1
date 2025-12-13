using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FarmLoadingManager : MonoBehaviour
{
    public static FarmLoadingManager Instance { get; private set; }

    [SerializeField] private GameObject loadingScreenPanel;  // Panel chứa loading UI (Image, Text, etc.)
    [SerializeField] private Text loadingText;              // Text để hiển thị "Loading..."
    [SerializeField] private float maxWaitTime = 30f;        // Timeout sau 30 giây

    private bool isLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Subscribe vào Firebase farm load event
        FirebaseDatabaseManager.OnFarmLoadComplete += OnFarmLoadComplete;
    }

    private void OnDisable()
    {
        FirebaseDatabaseManager.OnFarmLoadComplete -= OnFarmLoadComplete;
    }

    public void StartLoadingFarm(string userId = "Player1")
    {
        if (isLoading)
        {
            Debug.LogWarning("[FarmLoadingManager] Already loading, skip");
            return;
        }

        StartCoroutine(LoadFarmWithTimeout(userId));
    }

    private IEnumerator LoadFarmWithTimeout(string userId)
    {
        isLoading = true;
        
        // Hiển thị loading screen
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(true);

        if (loadingText != null)
            loadingText.text = "Loading Farm...";

        Debug.Log("[FarmLoadingManager] Starting farm load...");

        // Gọi load farm từ Firebase
        FirebaseDatabaseManager.Instance.LoadFarmFromFirebase(userId);

        // Đợi cho đến khi farm load xong hoặc timeout
        float elapsedTime = 0f;
        while (!FirebaseDatabaseManager.Instance.IsFarmLoaded && elapsedTime < maxWaitTime)
        {
            if (loadingText != null)
                loadingText.text = $"Loading Farm... ({elapsedTime:F1}s)";

            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (elapsedTime >= maxWaitTime)
        {
            Debug.LogError("[FarmLoadingManager] ❌ Farm load TIMEOUT after 30 seconds!");
        }
        else
        {
            Debug.Log($"[FarmLoadingManager] ✅ Farm loaded in {elapsedTime:F1} seconds");
        }

        // Ẩn loading screen
        yield return new WaitForSeconds(0.5f);  // Thêm delay nhỏ để smooth transition
        
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(false);

        isLoading = false;
        Debug.Log("[FarmLoadingManager] Loading complete!");
    }

    private void OnFarmLoadComplete(bool success)
    {
        Debug.Log($"[FarmLoadingManager] Farm load event received: {success}");
    }

    public bool IsLoading => isLoading;
}
