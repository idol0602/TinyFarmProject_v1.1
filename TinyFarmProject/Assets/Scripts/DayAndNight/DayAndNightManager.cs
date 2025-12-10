using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class DayAndNightManager : MonoBehaviour
{
    public static DayAndNightManager Instance { get; private set; }

    public static int LastNewDayEvent = -1;  // ⭐ CHO CROP BIẾT NGÀY MỚI

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
            totalGameSeconds = 7f * 3600f;
            currentDay = 1;
        }

        timeScale = SECONDS_PER_DAY / (realMinutesPerGameDay * 60f);
    }

    void Update()
    {
        totalGameSeconds += Time.deltaTime * timeScale;
        savedTotalGameSeconds = totalGameSeconds;

        int newDay = Mathf.FloorToInt(totalGameSeconds / SECONDS_PER_DAY) + 1;

        if (newDay > currentDay)
        {
            currentDay = newDay;

            Debug.Log($"[UPDATE NEW DAY] Ngày mới = {currentDay}");

            LastNewDayEvent = currentDay;           // ⭐ LƯU NGÀY MỚI
            DayAndNightEvents.InvokeNewDay(currentDay); // ⭐ BẮN SỰ KIỆN

            Debug.Log($"[SUN] BẮT ĐẦU NGÀY MỚI → Day {currentDay}");
        }

        UpdateUIAndLight();
    }

    void UpdateUIAndLight()
    {
        float secondsToday = totalGameSeconds % SECONDS_PER_DAY;

        int hours = Mathf.FloorToInt(secondsToday / 3600f);
        int minutes = Mathf.FloorToInt((secondsToday % 3600f) / 60f);

        if (textTimeInGame != null) textTimeInGame.text = $"{hours:00}:{minutes:00}";
        if (textDayInGame != null) textDayInGame.text = $"Ngày {currentDay}";

        if (globalLight != null)
        {
            float t = (totalGameSeconds % SECONDS_PER_DAY) / SECONDS_PER_DAY;
            globalLight.color = gradient.Evaluate(t);
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

        savedTotalGameSeconds = totalGameSeconds;

        Debug.Log($"[Sleep] After Sleep: totalGameSeconds={totalGameSeconds}");

        // ❌ Không cập nhật currentDay ở đây
        // ❌ Không bắn event ở đây

        UpdateUIAndLight();
    }

    public int GetCurrentDay() => currentDay;
    public int GetCurrentHour() => Mathf.FloorToInt((totalGameSeconds % SECONDS_PER_DAY) / 3600f);
}
