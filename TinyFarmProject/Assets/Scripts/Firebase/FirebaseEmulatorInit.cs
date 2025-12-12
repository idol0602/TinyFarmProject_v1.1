using Firebase;
using Firebase.Database;
using System;
using UnityEngine;

namespace TinyFarm.Firebase
{
    /// <summary>
    /// Initializes Firebase Emulator for local development in Unity Editor
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
                // Get Firebase Default Instance
                var firebaseApp = FirebaseApp.DefaultInstance;

                // Configure Emulator for Realtime Database
                // Format: http://localhost:port/?ns=projectId
                var databaseUrl = new Uri("http://127.0.0.1:9000/?ns=tinyfarmgameproject");
                
                firebaseApp.Options.DatabaseUrl = databaseUrl;

                Debug.Log($"✅ Firebase Emulator Initialized");
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
