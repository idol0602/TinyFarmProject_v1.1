using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class DayAndNightManager : MonoBehaviour
{
    public static DayAndNightManager Instance { get; private set; }

    [Header("=== GIỜ BẮT ĐẦU GAME ===")]
    [Range(0, 23)] public int startHour = 7;
    [Range(0, 59)] public int startMinute = 0;

    [Header("=== HIỂN THỊ ===")]
    public TMP_Text textTimeInGame;
    public TMP_Text textDayInGame;

    [Header("=== TỐC ĐỘ THỜI GIAN ===")]
    public float realMinutesPerGameDay = 5f;

    [Header("=== ÁNH SÁNG ===")]
    public Light2D globalLight;
    public Gradient gradient;

    private const float SECONDS_PER_DAY = 86400f;
    private float timeScale;
    private float totalGameSeconds = 0f;
    private int currentDay = 1;

    private static float savedTotalGameSeconds = -1f;
    private bool isGameTimeSet = false;  // 🔧 Track xem đã set game time từ Firebase hay chưa
    private bool hasInitializedTime = false;  // 🔧 Track xem đã setup time trong Start hay chưa
    private bool shouldUpdateUI = false;  // 🔧 Track xem nên update UI hay chưa (chờ Firebase ready)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 🔧 CHỈ khởi tạo timeScale, KHÔNG set time trong Awake
        // Để cho Firebase callback kịp gọi SetGameTime() trước Start()
        timeScale = SECONDS_PER_DAY / (realMinutesPerGameDay * 60f);
        
        // Set default time để game không bị lỗi nếu Firebase chậm
        if (!isGameTimeSet)
        {
            totalGameSeconds = (startHour * 3600f) + (startMinute * 60f);
            currentDay = 1;
        }
        
        // 🔧 KHÔNG gọi UpdateUIAndLight() ở đây - chờ cho Firebase ready
        Debug.Log("[DayAndNightManager] Awake: Skipping UI update - waiting for Firebase");
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedUpdate());
    }

    private System.Collections.IEnumerator DelayedUpdate()
    {
        yield return null;
        FindUIReferences();
        
        // 🔧 KHÔNG update UI ngay - chờ Firebase ready
        if (shouldUpdateUI)
        {
            UpdateUIAndLight();
        }
        else
        {
            Debug.Log("[DayAndNightManager] DelayedUpdate: Skipping UpdateUI - waiting for Firebase");
        }
    }

    private void FindUIReferences()
    {
        Debug.Log("[DayAndNightManager] FindUIReferences() started");
        
        // Kiểm tra xem reference còn valid không
        if (textTimeInGame != null && textTimeInGame.gameObject.activeInHierarchy)
        {
            Debug.Log("[DayAndNightManager] ✅ textTimeInGame reference still valid");
        }
        else
        {
            textTimeInGame = null;
            
            // Cách 1: Tìm theo tag "TimeText"
            GameObject timeTextGO = GameObject.FindWithTag("TimeText");
            if (timeTextGO != null)
            {
                textTimeInGame = timeTextGO.GetComponent<TMP_Text>();
                if (textTimeInGame != null)
                {
                    Debug.Log("[DayAndNightManager] ✅ Found TimeText via tag");
                }
            }
            
            // Cách 2: Tìm theo GameObject.Find
            if (textTimeInGame == null)
            {
                GameObject timeGO = GameObject.Find("TimeText");
                if (timeGO != null)
                {
                    textTimeInGame = timeGO.GetComponent<TMP_Text>();
                    if (textTimeInGame != null)
                    {
                        Debug.Log("[DayAndNightManager] ✅ Found TimeText via GameObject.Find");
                    }
                }
            }
            
            if (textTimeInGame == null)
            {
                Debug.LogWarning("[DayAndNightManager] ⚠️ Could not find TimeText UI");
            }
        }

        // Tìm DayText UI
        if (textDayInGame != null && textDayInGame.gameObject.activeInHierarchy)
        {
            Debug.Log("[DayAndNightManager] ✅ textDayInGame reference still valid");
        }
        else
        {
            textDayInGame = null;
            
            // Cách 1: Tìm theo tag "DayText"
            GameObject dayTextGO = GameObject.FindWithTag("DayText");
            if (dayTextGO != null)
            {
                textDayInGame = dayTextGO.GetComponent<TMP_Text>();
                if (textDayInGame != null)
                {
                    Debug.Log("[DayAndNightManager] ✅ Found DayText via tag");
                }
            }
            
            // Cách 2: Tìm theo GameObject.Find
            if (textDayInGame == null)
            {
                GameObject dayGO = GameObject.Find("DayText");
                if (dayGO != null)
                {
                    textDayInGame = dayGO.GetComponent<TMP_Text>();
                    if (textDayInGame != null)
                    {
                        Debug.Log("[DayAndNightManager] ✅ Found DayText via GameObject.Find");
                    }
                }
            }
            
            if (textDayInGame == null)
            {
                Debug.LogWarning("[DayAndNightManager] ⚠️ Could not find DayText UI");
            }
        }

        GameObject lightObj = GameObject.Find("Global Light 2D") ?? GameObject.FindWithTag("GlobalLight");
        if (lightObj != null)
            globalLight = lightObj.GetComponent<Light2D>();
    }

    private void Start()
    {
        Debug.Log($"[DayAndNightManager] Start() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}, CachedDayTimeData: {FirebaseDatabaseManager.CachedDayTimeData}");
        
        // 🔧 Reset time state khi scene mới load
        isGameTimeSet = false;
        shouldUpdateUI = false;
        savedTotalGameSeconds = -1f;
        
        FindUIReferences();  // Tìm UI trước
        
        if (!hasInitializedTime)
        {
            // ⭐ Cách 1: Nếu cache đã sẵn sàng, dùng ngay
            if (FirebaseDatabaseManager.CachedDayTimeData != null && !isGameTimeSet)
            {
                Debug.Log($"[DayAndNightManager] ✅ Cache ready! Applying day/time...");
                ApplyDayTime(FirebaseDatabaseManager.CachedDayTimeData);
            }
            // ⭐ Cách 2: Nếu Firebase ready nhưng cache chưa, load ngay
            else if (FirebaseDatabaseManager.FirebaseReady && !isGameTimeSet)
            {
                Debug.Log($"[DayAndNightManager] Firebase ready, loading day/time directly...");
                FirebaseDatabaseManager.Instance.LoadDayAndTimeFromFirebase(PlayerSession.GetCurrentUserId(), ApplyDayTime);
            }
            // ⭐ Cách 3: Firebase chưa ready, đợi 1 giây rồi thử lại
            else if (!isGameTimeSet)
            {
                Debug.LogWarning("[DayAndNightManager] Firebase NOT ready, retrying in 1 second...");
                Invoke(nameof(TryLoadDayTimeAgain), 1f);
            }
            
            hasInitializedTime = true;
        }

        // 🔧 Update UI SAU KHI đã load Firebase data (nếu có sẵn)
        // Nếu Firebase chậm, sẽ update lại trong ApplyDayTime() hoặc TryLoadDayTimeAgain()
        if (isGameTimeSet || FirebaseDatabaseManager.CachedDayTimeData != null)
        {
            Debug.Log($"[DayAndNightManager Start()] ⚠️ Condition TRUE: isGameTimeSet={isGameTimeSet}, CachedDayTimeData={FirebaseDatabaseManager.CachedDayTimeData != null}");
            shouldUpdateUI = true;
            UpdateUIAndLight();
        }
        else
        {
            Debug.Log($"[DayAndNightManager Start()] ⏸️ Condition FALSE: isGameTimeSet={isGameTimeSet}, CachedDayTimeData={FirebaseDatabaseManager.CachedDayTimeData != null}");
        }
    }

    // Retry mechanism tương tự PlayerMoney
    private void TryLoadDayTimeAgain()
    {
        Debug.Log($"[DayAndNightManager] TryLoadDayTimeAgain() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}");
        
        if (FirebaseDatabaseManager.CachedDayTimeData != null && !isGameTimeSet)
        {
            Debug.Log($"[DayAndNightManager] ✅ Cache now ready! Applying day/time...");
            ApplyDayTime(FirebaseDatabaseManager.CachedDayTimeData);
        }
        else if (FirebaseDatabaseManager.FirebaseReady && !isGameTimeSet)
        {
            Debug.Log("[DayAndNightManager] Retrying Firebase load...");
            FirebaseDatabaseManager.Instance.LoadDayAndTimeFromFirebase(PlayerSession.GetCurrentUserId(), ApplyDayTime);
        }
        else if (!isGameTimeSet)
        {
            Debug.LogError("[DayAndNightManager] Firebase still NOT ready after retry! Using default...");
            totalGameSeconds = (startHour * 3600f) + (startMinute * 60f);
            currentDay = 1;
            isGameTimeSet = true;
            
            // 🔧 Enable UI update với default time
            shouldUpdateUI = true;
            UpdateUIAndLight();
        }
    }

    // Callback để apply day/time từ Firebase
    private void ApplyDayTime(FirebaseDatabaseManager.DayTimeData dayTimeData)
    {
        if (dayTimeData == null)
        {
            Debug.LogWarning("[DayAndNightManager] DayTimeData is null!");
            return;
        }

        Debug.Log($"[DayAndNightManager] ⭐ ApplyDayTime: Day {dayTimeData.currentDay} {dayTimeData.currentHour:00}:{dayTimeData.currentMinute:00}");
        SetGameTime(dayTimeData.currentDay, dayTimeData.currentHour, dayTimeData.currentMinute);
        
        // 🔧 Enable UI update SAU KHI set time từ Firebase
        shouldUpdateUI = true;
        UpdateUIAndLight();
    }

    private void Update()
    {
        totalGameSeconds += Time.deltaTime * timeScale;
        savedTotalGameSeconds = totalGameSeconds;

        int calcDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        if (calcDay != currentDay)
        {
            currentDay = calcDay;
            Debug.Log("🌞 New day detected via Update()");
            OnNewDay();
        }

        UpdateUIAndLight();
    }

    private void OnNewDay()
    {
        Debug.Log($"🔥 [DayAndNightManager] Trigger New Day {currentDay}");
        DayAndNightEvents.InvokeNewDay(currentDay);

        // ✅ TẠO ORDER MỚI MỖI NGÀY
        if (OrderManager.Instance != null)
        {
            Debug.Log($"[DayAndNightManager] 📦 Gọi RefreshDailyOrders() cho ngày {currentDay}");
            OrderManager.Instance.RefreshDailyOrders();
        }
        else
        {
            Debug.LogWarning("[DayAndNightManager] ⚠️ OrderManager.Instance là null!");
        }
    }

    private void UpdateUIAndLight()
    {
        // 🔧 KHÔNG update UI nếu Firebase chưa ready
        if (!shouldUpdateUI)
        {
            Debug.Log("[DayAndNightManager] UpdateUIAndLight skipped - shouldUpdateUI=false");
            return;
        }

        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;

        int hours = Mathf.FloorToInt(secondsToday / 3600f);
        int minutes = Mathf.FloorToInt((secondsToday % 3600f) / 60f);

        if (textTimeInGame != null)
            textTimeInGame.text = $"{hours:00}:{minutes:00}";

        if (textDayInGame != null)
            textDayInGame.text = $"Ngày {currentDay}";

        if (globalLight != null)
        {
            float t = secondsToday / SECONDS_PER_DAY;
            globalLight.color = gradient.Evaluate(t);
        }
    }

    // ========================================================
    // SLEEP SYSTEM (20:00 → 06:00)
    // ========================================================
    public void SleepToNextDay()
    {
        int hour = GetCurrentHour();

        // ⭐ Được ngủ khi: (20h → 23h) hoặc (0h → 6h)
        bool canSleep = (hour >= 20) || (hour < 6);

        if (!canSleep)
        {
            Debug.Log($"⛔ Không thể ngủ bây giờ ({hour}:00). Chỉ ngủ 20:00 → 06:00.");
            return;
        }

        Debug.Log("😴 [Sleep] Bắt đầu chuyển sang ngày mới...");

        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        float morning = 7 * 3600f;

        if (secondsToday >= morning)
            totalGameSeconds += (SECONDS_PER_DAY - secondsToday) + morning;
        else
            totalGameSeconds += (morning - secondsToday);

        savedTotalGameSeconds = totalGameSeconds;

        currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        Debug.Log($"🌅 [Sleep] Sang ngày mới: Day {currentDay}");

        OnNewDay();
        UpdateUIAndLight();
    }

    public int GetCurrentDay() => currentDay;

    public int GetCurrentHour()
    {
        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        return Mathf.FloorToInt(secondsToday / 3600f);
    }

    public int GetCurrentMinute()
    {
        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        return Mathf.FloorToInt((secondsToday % 3600f) / 60f);
    }

    // ========================================================
    // SET GAME TIME (từ Firebase load)
    // ========================================================
    public void SetGameTime(int day, int hour, int minute)
    {
        if (day <= 0)
            day = 1;
        if (hour < 0 || hour > 23)
            hour = Mathf.Clamp(hour, 0, 23);
        if (minute < 0 || minute > 59)
            minute = Mathf.Clamp(minute, 0, 59);

        // Tính tổng giây từ day, hour, minute
        float secondsInDay = (hour * 3600f) + (minute * 60f);
        float dayInSeconds = (day - 1) * SECONDS_PER_DAY;
        
        totalGameSeconds = dayInSeconds + secondsInDay;
        savedTotalGameSeconds = totalGameSeconds;
        currentDay = day;
        isGameTimeSet = true;  // 🔧 Mark rằng đã set từ Firebase

        Debug.Log($"[DayAndNightManager] Set game time: Day {currentDay} {hour:00}:{minute:00} (totalGameSeconds: {totalGameSeconds})");
        
        // 🔧 KHÔNG gọi UpdateUIAndLight() ở đây - ApplyDayTime() sẽ gọi nó sau khi set shouldUpdateUI=true
    }
}
