using UnityEngine;
using System;

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

    private void Update()
    {
        // ⭐ TEST NHANH BẰNG PHÍM R
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRain();
        }
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
