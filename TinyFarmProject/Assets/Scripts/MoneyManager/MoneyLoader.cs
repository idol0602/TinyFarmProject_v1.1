using UnityEngine;
using System.Collections;

public class MoneyLoader : MonoBehaviour
{
    public string userId = "Player1";
    
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

        // 🔧 Reload tiền khi chuyển scene (đợi một chút để UI setup)
        if (PlayerMoney.Instance != null)
        {
            Debug.Log("[MoneyLoader] Reloading money for new scene");
            yield return new WaitForSeconds(0.1f);  // Đợi UI setup
            PlayerMoney.Instance.ReloadMoneyForNewScene();
        }

        // 🔧 Nếu enable loading screen, dùng FarmLoadingManager
        if (useLoadingScreen && FarmLoadingManager.Instance != null)
        {
            Debug.Log("[MoneyLoader] Using FarmLoadingManager to load farm with loading screen");
            FarmLoadingManager.Instance.StartLoadingFarm(userId);
        }
        else
        {
            // Fallback: load trực tiếp
            Debug.Log("[MoneyLoader] Loading farm directly (no loading screen)");
            firebase.LoadFarmFromFirebase(userId);
        }
    }
}

