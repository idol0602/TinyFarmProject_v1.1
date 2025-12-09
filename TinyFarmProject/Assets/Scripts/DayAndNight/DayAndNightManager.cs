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

        // === SINGLETON ===
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // === KHÔI PHỤC GAME TIME ===
        if (savedTotalGameSeconds >= 0f)
        {
            totalGameSeconds = savedTotalGameSeconds;
            currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        }
        else
        {
            totalGameSeconds = 7f * 3600f;
            currentDay = 1;
        }

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        StartCoroutine(DelayedUpdateLight());
    }

    private System.Collections.IEnumerator DelayedUpdateLight()
    {
        yield return null;

        FindUIReferences();

        UpdateUIAndLight();
    }

    private void FindUIReferences()
    {
        // tìm UI
        if (textTimeInGame == null)
            textTimeInGame = GameObject.Find("TimeText")?.GetComponent<TMP_Text>();

        if (textDayInGame == null)
            textDayInGame = GameObject.Find("DayText")?.GetComponent<TMP_Text>();

        // LUÔN tìm lại Global Light
        GameObject lightObj = GameObject.Find("Global Light 2D")
                           ?? GameObject.FindWithTag("GlobalLight");

        if (lightObj != null)
        {
            globalLight = lightObj.GetComponent<Light2D>();
        }
        else
        {
            Debug.LogWarning("[Light] ⚠ KHÔNG tìm thấy Global Light 2D trong scene mới!");
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
        savedTotalGameSeconds = totalGameSeconds;

        int newDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        if (newDay > currentDay)
        {
            currentDay = newDay;
            Debug.Log($"[DayNight] Sang ngày mới: Ngày {currentDay}");
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
            float t = (totalGameSeconds % SECONDS_PER_DAY) / SECONDS_PER_DAY;
            Color c = gradient.Evaluate(t);
            globalLight.color = c;

        }
        else
        {
            Debug.LogWarning("[Light] ⚠ Không có Global Light hoặc Gradient để cập nhật!");
        }
    }

    public void SleepToNextDay()
    {
        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;
        float target = 7f * 3600f;

        if (secondsToday >= target)
            totalGameSeconds += (SECONDS_PER_DAY - secondsToday) + target;
        else
            totalGameSeconds += (target - secondsToday);

        currentDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        Debug.Log($"[Sleep] Ngủ → Dậy lúc 7:00, Ngày {currentDay}");

        savedTotalGameSeconds = totalGameSeconds;

        UpdateUIAndLight();
    }

    private void OnNewDay()
    {
        DayAndNightEvents.InvokeNewDay(currentDay);
        Debug.Log($"[Sun] BẮT ĐẦU NGÀY MỚI → Day {currentDay}");
    }

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
