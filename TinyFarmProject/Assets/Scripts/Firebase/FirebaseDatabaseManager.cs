using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using MapSummer;
using System;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance;
    public static bool FirebaseReady = false;
    private DatabaseReference reference;

    private const string DEFAULT_USER_ID = "Player1"; // Đổi nếu có login

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

            // Tự động load tiền + farm khi mở game
            LoadAllPlayerData(DEFAULT_USER_ID);
        }
        else
        {
            Debug.LogError("Firebase lỗi: " + status);
        }
    }

    // ============================================================
    // SAVE FARM (giữ nguyên như cũ)
    // ============================================================
    public void SaveFarmToFirebase(string userId)
    {
        if (!FirebaseReady) return;

        List<CropData> crops = new List<CropData>();
        foreach (var crop in GameObject.FindObjectsOfType<Crop>())
            crops.Add(new CropData(crop));

        string json = JsonConvert.SerializeObject(crops, Formatting.Indented);

        reference.Child("Farms").Child(userId)
            .SetValueAsync(json)
            .ContinueWithOnMainThread(task =>
                Debug.Log($"Farm Saved ({crops.Count} cây)"));
    }

    public void LoadFarmFromFirebase(string userId)
    {
        // (giữ nguyên code load farm như cũ của bạn – không thay đổi)
        // ... (đoạn code load farm bạn đã có)
    }

    // ============================================================
    // SAVE MONEY → Money/Player1 = 102500
    // ============================================================
    public void SaveMoneyToFirebase(string userId = DEFAULT_USER_ID)
    {
        if (!FirebaseReady || reference == null) return;

        int money = PlayerMoney.Instance?.GetCurrentMoney() ?? 0;

        reference.Child("Money").Child(userId)
            .SetValueAsync(money)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Lưu tiền thất bại: " + task.Exception);
                else
                    Debug.Log($"ĐÃ LƯU TIỀN → Money/{userId} = {money:N0}đ");
            });
    }

    // ============================================================
    // LOAD MONEY → từ Money/Player1
    // ============================================================
    public void LoadMoneyFromFirebase(string userId = DEFAULT_USER_ID, Action<int> onComplete = null)
    {
        if (!FirebaseReady || reference == null)
        {
            onComplete?.Invoke(1000);
            return;
        }

        reference.Child("Money").Child(userId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                int money = 1000; // mặc định

                if (task.IsCompletedSuccessfully && task.Result.Exists && task.Result.Value != null)
                {
                    money = Convert.ToInt32(task.Result.Value);
                }

                // CHỈ CẦN GỌI CALLBACK – PlayerMoney sẽ tự xử lý
                onComplete?.Invoke(money);
            });
    }

    // ============================================================
    // TỰ ĐỘNG LOAD KHI MỞ GAME
    // ============================================================
    private void LoadAllPlayerData(string userId)
    {
        LoadMoneyFromFirebase(userId, (money) =>
        {
            LoadFarmFromFirebase(userId);
        });
    }

    // ============================================================
    // TỰ ĐỘNG SAVE KHI THOÁT + KHI TIỀN THAY ĐỔI
    // ============================================================
    private void OnApplicationQuit()
    {
        if (FirebaseReady)
        {
            SaveMoneyToFirebase();
            SaveFarmToFirebase(DEFAULT_USER_ID);
            Debug.Log("Đã tự động lưu toàn bộ dữ liệu khi thoát game");
        }
    }

    private void OnEnable()
    {
        if (PlayerMoney.Instance != null)
            PlayerMoney.Instance.OnMoneyChanged.AddListener(_ => SaveMoneyToFirebase());
    }

    private void OnDisable()
    {
        if (PlayerMoney.Instance != null)
            PlayerMoney.Instance.OnMoneyChanged.RemoveListener(_ => SaveMoneyToFirebase());
    }
}