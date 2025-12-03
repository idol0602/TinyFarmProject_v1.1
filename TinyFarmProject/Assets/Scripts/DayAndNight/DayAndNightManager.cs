using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)] // Chạy trước mọi script khác
public class DayAndNightManager : MonoBehaviour
{
    // ==================== SINGLETON - KHÔNG BAO GIỜ BỊ HỦY KHI CHUYỂN SCENE ====================
    public static DayAndNightManager Instance { get; private set; }

    [Header("=== HIỂN THỊ ===")]
    public TMP_Text textTimeInGame;
    public TMP_Text textDayInGame;

    [Header("=== TỐC ĐỘ THỜI GIAN ===")]
    [Tooltip("1 ngày trong game bằng bao nhiêu phút thực tế")]
    public float realMinutesPerGameDay = 5f;

    [Header("=== ÁNH SÁNG ===")]
    public Light2D globalLight;
    public Gradient gradient;

    // ================================= PRIVATE =================================
    private const float SECONDS_PER_DAY = 86400f;
    private float timeScale;

    // LƯU TRỮ THỜI GIAN TOÀN CỤC - KHÔNG BAO GIỜ RESET
    private static float savedTotalGameSeconds = -1f;  // -1 = chưa có save
    private float totalGameSeconds = 0f;

    private int currentDay = 1;

    // ========================================================================
    private void Awake()
    {
        // === SINGLETON PATTERN + DON'T DESTROY ON LOAD ===
        if (Instance != null && Instance != this)
        {
            // Nếu đã có 1 thằng khác rồi → destroy thằng mới (khi load scene lại)
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khôi phục thời gian từ lần chơi trước (nếu có)
        if (savedTotalGameSeconds >= 0f)
        {
            totalGameSeconds = savedTotalGameSeconds;
            currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;
        }
        else
        {
            // Lần đầu chơi → bắt đầu 7:00 sáng ngày 1
            totalGameSeconds = 7f * 3600f;
            currentDay = 1;
        }

        // Tính tốc độ thời gian
        timeScale = SECONDS_PER_DAY / (realMinutesPerGameDay * 60f);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Khi vào scene mới → tìm lại UI (vì UI ở mỗi scene khác nhau)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();
        UpdateUIAndLight();
    }

    // Tự động tìm lại Text và Light nếu bị mất khi chuyển scene
    private void FindUIReferences()
    {
        if (textTimeInGame == null)
            textTimeInGame = GameObject.Find("TimeText")?.GetComponent<TMP_Text>();

        if (textDayInGame == null)
            textDayInGame = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        if (globalLight == null)
        {
            var lightObj = GameObject.Find("Global Light 2D") ?? GameObject.FindWithTag("GlobalLight");
            globalLight = lightObj?.GetComponent<Light2D>();
        }
    }

    void Start()
    {
        FindUIReferences();
        UpdateUIAndLight();
    }

    void Update()
    {
        totalGameSeconds += Time.deltaTime * timeScale;
        savedTotalGameSeconds = totalGameSeconds; // Luôn cập nhật static để giữ khi reload

        // Tự động sang ngày mới khi qua 00:00
        int newDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;
        if (newDay > currentDay)
        {
            currentDay = newDay;
            OnNewDay();
        }

        UpdateUIAndLight();
    }

    void UpdateUIAndLight()
    {
        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        int hours = Mathf.FloorToInt(secondsToday / 3600f);
        int minutes = Mathf.FloorToInt((secondsToday % 3600f) / 60f);

        if (textTimeInGame != null)
            textTimeInGame.text = $"{hours:00}:{minutes:00}";

        if (textDayInGame != null)
            textDayInGame.text = $"Ngày {currentDay}";

        if (globalLight != null && gradient != null)
        {
            float t = secondsToday / SECONDS_PER_DAY;
            globalLight.color = gradient.Evaluate(t);
        }
    }

    // ==================== NGỦ → NHẢY TỚI 7:00 SÁNG HÔM SAU ====================
    public void SleepToNextDay()
    {
        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        float target = 7f * 3600f;

        if (secondsToday >= target)
            totalGameSeconds += (SECONDS_PER_DAY - secondsToday) + target;
        else
            totalGameSeconds += (target - secondsToday);

        currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;
        savedTotalGameSeconds = totalGameSeconds;

        Debug.Log($"[Sleep] Tỉnh dậy lúc 7:00 sáng - Ngày {currentDay}");
        UpdateUIAndLight();
    }

    private void OnNewDay()
    {
        DayAndNightEvents.InvokeNewDay(currentDay);

        Debug.Log($"[Sun] BẮT ĐẦU NGÀY MỚI: Ngày {currentDay}");
        // Có thể gọi event: crop grow, shop reset, v.v.
    }

    // ==================== CÁC HÀM PUBLIC CHO SCRIPT KHÁC DÙNG ====================
    public float GetCurrentGameSeconds() => totalGameSeconds % SECONDS_PER_DAY;
    public int GetCurrentHour() => Mathf.FloorToInt((totalGameSeconds % SECONDS_PER_DAY) / 3600f);
    public int GetCurrentDay() => currentDay;
    public string GetCurrentTimeString()
    {
        float s = totalGameSeconds % SECONDS_PER_DAY;
        return $"{Mathf.FloorToInt(s / 3600f):00}:{Mathf.FloorToInt((s % 3600f) / 60f):00}";
    }
    public float GetNormalizedTime01() => (totalGameSeconds % SECONDS_PER_DAY) / SECONDS_PER_DAY;
}