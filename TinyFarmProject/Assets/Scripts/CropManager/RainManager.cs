using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class RainManager : MonoBehaviour
{
    public static RainManager Instance;

    // ⭐ STATIC CACHE để giữ rain state giữa scene
    public static bool CachedRainState = false;

    // ⭐ EVENT BÁO CHO CÂY
    public static event Action<bool> OnRainChanged;

    [Header("Rain State")]
    [SerializeField] private bool _isRaining = false;
    public bool isRaining => _isRaining;

    [Header("Particle")]
    public ParticleSystem rainParticle;

    private AIDecisionWeather weatherSystem;

    private void Awake()
    {
        // ===== SINGLETON =====
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ⭐ RESTORE RAIN STATE TỪ CACHE KHI START
        SetRain(CachedRainState, true);
    }

    private void Start()
    {
        // ⭐ SUBSCRIBE VÀO SCENE LOAD ĐỂ KHÔI PHỤC PARTICLE
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // ⭐ SUBSCRIBE VÀO WEATHER SYSTEM
        weatherSystem = FindObjectOfType<AIDecisionWeather>();
        if (weatherSystem != null)
        {
            weatherSystem.onRainStart.AddListener(OnWeatherRainStart);
            weatherSystem.onRainEnd.AddListener(OnWeatherRainEnd);
            Debug.Log("[RainManager] ✅ Subscribed to AIDecisionWeather events");
        }
        else
        {
            Debug.LogWarning("[RainManager] ⚠️ AIDecisionWeather not found in scene!");
        }
    }

    private void OnDestroy()
    {
        // ⭐ UNSUBSCRIBE KHI DESTROY
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (weatherSystem != null)
        {
            weatherSystem.onRainStart.RemoveListener(OnWeatherRainStart);
            weatherSystem.onRainEnd.RemoveListener(OnWeatherRainEnd);
        }
    }

    /// <summary>
    /// Callback khi scene load (để tìm particle mới)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[RainManager] Scene '{scene.name}' loaded - khôi phục trạng thái mưa...");
        
        // Tìm particle mới trong scene
        if (rainParticle == null)
        {
            rainParticle = FindObjectOfType<ParticleSystem>();
            if (rainParticle != null)
                Debug.Log("[RainManager] ✅ Tìm thấy ParticleSystem mới");
        }
        
        // Khôi phục trạng thái mưa từ cache (không gọi event)
        if (_isRaining && rainParticle != null)
        {
            rainParticle.Play();
            Debug.Log("[RainManager] ✅ Khôi phục animation mưa từ cache");
        }
        else if (!_isRaining && rainParticle != null)
        {
            rainParticle.Stop();
            Debug.Log("[RainManager] ✅ Dừng animation mưa");
        }
    }

    private void Update()
    {
        // ⭐ TEST NHANH BẰNG PHÍM R
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRain();
        }
    }

    // =====================================================
    //  CALLBACKS TỪ WEATHER SYSTEM
    // =====================================================

    /// <summary>
    /// Gọi khi trời bắt đầu mưa (từ AIDecisionWeather)
    /// </summary>
    private void OnWeatherRainStart(int rainIntensity)
    {
        Debug.Log($"[RainManager] 🌧️ Thời tiết bắt đầu mưa! Mức độ: {rainIntensity}%");
        SetRain(true);
    }

    /// <summary>
    /// Gọi khi trời hết mưa (từ AIDecisionWeather)
    /// </summary>
    private void OnWeatherRainEnd()
    {
        Debug.Log("[RainManager] ☀️ Thời tiết hết mưa!");
        SetRain(false);
    }

    // =====================================================
    //  API
    // =====================================================

    public void ToggleRain()
    {
        SetRain(!_isRaining);
    }

    public void SetRain(bool value, bool silent = false)
    {
        if (_isRaining == value) return;

        _isRaining = value;
        
        // ⭐ CACHE RAIN STATE
        CachedRainState = value;

        // Particle
        if (rainParticle != null)
        {
            if (_isRaining)
                rainParticle.Play();
            else
                rainParticle.Stop();
        }

        // Event
        if (!silent)
        {
            OnRainChanged?.Invoke(_isRaining);
        }

        Debug.Log(_isRaining ? "🌧️ TRỜI ĐANG MƯA" : "☀️ TRỜI HẾT MƯA");
    }
}
