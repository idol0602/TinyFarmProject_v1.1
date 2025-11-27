using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;   // BẮT BUỘC cho TextMeshPro

public class DayAndNightManager : MonoBehaviour
{
    [Header("TextMeshPro hiển thị thời gian")]
    public TMP_Text textTimeInGame;   // Đổi từ Text -> TMP_Text

    // Thời gian 1 ngày trong game = 5 phút
    public float dayMultiplier = 100f;
    public Light2D light2D;
    public Gradient gradient;

    private void Update()
    {
        DateTime realTime = DateTime.Now;

        // Tổng số giây trong ngày thực
        float realSecondInDay =
            (realTime.Hour * 3600) +
            (realTime.Minute * 60) +
            realTime.Second;
        realSecondInDay = (realSecondInDay * dayMultiplier) % 86400;

        // Tính giờ trong game
        int gameHours =
            Mathf.FloorToInt(realSecondInDay / 3600);

        // Tính phút trong game
        int gameMinutes =
            Mathf.FloorToInt((realSecondInDay % 3600) / 60); 

        // Format thời gian 00:00
        string timeFormatted = string.Format("{0:00}:{1:00}", gameHours, gameMinutes);

        // Gán cho TextMeshPro
        if (textTimeInGame != null)
            textTimeInGame.text = timeFormatted;

        ChangeColorByTime(realSecondInDay);
    }

    public void ChangeColorByTime(float seconds)
    {
        light2D.color = gradient.Evaluate(seconds / 86400f);
    }
}
