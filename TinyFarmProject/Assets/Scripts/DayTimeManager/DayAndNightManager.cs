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
        UpdateUIAndLight();
    }

    private void FindUIReferences()
    {
        if (textTimeInGame == null)
            textTimeInGame = GameObject.Find("TimeText")?.GetComponent<TMP_Text>();

        if (textDayInGame == null)
            textDayInGame = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        GameObject lightObj = GameObject.Find("Global Light 2D") ?? GameObject.FindWithTag("GlobalLight");
        if (lightObj != null)
            globalLight = lightObj.GetComponent<Light2D>();
    }

    private void Start()
    {
        // ⭐ INITIALIZE TIME từ Firebase cache (trong Start để cho async callback kịp)
        if (!hasInitializedTime)
        {
            Debug.Log($"[DayAndNightManager] START: CachedDayTimeData={FirebaseDatabaseManager.CachedDayTimeData}, isGameTimeSet={isGameTimeSet}");
            
            if (FirebaseDatabaseManager.CachedDayTimeData != null && !isGameTimeSet)
            {
                Debug.Log($"[DayAndNightManager] ⭐ START: Loading cached day/time from Firebase!");
                var cached = FirebaseDatabaseManager.CachedDayTimeData;
                Debug.Log($"[DayAndNightManager] Cached data: Day {cached.currentDay} {cached.currentHour:00}:{cached.currentMinute:00}");
                SetGameTime(cached.currentDay, cached.currentHour, cached.currentMinute);
            }
            else if (!isGameTimeSet)
            {
                // Nếu cache chưa sẵn sàng, đợi một chút rồi check lại
                Debug.Log($"[DayAndNightManager] START: Cache not ready yet, waiting for Firebase...");
                StartCoroutine(WaitForCachedDayTime());
            }
            else
            {
                Debug.Log($"[DayAndNightManager] START: Time already set (isGameTimeSet=true), skipping");
            }
            
            hasInitializedTime = true;
        }

        FindUIReferences();
        UpdateUIAndLight();
    }

    // Đợi cache data từ Firebase
    private System.Collections.IEnumerator WaitForCachedDayTime()
    {
        float waitTime = 0f;
        while (FirebaseDatabaseManager.CachedDayTimeData == null && waitTime < 5f && !isGameTimeSet)
        {
            Debug.Log($"[DayAndNightManager] Waiting for cached day/time... ({waitTime:F1}s)");
            waitTime += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        if (FirebaseDatabaseManager.CachedDayTimeData != null && !isGameTimeSet)
        {
            Debug.Log($"[DayAndNightManager] ⭐ Finally got cached day/time!");
            var cached = FirebaseDatabaseManager.CachedDayTimeData;
            Debug.Log($"[DayAndNightManager] Cached data: Day {cached.currentDay} {cached.currentHour:00}:{cached.currentMinute:00}");
            SetGameTime(cached.currentDay, cached.currentHour, cached.currentMinute);
        }
        else if (!isGameTimeSet)
        {
            Debug.LogWarning($"[DayAndNightManager] Timeout waiting for cache, using default time {startHour:00}:{startMinute:00}");
            totalGameSeconds = (startHour * 3600f) + (startMinute * 60f);
            currentDay = 1;
            isGameTimeSet = true;
        }
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
    }

    private void UpdateUIAndLight()
    {
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
        UpdateUIAndLight();
    }
}
