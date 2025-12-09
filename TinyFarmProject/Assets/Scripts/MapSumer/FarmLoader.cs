using UnityEngine;
using System.Collections;

public class FarmLoader : MonoBehaviour
{
    public string userId = "Player1";

    private void Start()
    {
        StartCoroutine(LoadFarmRoutine());
    }

    IEnumerator LoadFarmRoutine()
    {
        // ⭐ B1: CHỜ FirebaseDatabaseManager được tạo (Awake đã chạy)
        while (FirebaseDatabaseManager.Instance == null)
        {
            yield return null;
        }

        // ⭐ B2: CHỜ Firebase thực sự sẵn sàng (async init)
        while (!FirebaseDatabaseManager.FirebaseReady)
        {
            yield return null;
        }

        // ⭐ B3: GỌI LOAD
        FirebaseDatabaseManager.Instance.LoadFarmFromFirebase(userId);

        Debug.Log("🌱 FarmLoader → LoadFarmFromFirebase DONE!");
    }
}
