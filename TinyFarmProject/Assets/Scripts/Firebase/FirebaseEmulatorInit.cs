using Firebase;
using Firebase.Database;
using System;
using UnityEngine;

namespace TinyFarm.Firebase
{
    /// <summary>
    /// Initializes Firebase Emulator for local development in Unity Editor
    /// ⭐ PHẢI chạy TRƯỚC mọi script khác (trong Awake, trước FirebaseApp.DefaultInstance)
    /// </summary>
    public class FirebaseEmulatorInit : MonoBehaviour
    {
        private static bool _initialized = false;

        private void Awake()
        {
            if (_initialized) return;
            _initialized = true;

#if UNITY_EDITOR
            try
            {
                // ⭐ BƯỚC 1: Set environment variables TRƯỚC khi gọi FirebaseApp.DefaultInstance
                // Điều này cực kỳ quan trọng - phải set TRƯỚC FirebaseApp check
                
                // Auth Emulator (port 9099)
                System.Environment.SetEnvironmentVariable(
                    "USE_AUTH_EMULATOR",
                    "127.0.0.1:9099"
                );
                Debug.Log("[Firebase] 🔐 Set USE_AUTH_EMULATOR=127.0.0.1:9099");

                // Database Emulator (port 9000)
                System.Environment.SetEnvironmentVariable(
                    "FIREBASE_DATABASE_EMULATOR_HOST",
                    "127.0.0.1:9000"
                );
                Debug.Log("[Firebase] 🗄 Set FIREBASE_DATABASE_EMULATOR_HOST=127.0.0.1:9000");

                // ⭐ BƯỚC 2: GỌI FirebaseApp.DefaultInstance (bây giờ nó sẽ đọc env variables)
                var firebaseApp = FirebaseApp.DefaultInstance;

                // Configure Emulator for Realtime Database
                // Format: http://localhost:port/?ns=projectId
                var databaseUrl = new Uri("http://127.0.0.1:9000/?ns=tinyfarmgameproject");
                
                firebaseApp.Options.DatabaseUrl = databaseUrl;

                Debug.Log($"✅ Firebase Emulator Initialized (Auth + Database)");
                Debug.Log($"📍 Database URL: {databaseUrl}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Firebase Emulator Init Error: {e.Message}");
            }
#endif
        }
    }
}

