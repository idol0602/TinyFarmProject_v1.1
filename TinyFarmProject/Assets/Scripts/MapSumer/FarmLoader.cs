using UnityEngine;
using System.Collections;

public class FarmLoader : MonoBehaviour
{
        private string userId => PlayerSession.GetCurrentUserId();
        [SerializeField] private bool useLoadingScreen = true;  // Toggle để use loading screen

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        FirebaseDatabaseManager firebase = FirebaseDatabaseManager.Instance;

        if (firebase == null)
        {
            Debug.LogError("Firebase manager missing!");
            yield break;
        }

        // 🔧 Reload tiền khi chuyển scene
        if (PlayerMoney.Instance != null)
        {
            Debug.Log("[FarmLoader] Reloading money for new scene");
            PlayerMoney.Instance.ReloadMoneyForNewScene();
        }

        // 🔧 Load Inventory khi vào MapSummer
        if (InventoryManager.Instance != null)
        {
            Debug.Log("[FarmLoader] Loading inventory from Firebase");
            firebase.LoadInventoryFromFirebase(userId);
        }

        // ⭐ Load rain state khi vào MapSummer
        Debug.Log("[FarmLoader] Loading rain state from Firebase");
        firebase.LoadRainFromFirebase(userId);

        // 🔧 Nếu enable loading screen, dùng FarmLoadingManager
        if (useLoadingScreen && FarmLoadingManager.Instance != null)
        {
            Debug.Log("[FarmLoader] Using FarmLoadingManager to load farm with loading screen");
            FarmLoadingManager.Instance.StartLoadingFarm(userId);
        }
        else
        {
            // Fallback: load trực tiếp
            Debug.Log("[FarmLoader] Loading farm directly (no loading screen)");
            firebase.LoadFarmFromFirebase(userId);
        }
    }
}

