using UnityEngine;
using UnityEngine.Events;

public class AIDecisionWeather : MonoBehaviour
{
    [Header("=== CHẾ ĐỘ ===")]
    [SerializeField] public bool isTestMode = false;  // Nếu true: dùng giá trị cố định; false: AI quyết định
    
    [Header("=== KHUNG GIỜ MƯA (Test Mode) ===")]
    [SerializeField] [Range(0, 23)] public int rainStartHour = 8;
    [SerializeField] [Range(0, 23)] public int rainEndHour = 17;
    
    [Header("=== XÁC SUẤT MƯA (Test Mode) ===")]
    [SerializeField] [Range(0, 100)] public float testRainChance = 100f;
    
    [Header("=== KHUNG GIỜ MƯA NGẪU NHIÊN (AI Mode) ===")]
    [SerializeField] [Range(0, 23)] public int aiRainStartHourMin = 6;
    [SerializeField] [Range(0, 23)] public int aiRainStartHourMax = 18;
    [SerializeField] [Range(0, 23)] public int aiRainEndHourMin = 9;
    [SerializeField] [Range(0, 23)] public int aiRainEndHourMax = 20;
    
    [Header("=== XÁC SUẤT MƯA NGẪU NHIÊN (AI Mode) ===")]
    [SerializeField] [Range(0, 100)] public float aiRainChanceMin = 100f;
    [SerializeField] [Range(0, 100)] public float aiRainChanceMax = 100f;
    
    [Header("=== EVENTS ===")]
    public UnityEvent<int> onRainStart = new UnityEvent<int>();  // Tham số: mức độ mưa (0-100)
    public UnityEvent onRainEnd = new UnityEvent();
    
    // State
    private int currentDay = -1;
    private bool willRainToday = false;
    private bool isRaining = false;
    private int actualRainStartHour = -1;
    private int actualRainEndHour = -1;
    
    private const float SECONDS_PER_DAY = 86400f;
    private const float SECONDS_PER_HOUR = 3600f;
    private const float SECONDS_PER_MINUTE = 60f;

    private void Start()
    {
        Debug.Log("[AIDecisionWeather] Start() - initialized weather system");
        
        // Subscribe vào sự kiện ngày mới
        DayAndNightEvents.OnNewDay += OnDayChanged;
        
        // Tạo thời tiết cho ngày đầu tiên
        if (DayAndNightManager.Instance != null)
        {
            int initialDay = DayAndNightManager.Instance.GetCurrentDay();
            GenerateWeatherForDay(initialDay);
        }
    }

    private void OnDestroy()
    {
        DayAndNightEvents.OnNewDay -= OnDayChanged;
    }

    private void Update()
    {
        if (!DayAndNightManager.Instance)
            return;

        // Nếu hôm nay sẽ có mưa, kiểm tra xem đến khung giờ mưa chưa
        if (willRainToday && !isRaining)
        {
            int currentHour = DayAndNightManager.Instance.GetCurrentHour();

            // ✅ KIỂM TRA GIỜ MƯA CÓ ĐẾN CHƯA
            if (currentHour >= actualRainStartHour && currentHour < actualRainEndHour)
            {
                TriggerRain();
            }
        }

        // Nếu đang mưa, kiểm tra xem hết khung giờ mưa chưa
        if (isRaining)
        {
            int currentHour = DayAndNightManager.Instance.GetCurrentHour();
            
            // Nếu vượt quá giờ kết thúc, kết thúc mưa
            if (currentHour >= actualRainEndHour)
            {
                EndRain();
            }
        }
    }

    /// <summary>
    /// Callback khi sang ngày mới
    /// </summary>
    private void OnDayChanged(int dayNumber)
    {
        Debug.Log($"[AIDecisionWeather] ☀️ Ngày mới: {dayNumber}");
        
        // Kết thúc mưa cũ (nếu có)
        if (isRaining)
            EndRain();
        
        // Tạo thời tiết mới
        GenerateWeatherForDay(dayNumber);
    }

    /// <summary>
    /// Tạo thời tiết ngẫu nhiên cho ngày
    /// </summary>
    private void GenerateWeatherForDay(int day)
    {
        currentDay = day;
        
        if (isTestMode)
        {
            // 🧪 TEST MODE: Dùng giá trị cố định từ Inspector
            float random = Random.Range(0f, 100f);
            willRainToday = random < testRainChance;
            actualRainStartHour = rainStartHour;
            actualRainEndHour = rainEndHour;
            
            if (willRainToday)
            {
                Debug.Log($"[AIDecisionWeather] 🧪 TEST MODE - Ngày {day}: Sẽ mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00 (Xác suất test: {testRainChance}%)");
            }
            else
            {
                Debug.Log($"[AIDecisionWeather] 🧪 TEST MODE - Ngày {day}: Không mưa");
            }
        }
        else
        {
            // 🤖 AI MODE: Random mọi thứ
            float rainChance = Random.Range(aiRainChanceMin, aiRainChanceMax);
            float random = Random.Range(0f, 100f);
            
            // ✅ LOGIC: Nếu xác suất < 60% → không mưa, không check random
            if (rainChance < 60f)
            {
                willRainToday = false;
                Debug.Log($"[AIDecisionWeather] 🤖 AI MODE - Ngày {day}: Không có mưa (Xác suất: {rainChance:F1}% < 60%)");
            }
            else
            {
                willRainToday = random < rainChance;
                
                if (willRainToday)
                {
                    // Random giờ bắt đầu và kết thúc mưa
                    actualRainStartHour = Random.Range(aiRainStartHourMin, aiRainStartHourMax + 1);
                    actualRainEndHour = Random.Range(aiRainEndHourMin, aiRainEndHourMax + 1);
                    
                    // ✅ ĐẢM BẢO: endHour > startHour và không vượt 23
                    if (actualRainEndHour <= actualRainStartHour)
                    {
                        // Nếu end <= start, set end = start + 2-5 giờ, nhưng max = 23
                        int addHours = Random.Range(2, 6);
                        actualRainEndHour = Mathf.Min(actualRainStartHour + addHours, 23);
                    }
                    
                    Debug.Log($"[AIDecisionWeather] 🤖 AI MODE - Ngày {day}: Sẽ mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00 (Xác suất: {rainChance:F1}%)");
                }
                else
                {
                    Debug.Log($"[AIDecisionWeather] 🤖 AI MODE - Ngày {day}: Không có mưa (Random {random:F1}% >= Xác suất {rainChance:F1}%)");
                }
            }
        }
    }

    /// <summary>
    /// Kích hoạt mưa
    /// </summary>
    private void TriggerRain()
    {
        if (isRaining)
            return;

        isRaining = true;
        
        // Tính mức độ mưa (0-100)
        int rainIntensity = Random.Range(30, 101);
        
        Debug.Log($"[AIDecisionWeather] 🌧️ MƯA BẮT ĐẦU! Mức độ: {rainIntensity}%");
        
        onRainStart?.Invoke(rainIntensity);
    }

    /// <summary>
    /// Kết thúc mưa
    /// </summary>
    private void EndRain()
    {
        if (!isRaining)
            return;

        isRaining = false;
        
        Debug.Log($"[AIDecisionWeather] ⛅ MƯA KẾT THÚC!");
        
        onRainEnd?.Invoke();
    }

    /// <summary>
    /// Lấy thông tin thời tiết hiện tại (để debug)
    /// </summary>
    public void PrintWeatherDebug()
    {
        string modeLabel = isTestMode ? "🧪 TEST MODE" : "🤖 AI MODE";
        string rainStatus = willRainToday 
            ? $"Sẽ mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00" 
            : "Không có mưa";
        
        string rainingStatus = isRaining 
            ? "Đang mưa" 
            : "Không mưa";
        
        Debug.Log($"[AIDecisionWeather] 📊 {modeLabel} - Ngày {currentDay}: {rainStatus} | {rainingStatus}");
    }

#if UNITY_EDITOR
    [ContextMenu("TEST: Trigger Rain Now")]
    private void TestTriggerRainNow()
    {
        TriggerRain();
        Debug.Log("[AIDecisionWeather] 🧪 TEST: Kích hoạt mưa ngay lập tức!");
    }

    [ContextMenu("TEST: End Rain Now")]
    private void TestEndRainNow()
    {
        EndRain();
        Debug.Log("[AIDecisionWeather] 🧪 TEST: Kết thúc mưa ngay lập tức!");
    }

    [ContextMenu("TEST: Print Weather Info")]
    private void TestPrintWeatherInfo()
    {
        PrintWeatherDebug();
    }

    [ContextMenu("TEST: Next Day")]
    private void TestNextDay()
    {
        int nextDay = (DayAndNightManager.Instance?.GetCurrentDay() ?? 1) + 1;
        DayAndNightEvents.InvokeNewDay(nextDay);
        Debug.Log($"[AIDecisionWeather] 🧪 TEST: Sang ngày {nextDay}");
    }
#endif
}
