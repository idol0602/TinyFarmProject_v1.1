using Firebase;
using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Quản lý khởi tạo Firebase Auth
/// - Trong Unity Editor: dùng Auth Emulator (127.0.0.1:9099)
/// - Trong Build: dùng Firebase Cloud
/// 
/// ⚠️ PHẢI được khởi tạo TRƯỚC khi gọi FirebaseAuth.DefaultInstance
/// </summary>
public class FirebaseAuthManager : MonoBehaviour
{
    private static bool _authEmulatorInitialized = false;

    /// <summary>
    /// Gọi trước khi dùng FirebaseAuth.DefaultInstance
    /// </summary>
    public static void InitializeAuthEmulator()
    {
        if (_authEmulatorInitialized)
            return;

        _authEmulatorInitialized = true;

#if UNITY_EDITOR
        try
        {
            // Set Auth Emulator URL via environment variable
            // Format: "host:port" (127.0.0.1:9099)
            System.Environment.SetEnvironmentVariable(
                "USE_AUTH_EMULATOR",
                "127.0.0.1:9099"
            );
            Debug.Log("[Firebase] 🔐 Using AUTH EMULATOR via USE_AUTH_EMULATOR=127.0.0.1:9099");
            
            // Nếu SDK hỗ trợ UseEmulator, có thể gọi thêm (nhưng environment variable đã đủ)
            // FirebaseAuth.DefaultInstance.UseEmulator("127.0.0.1", 9099);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Failed to init Auth Emulator: {ex.Message}");
        }
#else
        Debug.Log("[Firebase] 🌍 Using Firebase Cloud Auth (PRODUCTION)");
#endif
    }
}
