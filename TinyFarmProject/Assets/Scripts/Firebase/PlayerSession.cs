using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Quản lý session người chơi hiện tại
/// Lưu trữ User ID từ Firebase Authentication
/// </summary>
public class PlayerSession : MonoBehaviour
{
    private static PlayerSession _instance;
    private string _currentUserId = "";

    public static PlayerSession Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerSession>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("PlayerSession");
                    _instance = obj.AddComponent<PlayerSession>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Lấy ID của player hiện tại
    /// </summary>
    public static string GetCurrentUserId()
    {
        return Instance._currentUserId;
    }

    /// <summary>
    /// Set ID của player (gọi sau khi login thành công)
    /// </summary>
    public static void SetCurrentUserId(string userId)
    {
        Instance._currentUserId = userId;
        Debug.Log($"[PlayerSession] Current user ID set to: {userId}");
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.ClearCacheForNewUser();
        }
    }

    /// <summary>
    /// Kiểm tra xem đã có user login chưa
    /// </summary>
    public static bool IsUserLoggedIn()
    {
        return !string.IsNullOrEmpty(Instance._currentUserId);
    }

    /// <summary>
    /// Clear session khi logout
    /// </summary>
    public static void ClearSession()
    {
        Instance._currentUserId = "";
        Debug.Log("[PlayerSession] Session cleared");
    }
}
