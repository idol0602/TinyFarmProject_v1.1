using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(-101)]  // 🔧 Chạy TRƯỚC DayAndNightManager (-100)
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

        // 🔧 Không load day/time trong Awake, chờ Firebase ready
        Debug.Log("[FarmLoadingManager] Awake: Firebase not ready yet, will preload in Start");
    }

    private void Start()
    {
        // 🔧 Load day/time ngay trong Start khi Firebase sẵn sàng
        Debug.Log($"[FarmLoadingManager] Start: Firebase ready? {FirebaseDatabaseManager.FirebaseReady}");
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[FarmLoadingManager] Start: Firebase ready, preloading day/time...");
            PreloadDayAndTimeFromFirebase(PlayerSession.GetCurrentUserId());
        }
        else
        {
            Debug.LogWarning("[FarmLoadingManager] Start: Firebase NOT ready yet, waiting...");
            StartCoroutine(WaitForFirebaseAndPreload(PlayerSession.GetCurrentUserId()));
        }
    }

    private IEnumerator WaitForFirebaseAndPreload(string userId)
    {
        float waitTime = 0f;
        while (!FirebaseDatabaseManager.FirebaseReady && waitTime < 10f)
        {
            Debug.Log("[FarmLoadingManager] Waiting for Firebase...");
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[FarmLoadingManager] Firebase ready! Preloading day/time...");
            PreloadDayAndTimeFromFirebase(userId);
        }
        else
        {
            Debug.LogError("[FarmLoadingManager] Firebase NOT ready after 10 seconds!");
        }
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

    public void StartLoadingFarm(string userId = null)
    {
        // 🔧 Nếu userId null, lấy từ PlayerSession
        if (string.IsNullOrEmpty(userId))
        {
            userId = PlayerSession.GetCurrentUserId();
        }
        
        // 🔧 Clear cache để force load dữ liệu mới từ Firebase
        if (FirebaseDatabaseManager.Instance != null)
        {
            Debug.Log("[FarmLoadingManager] Clearing cache before load");
            FirebaseDatabaseManager.Instance.ClearCacheForNewUser();
        }
        
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
            loadingText.text = "Loading...";

        Debug.Log("[FarmLoadingManager] Starting farm load...");

        // ⭐ Load day/time từ Firebase TRƯỚC
        FirebaseDatabaseManager.DayTimeData loadedDayTime = null;
        bool dayTimeLoaded = false;

        FirebaseDatabaseManager.Instance.LoadDayAndTimeFromFirebase(userId, (dayTimeData) =>
        {
            loadedDayTime = dayTimeData;
            dayTimeLoaded = true;
            Debug.Log($"[FarmLoadingManager] ✓ Day/time loaded: Day {dayTimeData.currentDay} {dayTimeData.currentHour:00}:{dayTimeData.currentMinute:00}");
        });

        // Đợi day/time load xong
        float elapsedTime = 0f;
        while (!dayTimeLoaded && elapsedTime < 5f)
        {
            elapsedTime += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        // ⭐ Apply day/time NGAY SAU KHI LOAD
        if (loadedDayTime != null && DayAndNightManager.Instance != null)
        {
            Debug.Log($"[FarmLoadingManager] ⭐ Applying day/time: Day {loadedDayTime.currentDay} {loadedDayTime.currentHour:00}:{loadedDayTime.currentMinute:00}");
            DayAndNightManager.Instance.SetGameTime(loadedDayTime.currentDay, loadedDayTime.currentHour, loadedDayTime.currentMinute);
        }
        else
        {
            Debug.LogWarning("[FarmLoadingManager] ⚠ Day/time load failed, using default");
        }

        // Giờ load farm
        Debug.Log("[FarmLoadingManager] Loading farm...");
        FirebaseDatabaseManager.Instance.LoadFarmFromFirebase(userId);

        // Đợi farm load xong hoặc timeout
        elapsedTime = 0f;
        while (!FirebaseDatabaseManager.Instance.IsFarmLoaded && elapsedTime < maxWaitTime)
        {
            if (loadingText != null)
                loadingText.text = $"Loading... ({elapsedTime:F1}s)";

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

    private void PreloadDayAndTimeFromFirebase(string userId)
    {
        Debug.Log($"[FarmLoadingManager] PreloadDayAndTimeFromFirebase called. Firebase.Ready={FirebaseDatabaseManager.FirebaseReady}, Instance={FirebaseDatabaseManager.Instance}");
        
        if (!FirebaseDatabaseManager.FirebaseReady || FirebaseDatabaseManager.Instance == null)
        {
            Debug.LogError("[FarmLoadingManager] ❌ Firebase not ready or Instance is null!");
            return;
        }

        Debug.Log("[FarmLoadingManager] Loading day/time from Firebase...");
        FirebaseDatabaseManager.Instance.LoadDayAndTimeFromFirebase(userId, (dayTimeData) =>
        {
            Debug.Log($"[FarmLoadingManager] ✅ Callback received! Day {dayTimeData.currentDay} {dayTimeData.currentHour:00}:{dayTimeData.currentMinute:00}");
            
            if (dayTimeData != null && DayAndNightManager.Instance != null)
            {
                Debug.Log($"[FarmLoadingManager] ⭐ Setting game time in callback");
                DayAndNightManager.Instance.SetGameTime(dayTimeData.currentDay, dayTimeData.currentHour, dayTimeData.currentMinute);
            }
            else
            {
                Debug.LogWarning($"[FarmLoadingManager] dayTimeData={dayTimeData}, DayAndNightManager.Instance={DayAndNightManager.Instance}");
            }
        });
    }
}
