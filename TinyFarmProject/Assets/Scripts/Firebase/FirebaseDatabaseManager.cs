using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using MapSummer;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance;
    public static bool FirebaseReady = false;

    private DatabaseReference reference;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitFirebase();
    }

    private async void InitFirebase()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == DependencyStatus.Available)
        {
            reference = FirebaseDatabase.DefaultInstance.RootReference;
            FirebaseReady = true;
            Debug.Log("🔥 Firebase Ready");
        }
        else
        {
            Debug.LogError("❌ Firebase lỗi: " + status);
        }
    }

    // ============================================================
    // SAVE FARM
    // ============================================================
    public void SaveFarmToFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("❌ Firebase chưa sẵn sàng → KHÔNG SAVE");
            return;
        }

        List<CropData> crops = new List<CropData>();

        foreach (var crop in GameObject.FindObjectsOfType<Crop>())
            crops.Add(new CropData(crop));

        string json = JsonConvert.SerializeObject(crops);

        reference.Child("Farms").Child(userId)
            .SetValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log($"📤 Farm Saved ({crops.Count} cây)");
            });
    }

    // ============================================================
    // LOAD FARM
    // ============================================================
    public void LoadFarmFromFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("❌ Firebase chưa sẵn sàng → KHÔNG LOAD");
            return;
        }

        reference.Child("Farms").Child(userId).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("❌ Load lỗi → " + task.Exception);
                    return;
                }

                DataSnapshot snap = task.Result;

                if (snap.Value == null)
                {
                    Debug.Log("⚠ Firebase không có farm");
                    return;
                }

                string json = snap.Value.ToString();
                List<CropData> crops = JsonConvert.DeserializeObject<List<CropData>>(json);

                // Xóa cây hiện tại
                foreach (var old in GameObject.FindObjectsOfType<Crop>())
                    Destroy(old.gameObject);

                GameObject prefab = Resources.Load<GameObject>("CropPrefab");

                foreach (var d in crops)
                {
                    GameObject obj = Instantiate(prefab, new Vector3(d.posX, d.posY, 0), Quaternion.identity);
                    obj.GetComponent<Crop>().LoadFromData(d);
                }

                Debug.Log("🌱 Farm Loaded");
            });
    }

    private void OnApplicationQuit()
    {
        if (FirebaseReady)
        {
            Debug.Log("💾 Auto SAVE khi thoát game");
            SaveFarmToFirebase("Player1");
        }
    }
}
