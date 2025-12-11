using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using MapSummer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

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
            Debug.Log("Firebase Ready");
        }
        else
        {
            Debug.LogError("Firebase lỗi: " + status);
        }
    }

    // ============================================================
    // SAVE MONEY (chỉ dùng 1 hàm duy nhất này)
    // ============================================================
    public void SaveMoneyToFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG SAVE TIỀN");
            return;
        }

        int money = PlayerMoney.Instance != null ? PlayerMoney.Instance.GetCurrentMoney() : 0;

        reference.Child("Money").Child(userId)
            .SetValueAsync(money)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Lỗi SAVE tiền: " + task.Exception);
                else
                    Debug.Log($"Đã lưu tiền: {money:N0}đ → /Money/{userId}");
            });
    }

    // ============================================================
    // LOAD MONEY (chỉ dùng 1 hàm duy nhất này)
    // ============================================================
    public void LoadMoneyFromFirebase(string userId, Action<int> callback)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG LOAD TIỀN");
            callback?.Invoke(1000); // fallback về tiền mặc định
            return;
        }

        reference.Child("Money").Child(userId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("Lỗi LOAD tiền: " + task.Exception);
                    callback?.Invoke(1000);
                    return;
                }

                DataSnapshot snap = task.Result;

                int loadedMoney = 1000; // default

                if (snap.Value != null)
                {
                    loadedMoney = int.Parse(snap.Value.ToString());
                    Debug.Log($"LOAD tiền thành công từ Firebase: {loadedMoney:N0}đ");
                }
                else
                {
                    Debug.Log("Không có dữ liệu tiền trên Firebase → dùng default 1000đ");
                }

                callback?.Invoke(loadedMoney);
            });
    }

    // ============================================================
    // SAVE FARM
    // ============================================================
    public void SaveFarmToFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG SAVE FARM");
            return;
        }

        List<CropData> crops = new List<CropData>();
        foreach (var crop in FindObjectsOfType<Crop>())
            crops.Add(new CropData(crop));

        string json = JsonConvert.SerializeObject(crops, Formatting.Indented);

        reference.Child("Farms").Child(userId)
            .SetValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log($"Farm Saved ({crops.Count} cây trồng)");
            });
    }

    // ============================================================
    // LOAD FARM
    // ============================================================
    public void LoadFarmFromFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG LOAD FARM");
            return;
        }

        reference.Child("Farms").Child(userId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("Load farm lỗi: " + task.Exception);
                    return;
                }

                DataSnapshot snap = task.Result;

                if (snap.Value == null)
                {
                    Debug.Log("Firebase không có dữ liệu farm → để trống");
                    return;
                }

                string json = snap.Value.ToString();
                List<CropData> crops = JsonConvert.DeserializeObject<List<CropData>>(json);

                // Xóa cây cũ
                foreach (var old in FindObjectsOfType<Crop>())
                    Destroy(old.gameObject);

                // Tạo lại cây
                foreach (var d in crops)
                {
                    string path = "Crops/" + d.cropType;
                    GameObject prefab = Resources.Load<GameObject>(path);
                    if (prefab == null)
                    {
                        Debug.LogError("Không tìm thấy prefab: " + d.cropType);
                        continue;
                    }

                    Vector3 pos = new Vector3(d.posX, d.posY, 0);
                    GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
                    obj.GetComponent<Crop>().LoadFromData(d);
                }

                // New day event khi ngủ dậy
                int day = DayAndNightManager.Instance.GetCurrentDay();
                if (FarmState.IsSleepTransition)
                {
                    FarmState.IsSleepTransition = false;
                    DayAndNightEvents.InvokeNewDay(day);
                }

                Debug.Log("Farm Loaded xong!");
            });
    }

    // Auto save farm khi thoát game
    private void OnApplicationQuit()
    {
        if (FirebaseReady)
        {
            Debug.Log("Auto SAVE farm + tiền khi thoát game");
            SaveFarmToFirebase("Player1");
            SaveMoneyToFirebase("Player1");
        }
    }
}