using UnityEngine;
using Firebase;
using Firebase.Extensions;
using System.Collections;

/// <summary>
/// ⭐ BOOTSTRAP SCRIPT - CHẠY ĐẦU TIÊN TRONG APP
/// 
/// Config Firebase Emulator BEFORE Firebase initialization.
/// </summary>
public class FirebaseBootstrap : MonoBehaviour
{
    private static bool _bootstrapped = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (_bootstrapped)
            return;

        _bootstrapped = true;

#if UNITY_EDITOR
        try
        {
            // ⭐ BƯỚC 1: Set environment variables để Firebase SDK đọc
            System.Environment.SetEnvironmentVariable("USE_AUTH_EMULATOR", "127.0.0.1:9099");
            System.Environment.SetEnvironmentVariable("FIREBASE_DATABASE_EMULATOR_HOST", "127.0.0.1:9000");

            // ⭐ BƯỚC 2: Khởi tạo Firebase App và set DatabaseUrl explicitly
            var firebaseApp = FirebaseApp.DefaultInstance;
            
            // Set Database URL để trỏ tới emulator (phải match namespace đúng)
            firebaseApp.Options.DatabaseUrl = new System.Uri("http://127.0.0.1:9000/?ns=tinyfarmgameproject-default-rtdb");

            Debug.Log("[Firebase Bootstrap] 🚀 Emulator configured:");
            Debug.Log("[Firebase Bootstrap]   - Auth Emulator: 127.0.0.1:9099");
            Debug.Log("[Firebase Bootstrap]   - Database Emulator: 127.0.0.1:9000");
            Debug.Log("[Firebase Bootstrap]   - Database URL: http://127.0.0.1:9000/?ns=tinyfarmgameproject-default-rtdb");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Firebase Bootstrap] ❌ Error: {ex.Message}");
        }
#endif
    }

    private void Awake()
    {
        if (_bootstrapped)
            return;

        _bootstrapped = true;

#if UNITY_EDITOR
        try
        {
            System.Environment.SetEnvironmentVariable("USE_AUTH_EMULATOR", "127.0.0.1:9099");
            System.Environment.SetEnvironmentVariable("FIREBASE_DATABASE_EMULATOR_HOST", "127.0.0.1:9000");
            
            var firebaseApp = FirebaseApp.DefaultInstance;
            firebaseApp.Options.DatabaseUrl = new System.Uri("http://127.0.0.1:9000/?ns=tinyfarmgameproject-default-rtdb");
            
            Debug.Log("[Firebase Bootstrap] 🚀 Awake: Emulator configured");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Firebase Bootstrap] ❌ Awake Error: {ex.Message}");
        }
#endif
    }
}

