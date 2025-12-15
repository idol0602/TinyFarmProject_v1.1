using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class RainManager : MonoBehaviour
{
    public static RainManager Instance;

    // ⭐ CACHE RAIN STATE
    public static bool CachedRainState = false;

    // ⭐ EVENT
    public static event Action<bool> OnRainChanged;

    [Header("Rain State")]
    [SerializeField] private bool _isRaining = false;
    public bool isRaining => _isRaining;

    [Header("Particle")]
    public ParticleSystem rainParticle;

    private AIDecisionWeather weatherSystem;

    // =====================================================
    // UNITY
    // =====================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _isRaining = CachedRainState;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        weatherSystem = FindObjectOfType<AIDecisionWeather>();
        if (weatherSystem != null)
        {
            weatherSystem.onRainStart.AddListener(OnWeatherRainStart);
            weatherSystem.onRainEnd.AddListener(OnWeatherRainEnd);
        }

        ApplyRainState();
    }
    private void Update()
    {
        // Nhấn R để bật / tắt mưa thủ công
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRain();
            Debug.Log("[RainManager] ⌨ Nhấn R → Toggle Rain");
        }
    }


    private void OnDestroy()
    {
        if (weatherSystem != null)
        {
            weatherSystem.onRainStart.RemoveListener(OnWeatherRainStart);
            weatherSystem.onRainEnd.RemoveListener(OnWeatherRainEnd);
        }
    }

    // =====================================================
    // SCENE
    // =====================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[RainManager] Scene loaded: {scene.name}");
        ApplyRainState();
    }

    private void ApplyRainState()
    {
        // 🔍 Tìm spawn point trong scene
        RainSpawnPoint spawn = FindObjectOfType<RainSpawnPoint>();

        // ❌ Map KHÔNG có RainSpawnPoint → ẨN MƯA
        if (spawn == null)
        {
            if (rainParticle != null)
            {
                rainParticle.Stop();
                rainParticle.gameObject.SetActive(false);
            }

            Debug.Log("[RainManager] ⛔ Map này không có mưa");
            return;
        }

        // ✅ Map có mưa
        if (rainParticle == null)
        {
            rainParticle = FindObjectOfType<ParticleSystem>();
            if (rainParticle == null)
            {
                Debug.LogWarning("[RainManager] ⚠ Không tìm thấy ParticleSystem!");
                return;
            }
        }

        // 👉 MOVE VỀ SPAWN CỦA MAP
        rainParticle.transform.position = spawn.transform.position;
        rainParticle.gameObject.SetActive(true);

        if (_isRaining)
        {
            rainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rainParticle.Clear(true);
            rainParticle.Play();

            Debug.Log("[RainManager] 🌧 Reset particle để rebind collision");
        }
        else
        {
            rainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

    }

    // =====================================================
    // WEATHER CALLBACK
    // =====================================================
    private void OnWeatherRainStart(int rainIntensity)
    {
        SetRain(true);
    }

    private void OnWeatherRainEnd()
    {
        SetRain(false);
    }

    // =====================================================
    // API
    // =====================================================
    public void ToggleRain()
    {
        SetRain(!_isRaining);
    }

    public void SetRain(bool value, bool silent = false)
    {
        if (_isRaining == value) return;

        _isRaining = value;
        CachedRainState = value;

        ApplyRainState();

        if (!silent)
        {
            OnRainChanged?.Invoke(_isRaining);
        }
    }
}
