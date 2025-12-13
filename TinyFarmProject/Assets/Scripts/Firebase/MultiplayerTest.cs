using UnityEngine;
using Firebase.Auth;

/// <summary>
/// Script kiểm tra multiplayer functionality
/// Gắn vào một GameObject trong scene để test
/// </summary>
public class MultiplayerTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== MULTIPLAYER TEST STARTED ===");
        TestPlayerSession();
        TestFirebaseLogin();
    }

    private void Update()
    {
        // In ra User ID hiện tại mỗi 5 giây
        if (Time.time % 5f < 0.016f)  // ~every 5 seconds
        {
            DebugCurrentUser();
        }
    }

    /// <summary>
    /// Test PlayerSession functionality
    /// </summary>
    private void TestPlayerSession()
    {
        Debug.Log("[TEST] PlayerSession Tests");
        Debug.Log($"  • Instance exists: {PlayerSession.Instance != null}");
        Debug.Log($"  • Current User ID: {PlayerSession.GetCurrentUserId() ?? "(empty)"}");
        Debug.Log($"  • Is Logged In: {PlayerSession.IsUserLoggedIn()}");
    }

    /// <summary>
    /// Test Firebase Login functionality
    /// </summary>
    private void TestFirebaseLogin()
    {
        Debug.Log("[TEST] Firebase Authentication Tests");
        
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser != null)
        {
            Debug.Log($"  ✅ User logged in");
            Debug.Log($"  • Firebase UID: {currentUser.UserId}");
            Debug.Log($"  • Email: {currentUser.Email}");
            Debug.Log($"  • Session UID: {PlayerSession.GetCurrentUserId()}");
            
            // Verify they match
            if (currentUser.UserId == PlayerSession.GetCurrentUserId())
            {
                Debug.Log("  ✅ PASS: Firebase UID matches PlayerSession UID");
            }
            else
            {
                Debug.LogWarning("  ⚠️ MISMATCH: Firebase UID != PlayerSession UID");
            }
        }
        else
        {
            Debug.Log("  ℹ️ No user logged in (login first)");
        }
    }

    /// <summary>
    /// Continuously debug current user
    /// </summary>
    private void DebugCurrentUser()
    {
        string userId = PlayerSession.GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            Debug.Log($"[Current User] {userId}");
        }
    }

    /// <summary>
    /// Manual test: Simulate setting user ID
    /// Call từ Developer Console hoặc button
    /// </summary>
    public void TestSetUserId(string testId)
    {
        Debug.Log($"[TEST] Setting User ID to: {testId}");
        PlayerSession.SetCurrentUserId(testId);
        Debug.Log($"[TEST] New User ID: {PlayerSession.GetCurrentUserId()}");
    }

    /// <summary>
    /// Manual test: Simulate logout
    /// </summary>
    public void TestLogout()
    {
        Debug.Log("[TEST] Clearing session (logout)");
        PlayerSession.ClearSession();
        Debug.Log($"[TEST] Current User ID after logout: {PlayerSession.GetCurrentUserId() ?? "(empty)"}");
    }

    /// <summary>
    /// Manual test: Check if logged in
    /// </summary>
    public void TestIsLoggedIn()
    {
        bool isLoggedIn = PlayerSession.IsUserLoggedIn();
        Debug.Log($"[TEST] Is Logged In: {isLoggedIn}");
        
        if (isLoggedIn)
        {
            Debug.Log($"[TEST] Current User ID: {PlayerSession.GetCurrentUserId()}");
        }
        else
        {
            Debug.LogWarning("[TEST] No user logged in");
        }
    }

    /// <summary>
    /// Test Firebase Database operations with current user
    /// </summary>
    public void TestFirebaseOperations()
    {
        string userId = PlayerSession.GetCurrentUserId();
        
        Debug.Log("[TEST] Firebase Database Operations");
        
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("  ❌ FAIL: No user logged in");
            return;
        }

        if (!FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.LogError("  ❌ FAIL: Firebase not ready");
            return;
        }

        Debug.Log($"  ✅ User ID: {userId}");
        Debug.Log($"  ✅ Firebase Ready: {FirebaseDatabaseManager.FirebaseReady}");
        
        // Example: Test save
        Debug.Log($"  → Test: SaveFarmToFirebase('{userId}')");
        // FirebaseDatabaseManager.Instance.SaveFarmToFirebase(userId);
        
        Debug.Log("  ℹ️ Uncomment to actually save/load");
    }

    /// <summary>
    /// Log all important info
    /// </summary>
    public void PrintDiagnostics()
    {
        Debug.Log("===== MULTIPLAYER DIAGNOSTICS =====");
        
        // PlayerSession
        Debug.Log("[PlayerSession]");
        Debug.Log($"  Current User ID: {PlayerSession.GetCurrentUserId() ?? "(empty)"}");
        Debug.Log($"  Is Logged In: {PlayerSession.IsUserLoggedIn()}");
        
        // Firebase Auth
        Debug.Log("[Firebase Auth]");
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseUser user = auth.CurrentUser;
        Debug.Log($"  Logged In User: {user?.UserId ?? "(none)"}");
        Debug.Log($"  Email: {user?.Email ?? "(none)"}");
        
        // Firebase Database
        Debug.Log("[Firebase Database]");
        Debug.Log($"  Ready: {FirebaseDatabaseManager.FirebaseReady}");
        Debug.Log($"  Instance: {FirebaseDatabaseManager.Instance != null}");
        
        // Loaders
        Debug.Log("[Game Managers]");
        Debug.Log($"  FarmLoadingManager: {FarmLoadingManager.Instance != null}");
        Debug.Log($"  PlayerMoney: {PlayerMoney.Instance != null}");
        Debug.Log($"  InventoryManager: {InventoryManager.Instance != null}");
        Debug.Log($"  DayAndNightManager: {DayAndNightManager.Instance != null}");
        
        Debug.Log("====================================");
    }
}
