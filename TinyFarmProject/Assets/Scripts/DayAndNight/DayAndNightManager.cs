using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class DayAndNightManager : MonoBehaviour
{
    public static DayAndNightManager Instance { get; private set; }

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

    private static float savedTotalGameSeconds = -1f;
    private float totalGameSeconds = 0f;

    private int currentDay = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (savedTotalGameSeconds >= 0f)
        {
            totalGameSeconds = savedTotalGameSeconds;
            currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;
        }
        else
        {
            totalGameSeconds = 7 * 3600f; // 7:00 sáng
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

    // ================= UI FINDER =====================

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

    // ================= GAME LOOP =====================

    private void Update()
    {
        totalGameSeconds += Time.deltaTime * timeScale;
        savedTotalGameSeconds = totalGameSeconds;

        int calcDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        // ⭐⭐ FIX LỖI QUAN TRỌNG ⭐⭐
        if (calcDay != currentDay)
        {
            currentDay = calcDay;
            Debug.Log("🌞 New day detected via Update()");
            OnNewDay();
        }

        UpdateUIAndLight();
    }

    // ================= EVENT CALLER =====================

    private void OnNewDay()
    {
        Debug.Log($"🔥 [DayAndNightManager] Trigger New Day {currentDay}");
        DayAndNightEvents.InvokeNewDay(currentDay);
    }

    // ================= UI / LIGHT =====================

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

    // ================= SLEEP SYSTEM =====================

    public void SleepToNextDay()
    {
        int hour = GetCurrentHour();

        // ⭐ Chỉ được ngủ từ 18:00 trở lên
        if (hour < 20)
        {
            Debug.Log("🌙 Không thể ngủ lúc này! Còn quá sớm (phải sau 18:00).");
            // Nếu muốn hiện UI thông báo:
            // UIManager.ShowMessage("Còn quá sớm để ngủ!");
            return;
        }

        Debug.Log("😴 [Sleep] Gọi SleepToNextDay()");

        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        float morning = 7 * 3600f;

        if (secondsToday >= morning)
            totalGameSeconds += (SECONDS_PER_DAY - secondsToday) + morning;
        else
            totalGameSeconds += (morning - secondsToday);

        savedTotalGameSeconds = totalGameSeconds;

        int newDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        currentDay = newDay;

        Debug.Log($"🌅 [Sleep] Sang ngày mới = {currentDay}");

        OnNewDay();  // Bắn event tăng trưởng cây

        UpdateUIAndLight();
    }


    // ================= GETTERS =====================

    public int GetCurrentDay() => currentDay;
    public int GetCurrentHour() => Mathf.FloorToInt((totalGameSeconds % SECONDS_PER_DAY) / 3600f);
}
