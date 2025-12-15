using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class AIDecisionWeather : MonoBehaviour
{
    [Header("=== CHẾ ĐỘ ===")]
    [SerializeField] public bool isTestMode = false;  // Nếu true: dùng giá trị cố định; false: AI quyết định
    
    [Header("=== KHUNG GIỜ MƯA (Test Mode) ===")]
    [SerializeField] [Range(0, 23)] public int rainStartHour = 8;
    [SerializeField] [Range(0, 23)] public int rainEndHour = 17;
    
    [Header("=== XÁC SUẤT MƯA (Test Mode) ===")]
    [SerializeField] [Range(0, 100)] public float testRainChance = 100f;
    
    [Header("=== XÁC SUẤT MƯA (AI Mode) ===")]
    [SerializeField] [Range(0, 100)] public float aiRainProbability = 40f;  // 40% xác suất có mưa
    
    [Header("=== Gemini API (AI Mode) ===")]
    [SerializeField] private string geminiApiKey = "AIzaSyBuhuCuBWQ90BA6AM0c6XIuu0a99wSdqPw";
    
    [Header("=== EVENTS ===")]
    public UnityEvent<int> onRainStart = new UnityEvent<int>();  // Tham số: mức độ mưa (0-100)
    public UnityEvent onRainEnd = new UnityEvent();
    
    // State
    private int currentDay = -1;
    private bool willRainToday = false;
    private bool isRaining = false;
    private bool rainTriggeredToday = false;  // ⭐ Cờ để ngăn chặn 2 sự kiện mưa trong 1 ngày
    private int actualRainStartHour = -1;
    private int actualRainEndHour = -1;
    private bool weatherGeneratedForDay = false;  // ⭐ Cờ để tránh gọi AI multiple lần trong 1 ngày
    
    private const float SECONDS_PER_DAY = 86400f;
    private const float SECONDS_PER_HOUR = 3600f;
    private const float SECONDS_PER_MINUTE = 60f;
    private const string PREFS_WEATHER_DAY = "AIWeather_LastDay";  // ⭐ Lưu ngày cuối cùng gọi AI
    private const string PREFS_WEATHER_STATE = "AIWeather_WillRain";  // ⭐ Lưu trạng thái mưa
    private const string PREFS_RAIN_START_HOUR = "AIWeather_RainStartHour";  // ⭐ Lưu giờ bắt đầu mưa
    private const string PREFS_RAIN_END_HOUR = "AIWeather_RainEndHour";  // ⭐ Lưu giờ kết thúc mưa
    private const string PREFS_WEATHER_GENERATED = "AIWeather_Generated";  // ⭐ Lưu trạng thái: đã gọi API cho ngày này chưa

    private void Start()
    {
        Debug.Log("[AIDecisionWeather] Start() - initialized weather system");
        
        // Subscribe vào sự kiện ngày mới
        DayAndNightEvents.OnNewDay += OnDayChanged;
        
        // Tạo thời tiết cho ngày đầu tiên
        if (DayAndNightManager.Instance != null)
        {
            int initialDay = DayAndNightManager.Instance.GetCurrentDay();
            
            // ⭐ Kiểm tra xem ngày này đã gọi AI chưa (từ lần load trước)
            int lastDay = PlayerPrefs.GetInt(PREFS_WEATHER_DAY, -1);
            if (lastDay == initialDay)
            {
                // Đã gọi AI cho ngày này rồi, restore state
                weatherGeneratedForDay = PlayerPrefs.GetInt(PREFS_WEATHER_GENERATED, 0) == 1;  // ⭐ Restore flag
                willRainToday = PlayerPrefs.GetInt(PREFS_WEATHER_STATE, 0) == 1;
                actualRainStartHour = PlayerPrefs.GetInt(PREFS_RAIN_START_HOUR, -1);
                actualRainEndHour = PlayerPrefs.GetInt(PREFS_RAIN_END_HOUR, -1);
                
                // Kiểm tra xem hiện tại có trong khoảng mưa không
                int currentHour = DayAndNightManager.Instance.GetCurrentHour();
                bool inRainWindow = willRainToday && actualRainStartHour >= 0 && actualRainEndHour >= 0 && 
                                   currentHour >= actualRainStartHour && currentHour < actualRainEndHour;
                
                if (inRainWindow)
                {
                    Debug.Log($"[AIDecisionWeather] 📚 Restored weather state từ PlayerPrefs cho ngày {initialDay}: Có mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00. Hiện tại {currentHour:00}:00 - TRONG KHOẢNG MƯA, trigger mưa");
                    TriggerRain();
                }
                else if (willRainToday)
                {
                    // Ngoài khoảng mưa
                    bool afterRainWindow = actualRainStartHour >= 0 && actualRainEndHour >= 0 && currentHour >= actualRainEndHour;
                    if (afterRainWindow)
                    {
                        // Giờ hiện tại đã sau khoảng mưa, dừng mưa
                        Debug.Log($"[AIDecisionWeather] 📚 Restored weather state từ PlayerPrefs cho ngày {initialDay}: Có mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00. Hiện tại {currentHour:00}:00 - NGOÀI KHOẢNG MƯA (ĐÃ QUA), dừng mưa");
                        EndRain();
                    }
                    else
                    {
                        Debug.Log($"[AIDecisionWeather] 📚 Restored weather state từ PlayerPrefs cho ngày {initialDay}: Có mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00. Hiện tại {currentHour:00}:00 - NGOÀI KHOẢNG MƯA (CHƯA ĐẾN), không trigger");
                    }
                }
                else
                {
                    Debug.Log($"[AIDecisionWeather] 📚 Restored weather state từ PlayerPrefs cho ngày {initialDay}: Không mưa");
                }
            }
            else
            {
                // Ngày mới, tạo thời tiết mới
                weatherGeneratedForDay = false;
                GenerateWeatherForDay(initialDay);
            }
        }
    }

    private void OnDestroy()
    {
        DayAndNightEvents.OnNewDay -= OnDayChanged;
    }

    private void Update()
    {
        int currentHour = DayAndNightManager.Instance.GetCurrentHour();
        if (!DayAndNightManager.Instance)
            return;

        // ⭐ PRIORITY 1: Kiểm tra xem hết khung giờ mưa chưa - PHẢI LÀM TRƯỚC
        if (isRaining && currentHour >= actualRainEndHour)
        {
            EndRain();
            return; // ✅ Dừng kiểm tra khi đã ngừng mưa
        }

        // ⭐ PRIORITY 2: Nếu hôm nay sẽ có mưa, kiểm tra xem đến khung giờ mưa chưa
        if (willRainToday && !isRaining && !rainTriggeredToday)  // ✅ Ngăn 2 sự kiện mưa
        {
            // ✅ KIỂM TRA GIỜ MƯA CÓ ĐẾN CHƯA
            if (currentHour >= actualRainStartHour && currentHour < actualRainEndHour)
            {
                TriggerRain();
                rainTriggeredToday = true;  // ⭐ Đánh dấu đã trigger mưa trong ngày
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
        
        // ⭐ RESET FLAGS cho ngày mới
        rainTriggeredToday = false;  // Reset cờ mưa
        weatherGeneratedForDay = false;  // Reset cờ gọi AI
        
        // Tạo thời tiết mới
        GenerateWeatherForDay(dayNumber);
    }

    /// <summary>
    /// Tạo thời tiết cho ngày
    /// </summary>
    private void GenerateWeatherForDay(int day)
    {
        currentDay = day;
        
        if (isTestMode)
        {
            // 🧪 TEST MODE: Dùng giá trị cố định từ Inspector
            willRainToday = Random.Range(0f, 100f) < testRainChance;
            actualRainStartHour = rainStartHour;
            actualRainEndHour = rainEndHour;
            weatherGeneratedForDay = true;
            
            if (willRainToday)
            {
                Debug.Log($"[AIDecisionWeather] 🧪 TEST MODE - Ngày {day}: Sẽ mưa từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00 (Xác suất test: {testRainChance}%)");
            }
            else
            {
                Debug.Log($"[AIDecisionWeather] 🧪 TEST MODE - Ngày {day}: Không mưa");
            }
        }
        else if (!weatherGeneratedForDay)
        {
            // 🤖 AI MODE: Gọi Gemini để nó trả về 1 xác suất ngẫu nhiên
            Debug.Log($"[AIDecisionWeather] 🤖 AI MODE - Gọi Gemini để lấy xác suất mưa ngẫu nhiên cho ngày {day}...");
            weatherGeneratedForDay = true;  // ⭐ Đánh dấu đã gọi AI
            StartCoroutine(CallGeminiForWeather(day));
        }
        else
        {
            Debug.Log($"[AIDecisionWeather] ℹ️ Thời tiết ngày {day} đã được tạo, không gọi AI lại");
        }
    }

    /// <summary>
    /// Gọi Gemini API 1 lần để lấy cả xác suất AND giờ mưa
    /// Trả về format: "PROBABILITY|RAIN_TIME" hoặc "PROBABILITY|NONE"
    /// </summary>
    private IEnumerator CallGeminiForWeather(int day)
    {
        string prompt = $@"Bạn là AI tạo thời tiết ngẫu nhiên trong game nông trại.

BƯỚC 1: Tạo 1 số ngẫu nhiên từ 0 đến 100 (không có thập phân). Đây là xác suất mưa.
BƯỚC 2: 
- Nếu xác suất <= {aiRainProbability}: Chọn 1 khung giờ mưa hợp lý ví dụ (08:00-12:00, 11:00-15:00, 14:00-18:00, hoặc 19:00-23:00) bạn có thể tạo khác các khung giờ đã gợi ý. nếu mưa luôn luôn phải tạo khung giờ mưa. Khi quyết định mưa thì luôn luôn cung cấp giờ mưa chú ý điều này.
- Nếu xác suất > {aiRainProbability}: Không mưa

TRUYỀN VỀ 2 DÒNG:
[Dòng 1] SỐ XÁC SUẤT (0-100)
[Dòng 2] RAIN|HH:MM-HH:MM nếu có mưa, hoặc NONE nếu không mưa

KHÔNG THÊM DỮ LIỆU KHÁC!

Ví dụ:
75
RAIN|10:00-14:00

Hoặc:
30
NONE";

        string jsonBody = $@"{{
  ""contents"": [{{
    ""role"": ""user"",
    ""parts"": [{{
      ""text"": ""{EscapeJson(prompt)}""
    }}]
  }}],
  ""generationConfig"": {{
    ""temperature"": 0.7,
    ""maxOutputTokens"": 150
  }}
}}";

        string apiUrl = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash-lite:generateContent";

        using (UnityWebRequest www = new UnityWebRequest(apiUrl + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string fullResponse = www.downloadHandler.text;
                Debug.Log($"[AIDecisionWeather] 📝 Raw response từ Gemini: {fullResponse}");
                
                string aiResponse = ExtractGeminiText(fullResponse).Trim();
                Debug.Log($"[AIDecisionWeather] 🔍 Extracted text: '{aiResponse}'");
                
                ParseWeatherDecision(aiResponse, day);
            }
            else
            {
                Debug.LogWarning($"[AIDecisionWeather] ⚠️ Gemini lỗi ({www.responseCode}), dùng mặc định");
                Debug.LogWarning($"[AIDecisionWeather] 💡 Kiểm tra API key trong Inspector hoặc console");
                willRainToday = false;
                
                // Log full response để debug
                Debug.LogWarning($"[AIDecisionWeather] Response: {www.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// Parse response từ Gemini: 2 dòng (Probability + RainTime)
    /// Line 1: Xác suất 0-100
    /// Line 2: RAIN|HH:MM-HH:MM hoặc NONE
    /// </summary>
    private void ParseWeatherDecision(string response, int day)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: Response rỗng, AI không trả lời. Dùng xác suất mặc định");
            willRainToday = aiRainProbability >= 50;
            Debug.Log($"[AIDecisionWeather] Ngày {day}: Fallback - {aiRainProbability}% >= 50% = {(willRainToday ? "✅ MƯA" : "❌ KHÔNG MƯA")}");
            SaveWeatherState(day);
            return;
        }

        // Tách 2 dòng
        string[] lines = response.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: Response không đủ 2 dòng, chỉ có {lines.Length} dòng. Dùng fallback");
            willRainToday = aiRainProbability >= 50;
            Debug.Log($"[AIDecisionWeather] Ngày {day}: Fallback - {aiRainProbability}% >= 50% = {(willRainToday ? "✅ MƯA" : "❌ KHÔNG MƯA")}");
            SaveWeatherState(day);
            return;
        }

        // Parse dòng 1: Xác suất
        string probabilityLine = lines[0].Trim();
        if (!int.TryParse(probabilityLine, out int aiProbability))
        {
            Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: Dòng 1 parse lỗi: '{probabilityLine}' không phải số. Dùng fallback");
            willRainToday = aiRainProbability >= 50;
            Debug.Log($"[AIDecisionWeather] Ngày {day}: Fallback - {aiRainProbability}% >= 50% = {(willRainToday ? "✅ MƯA" : "❌ KHÔNG MƯA")}");
            SaveWeatherState(day);
            return;
        }

        aiProbability = Mathf.Clamp(aiProbability, 0, 100);

        // Parse dòng 2: Giờ mưa hoặc NONE
        string rainTimeLine = lines[1].Trim();
        
        // AI quyết định: nếu xác suất <= ngưỡng aiRainProbability → sẽ mưa (phải có RAIN|HH:MM-HH:MM)
        // Nếu xác suất > ngưỡng → không mưa (NONE)
        bool aiDecidesToRain = aiProbability <= aiRainProbability;
        willRainToday = false;

        if (aiDecidesToRain)
        {
            // AI quyết định sẽ mưa → phải cung cấp giờ mưa hợp lệ
            if (rainTimeLine.StartsWith("RAIN|"))
            {
                string timeRange = rainTimeLine.Substring(5); // Bỏ "RAIN|"
                
                if (timeRange.Contains("-"))
                {
                    string[] times = timeRange.Split('-');
                    if (times.Length == 2 && 
                        int.TryParse(times[0].Split(':')[0], out int startHour) &&
                        int.TryParse(times[1].Split(':')[0], out int endHour))
                    {
                        actualRainStartHour = Mathf.Clamp(startHour, 0, 23);
                        actualRainEndHour = Mathf.Clamp(endHour, 0, 23);
                        willRainToday = true;
                        Debug.Log($"[AIDecisionWeather] Ngày {day}: ✅ MƯA CHẮC CHẮN (AI xác suất: {aiProbability}%) từ {actualRainStartHour:00}:00 đến {actualRainEndHour:00}:00");
                    }
                    else
                    {
                        Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: Parse giờ mưa lỗi: '{rainTimeLine}' → KHÔNG MƯA");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: Format giờ mưa lỗi (thiếu '-'): '{rainTimeLine}' → KHÔNG MƯA");
                }
            }
            else
            {
                Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: AI quyết định mưa nhưng không cung cấp giờ ('{rainTimeLine}') → KHÔNG MƯA");
            }
        }
        else
        {
            // AI quyết định không mưa
            if (rainTimeLine == "NONE")
            {
                Debug.Log($"[AIDecisionWeather] Ngày {day}: ❌ KHÔNG MƯA (AI xác suất: {aiProbability}% > {aiRainProbability}%)");
            }
            else
            {
                Debug.LogWarning($"[AIDecisionWeather] ⚠️ Ngày {day}: AI quyết định không mưa nhưng nói '{rainTimeLine}' → KHÔNG MƯA");
            }
        }

        SaveWeatherState(day);
    }


    /// <summary>
    /// Lưu trạng thái thời tiết vào PlayerPrefs để persist giữa scene loads
    /// </summary>
    private void SaveWeatherState(int day)
    {
        PlayerPrefs.SetInt(PREFS_WEATHER_DAY, day);
        PlayerPrefs.SetInt(PREFS_WEATHER_STATE, willRainToday ? 1 : 0);
        PlayerPrefs.SetInt(PREFS_RAIN_START_HOUR, actualRainStartHour >= 0 ? actualRainStartHour : -1);
        PlayerPrefs.SetInt(PREFS_RAIN_END_HOUR, actualRainEndHour >= 0 ? actualRainEndHour : -1);
        PlayerPrefs.SetInt(PREFS_WEATHER_GENERATED, weatherGeneratedForDay ? 1 : 0);  // ⭐ Lưu flag
        PlayerPrefs.Save();
        Debug.Log($"[AIDecisionWeather] 💾 Lưu trạng thái thời tiết vào PlayerPrefs cho ngày {day}: {(willRainToday ? $"Mưa {actualRainStartHour:00}:00-{actualRainEndHour:00}:00" : "Không mưa")}");
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
    }

    private string ExtractGeminiText(string jsonResponse)
    {
        try
        {
            // ⭐ Debug: Log full JSON để thấy format
            Debug.Log($"[ExtractGeminiText] Full JSON length: {jsonResponse.Length}");
            Debug.Log($"[ExtractGeminiText] Full JSON: {jsonResponse}");
            
            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogWarning("[ExtractGeminiText] Response là null/empty");
                return "";
            }
            
            // Parse JSON để tìm text content
            // Format: {"candidates":[{"content":{"parts":[{"text":"50"}]}}]}
            
            // Tìm "text": trong JSON
            int textIndex = jsonResponse.IndexOf("\"text\":");
            if (textIndex == -1)
            {
                Debug.LogWarning("[ExtractGeminiText] ❌ Không tìm thấy \"text\": trong JSON");
                return "";
            }
            
            // Bắt đầu từ vị trí "text":
            int valueStart = textIndex + 7; // Bỏ qua "text":
            
            // Bỏ qua whitespace
            while (valueStart < jsonResponse.Length && (jsonResponse[valueStart] == ' ' || jsonResponse[valueStart] == '\n' || jsonResponse[valueStart] == '\r'))
                valueStart++;
            
            // Tìm dấu quote mở
            if (valueStart >= jsonResponse.Length || jsonResponse[valueStart] != '"')
            {
                Debug.LogWarning("[ExtractGeminiText] ❌ Không tìm thấy dấu quote sau \"text\":");
                return "";
            }
            
            // Tìm dấu quote đóng
            int valueEnd = jsonResponse.IndexOf("\"", valueStart + 1);
            if (valueEnd == -1)
            {
                Debug.LogWarning("[ExtractGeminiText] ❌ Không tìm thấy dấu quote đóng");
                return "";
            }
            
            // Extract giá trị
            string result = jsonResponse.Substring(valueStart + 1, valueEnd - valueStart - 1)
                                      .Replace("\\n", "\n")
                                      .Replace("\\\"", "\"")
                                      .Trim();
            
            Debug.Log($"[ExtractGeminiText] ✅ Extracted: '{result}'");
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ExtractGeminiText] ❌ Exception: {ex.Message}\n{ex.StackTrace}");
            return "";
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
        
        // ⭐ Gọi RainManager để quản lý trạng thái mưa
        if (RainManager.Instance != null)
        {
            Debug.Log($"[AIDecisionWeather] 🌧️ MƯA BẮT ĐẦU! Gọi RainManager.SetRain(true)");
            RainManager.Instance.SetRain(true);
        }
        else
        {
            Debug.LogWarning("[AIDecisionWeather] ⚠️ RainManager.Instance không tìm thấy!");
        }
    }

    /// <summary>
    /// Kết thúc mưa
    /// </summary>
    private void EndRain()
    {
        if (!isRaining)
            return;

        isRaining = false;
        
        // Gọi RainManager để quản lý trạng thái mưa
        if (RainManager.Instance != null)
        {
            RainManager.Instance.SetRain(false);
            Debug.Log($"[AIDecisionWeather] ⛅ MƯA KẾT THÚC!");
        }
        else
        {
            Debug.LogWarning("[AIDecisionWeather] ⚠️ RainManager.Instance không tìm thấy!");
        }
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
