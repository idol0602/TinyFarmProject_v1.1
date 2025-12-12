using UnityEngine;
using System;

public class RainManager : MonoBehaviour
{
    public static RainManager Instance;

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

        // ⭐ ĐẢM BẢO KHI START GAME KHÔNG MƯA
        SetRain(false, true);
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
