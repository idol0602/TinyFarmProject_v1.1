using UnityEngine;
using System.Collections;

public class CropFarmLoader : MonoBehaviour
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

        // 🔧 Nếu enable loading screen, dùng FarmLoadingManager
        if (useLoadingScreen && FarmLoadingManager.Instance != null)
        {
            Debug.Log("[CropFarmLoader] Using FarmLoadingManager to load farm with loading screen");
            FarmLoadingManager.Instance.StartLoadingFarm(userId);
        }
        else
        {
            // Fallback: load trực tiếp
            Debug.Log("[CropFarmLoader] Loading farm directly (no loading screen)");
            firebase.LoadFarmFromFirebase(userId);
        }
    }
}


