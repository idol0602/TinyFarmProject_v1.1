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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ==========================
        // LOAD / SETUP GAME TIME
        // ==========================
        if (savedTotalGameSeconds >= 0f)
        {
            totalGameSeconds = savedTotalGameSeconds;
            currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;
        }
        else
        {
            // ⭐ Giờ bắt đầu có thể chỉnh trong Inspector
            totalGameSeconds = (startHour * 3600f) + (startMinute * 60f);
            currentDay = 1;
        }

        timeScale = SECONDS_PER_DAY / (realMinutesPerGameDay * 60f);
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
        FindUIReferences();
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
}
