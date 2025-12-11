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

        string json = JsonConvert.SerializeObject(crops, Formatting.Indented);

        reference.Child("Farms").Child(userId)
            .SetValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log($"📤 Farm Saved ({crops.Count} cây)");
            });
    }

    // ============================================================
    // LOAD FARM — VERSION HỖ TRỢ NHIỀU LOẠI CÂY
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
                    Debug.Log("⚠ Firebase không có dữ liệu farm");
                    return;
                }

                string json = snap.Value.ToString();
                List<CropData> crops = JsonConvert.DeserializeObject<List<CropData>>(json);

                // Xóa cây hiện tại
                foreach (var old in GameObject.FindObjectsOfType<Crop>())
                    Destroy(old.gameObject);

                // Load từng cây theo đúng loại
                // Load từng cây theo đúng loại
                foreach (var d in crops)
                {
                    string path = "Crops/" + d.cropType;
                    GameObject prefab = Resources.Load<GameObject>(path);

                    if (prefab == null)
                    {
                        Debug.LogError("❌ Không tìm thấy prefab cho loại cây: " + d.cropType);
                        continue;
                    }

                    Vector3 pos = new Vector3(d.posX, d.posY, 0);
                    GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

                    obj.GetComponent<Crop>().LoadFromData(d);
                }

                // ⭐ FIX QUAN TRỌNG: BẮN LẠI EVENT SAU KHI LOAD FARM
                // ⭐ CHỈ GỌI OnNewDay NẾU VỪA NGỦ DẬY
                int day = DayAndNightManager.Instance.GetCurrentDay();

                if (FarmState.IsSleepTransition)
                {
                    Debug.Log("😴 LoadFarm → SleepTransition TRUE → OnNewDay()");
                    FarmState.IsSleepTransition = false;

                    DayAndNightEvents.InvokeNewDay(day);
                }
                else
                {
                    Debug.Log("🌱 LoadFarm → Bình thường → không OnNewDay()");
                }


                Debug.Log("🌱 Farm Loaded xong!");

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
