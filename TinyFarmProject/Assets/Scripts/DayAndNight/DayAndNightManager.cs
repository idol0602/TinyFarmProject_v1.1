using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;

public class DayAndNightManager : MonoBehaviour
{
    [Header("HIỂN THỊ GIỜ")]
    public TMP_Text textTimeInGame;

    [Header("THỜI GIAN 1 NGÀY TRONG GAME (PHÚT)")]
    public float realMinutesPerGameDay = 5f;  // ✅ 1 ngày = X phút (bạn chỉnh ở đây)

    [Header("ÁNH SÁNG")]
    public Light2D globalLight;
    public Gradient gradient;

    // =======================
    private float gameSecondsInDay = 86400f; // 24h * 60m * 60s
    private float timeScale;                 // Tốc độ chạy thời gian
    private float currentGameSeconds;        // Giây hiện tại trong ngày

    private int currentDay = 1;

    // =======================

    void Start()
    {
        // Tính scale thời gian: 1 ngày game = X phút real
        timeScale = gameSecondsInDay / (realMinutesPerGameDay * 60f);

        // Bắt đầu ngày tại 7:00 sáng
        SetTimeToMorning();
    }

    void Update()
    {
        // Chạy thời gian
        currentGameSeconds += Time.deltaTime * timeScale;

        // Hết 1 ngày → sang ngày mới
        if (currentGameSeconds >= gameSecondsInDay)
        {
            NextDay();
        }

        UpdateClockUI();
        UpdateLight();
    }

    // =======================
    // ✅ HIỂN THỊ GIỜ
    void UpdateClockUI()
    {
        int hours = Mathf.FloorToInt(currentGameSeconds / 3600);
        int minutes = Mathf.FloorToInt((currentGameSeconds % 3600) / 60);

        if (textTimeInGame != null)
            textTimeInGame.text = $"{hours:00}:{minutes:00}";
    }

    // =======================
    // ✅ ÁNH SÁNG THEO THỜI GIAN
    void UpdateLight()
    {
        if (globalLight != null)
            globalLight.color = gradient.Evaluate(currentGameSeconds / gameSecondsInDay);
    }

    // =======================
    // ✅ SANG NGÀY MỚI
    public void NextDay()
    {
        currentDay++;
        SetTimeToMorning();

        Debug.Log("🌞 Ngày mới: " + currentDay);
    }

    // =======================
    // ✅ SET 7:00 SÁNG
    public void SetTimeToMorning()
    {
        currentGameSeconds = 7 * 3600; // ✅ 7:00 sáng
    }

    // =======================
    // ✅ GỌI KHI NHÂN VẬT ĐI NGỦ
    public void SleepToNextDay()
    {
        currentDay++;
        SetTimeToMorning();

        Debug.Log("😴 Ngủ dậy sang ngày mới: " + currentDay);
    }
}
