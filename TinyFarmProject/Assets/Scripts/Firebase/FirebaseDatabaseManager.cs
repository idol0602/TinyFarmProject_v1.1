using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using MapSummer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance;
    public static bool FirebaseReady = false;

    private DatabaseReference reference;
    
    // 🔧 Track xem inventory đã được load từ Firebase hay chưa
    private bool inventoryLoaded = false;
    
    // 🔧 Track xem farm đã được load từ Firebase hay chưa
    private bool farmLoaded = false;
    
    // � Cache day/time data từ Firebase để DayAndNightManager có thể lấy ngay
    public static DayTimeData CachedDayTimeData = null;
    
    // �📢 Event callback khi farm load xong
    public static event Action<bool> OnFarmLoadComplete;

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
        
        // ⭐ LẮNG NGHE SCENE UNLOAD ĐỂ SAVE RAIN STATE
        SceneManager.sceneUnloaded += OnSceneUnloaded;
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

    // ⭐ SAVE RAIN STATE KHI UNLOAD SCENE
    private void OnSceneUnloaded(Scene scene)
    {
        if (FirebaseReady && scene.name == "MapSummer")
        {
            SaveDayAndTimeToFirebase(PlayerSession.GetCurrentUserId());
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
        
        Debug.Log($"[Firebase] Saving money: {money:N0}đ → /{userId}/Money");

        reference.Child(userId).Child("Money")
            .SetValueAsync(money)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Lỗi SAVE tiền: " + task.Exception);
                else
                    Debug.Log($"✓ Đã lưu tiền: {money:N0}đ → /{userId}/Money");
            });
    }

    // ============================================================
    // SAVE ORDERS (lưu danh sách order lên Firebase)
    // ============================================================
    public void SaveOrdersToDatabase(string userId, string ordersJson)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG SAVE ORDERS");
            return;
        }

        Debug.Log($"[Firebase] Saving orders to /{userId}/Orders");

        reference.Child(userId).Child("Orders")
            .SetValueAsync(ordersJson)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Lỗi SAVE orders: " + task.Exception);
                else
                    Debug.Log($"✓ Đã lưu orders lên Firebase");
            });
    }

    // ============================================================
    // SAVE DAY AND TIME (chỉ dùng 1 hàm duy nhất này)
    // ============================================================
    public void SaveDayAndTimeToFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG SAVE DAY/TIME");
            return;
        }

        if (DayAndNightManager.Instance == null)
        {
            Debug.LogError("DayAndNightManager không tìm thấy");
            return;
        }

        int currentDay = DayAndNightManager.Instance.GetCurrentDay();
        int currentHour = DayAndNightManager.Instance.GetCurrentHour();
        int currentMinute = DayAndNightManager.Instance.GetCurrentMinute();
        
        // Tạo data structure cho day/time
        DayTimeData dayTimeData = new DayTimeData
        {
            currentDay = currentDay,
            currentHour = currentHour,
            currentMinute = currentMinute
        };

        // 🔧 Convert sang Dictionary thay vì JSON string
        // ⭐ LẤY TRẠNG THÁI MƯA
        bool isRaining = RainManager.Instance != null ? RainManager.Instance.isRaining : false;
        
        var updates = new Dictionary<string, object>
        {
            { $"{userId}/DayAndTime/currentDay", currentDay },
            { $"{userId}/DayAndTime/currentHour", currentHour },
            { $"{userId}/DayAndTime/currentMinute", currentMinute },
            { $"{userId}/DayAndTime/isRaining", isRaining }
        };
        
        Debug.Log($"[Firebase] Saving day/time: Day {currentDay} {currentHour:00}:{currentMinute:00} (Mưa: {isRaining}) → /{userId}/DayAndTime");

        reference.UpdateChildrenAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("❌ Lỗi SAVE day/time: " + task.Exception);
                else
                    Debug.Log($"✓ Đã lưu day/time: Day {currentDay} {currentHour:00}:{currentMinute:00} → /{userId}/DayAndTime");
            });
    }

    // ============================================================
    // LOAD RAIN STATE
    // ============================================================
    public void LoadRainFromFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogWarning("[Firebase] Firebase chưa sẵn sàng → không load rain state");
            return;
        }

        reference.Child(userId).Child("DayAndTime").Child("isRaining")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogWarning("[Firebase] Lỗi load rain state: " + task.Exception);
                    return;
                }

                DataSnapshot snap = task.Result;

                if (snap.Value != null)
                {
                    bool isRaining = System.Convert.ToBoolean(snap.Value);
                    if (RainManager.Instance != null)
                    {
                        RainManager.Instance.SetRain(isRaining, silent: true);
                        Debug.Log($"☔ Load rain state: {(isRaining ? "MƯA" : "HẾT MƯA")}");
                    }
                }
                else
                {
                    Debug.Log("[Firebase] Không có rain state → mặc định hết mưa");
                    if (RainManager.Instance != null)
                        RainManager.Instance.SetRain(false, silent: true);
                }
            });
    }

    // ============================================================
    // LOAD DAY AND TIME (chỉ dùng 1 hàm duy nhất này)
    // ============================================================
    public void LoadDayAndTimeFromFirebase(string userId, Action<DayTimeData> callback)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG LOAD DAY/TIME");
            callback?.Invoke(new DayTimeData { currentDay = 1, currentHour = 7, currentMinute = 0 }); // fallback
            return;
        }

        Debug.Log($"[Firebase] Loading day/time from /{userId}/DayAndTime...");
        reference.Child(userId).Child("DayAndTime")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("Lỗi LOAD day/time: " + task.Exception);
                    callback?.Invoke(new DayTimeData { currentDay = 1, currentHour = 7, currentMinute = 0 });
                    return;
                }

                DataSnapshot snap = task.Result;
                DayTimeData loadedData = new DayTimeData { currentDay = 1, currentHour = 7, currentMinute = 0 }; // default

                if (snap.Value != null)
                {
                    Debug.Log($"[Firebase] snap.Value type (DayAndTime): {snap.Value.GetType()}, value: {snap.Value}");
                    try
                    {
                        // 🔧 Firebase trả về Dictionary với 3 fields
                        if (snap.Value is Dictionary<string, object> dict)
                        {
                            loadedData = new DayTimeData();
                            if (dict.TryGetValue("currentDay", out var dayObj))
                                loadedData.currentDay = System.Convert.ToInt32(dayObj);
                            if (dict.TryGetValue("currentHour", out var hourObj))
                                loadedData.currentHour = System.Convert.ToInt32(hourObj);
                            if (dict.TryGetValue("currentMinute", out var minObj))
                                loadedData.currentMinute = System.Convert.ToInt32(minObj);
                            
                            Debug.Log($"✓ LOAD day/time thành công từ Firebase: Day {loadedData.currentDay} {loadedData.currentHour:00}:{loadedData.currentMinute:00}");
                        }
                        else
                        {
                            Debug.LogWarning($"⚠ snap.Value type không phải Dictionary: {snap.Value.GetType()}");
                        }
                        
                        if (loadedData.currentDay <= 0)
                        {
                            Debug.LogWarning($"⚠ Day bằng {loadedData.currentDay} (invalid) → dùng default 1");
                            loadedData.currentDay = 1;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"❌ Lỗi parse day/time: {ex.Message}, raw value: {snap.Value}");
                    }
                }
                else
                {
                    Debug.Log("⚠ Không có dữ liệu day/time trên Firebase → dùng default");
                }

                // 🔧 CACHE data để DayAndNightManager có thể dùng ngay
                CachedDayTimeData = loadedData;
                Debug.Log($"[Firebase] ✅ Cached day/time: Day {loadedData.currentDay} {loadedData.currentHour:00}:{loadedData.currentMinute:00}");
                
                callback?.Invoke(loadedData);
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

        Debug.Log($"[Firebase] Loading money from /{userId}/Money...");
        reference.Child(userId).Child("Money")
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
                    Debug.Log($"[Firebase] snap.Value type: {snap.Value.GetType()}, value: {snap.Value}");
                    try
                    {
                        // ⚠️ Convert từ Int64 (Firebase default)
                        loadedMoney = (int)System.Convert.ToInt64(snap.Value);
                        
                        // ✅ Nếu load được 0 hoặc âm, dùng default 1000
                        if (loadedMoney <= 0)
                        {
                            Debug.LogWarning($"⚠ Money bằng {loadedMoney} (invalid) → dùng default 1000đ");
                            loadedMoney = 1000;
                        }
                        else
                        {
                            Debug.Log($"✓ LOAD tiền thành công từ Firebase: {loadedMoney:N0}đ");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"❌ Lỗi parse tiền: {ex.Message}, raw value: {snap.Value}");
                        loadedMoney = 1000;
                    }
                }
                else
                {
                    Debug.Log("⚠ Không có dữ liệu tiền trên Firebase → dùng default 1000đ");
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

        reference.Child(userId).Child("Farms")
            .SetValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log($"Farm Saved ({crops.Count} cây trồng)");
            });
    }

    // ============================================================
    // LOAD FARM
    // ============================================================
    public void LoadFarmFromFirebase(string userId, System.Action onLoadComplete = null)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG LOAD FARM");
            onLoadComplete?.Invoke();
            return;
        }

        reference.Child(userId).Child("Farms")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("Load farm lỗi: " + task.Exception);
                    farmLoaded = true;
                    onLoadComplete?.Invoke();
                    return;
                }

                DataSnapshot snap = task.Result;

                if (snap.Value == null)
                {
                    Debug.Log("Firebase không có dữ liệu farm → xóa cây cũ");
                    
                    // ⭐ XÓA CÂY CŨ NGAY CẢ KHI FIREBASE TRỐNG
                    foreach (var old in FindObjectsOfType<Crop>())
                        Destroy(old.gameObject);
                    
                    farmLoaded = true;
                    onLoadComplete?.Invoke();
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
                
                // 🔧 Mark farm as loaded + invoke callback
                farmLoaded = true;
                OnFarmLoadComplete?.Invoke(true);
                onLoadComplete?.Invoke();
            });
    }

    // ============================================================
    // SAVE INVENTORY (cả 2 inventory: main + second)
    // ============================================================
    public void SaveInventoryToFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG SAVE INVENTORY");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager không tìm thấy");
            return;
        }

        Debug.Log("[Firebase] Starting SaveInventoryToFirebase...");

        // Tạo data structure cho main inventory
        List<SlotItemData> mainInventoryData = new List<SlotItemData>();
        for (int i = 0; i < InventoryManager.Instance.inventorySize; i++)
        {
            var slotData = InventoryManager.Instance.GetSlotData(i);
            if (slotData != null && slotData.item != null)
            {
                mainInventoryData.Add(new SlotItemData
                {
                    slotIndex = i,  // 🔧 Dùng vị trí loop hiện tại, không phải slotData.slotIndex cũ
                    itemName = slotData.item.itemName,
                    quantity = slotData.quantity
                });
                Debug.Log($"  [Main] Slot {i}: {slotData.item.itemName} x{slotData.quantity}");
            }
        }

        // Tạo data structure cho second inventory
        List<SlotItemData> secondInventoryData = new List<SlotItemData>();
        for (int i = 0; i < InventoryManager.Instance.secondInventorySize; i++)
        {
            var slotData = InventoryManager.Instance.GetSecondSlotData(i);
            if (slotData != null && slotData.item != null)
            {
                secondInventoryData.Add(new SlotItemData
                {
                    slotIndex = i,  // 🔧 Dùng vị trí loop hiện tại, không phải slotData.slotIndex cũ
                    itemName = slotData.item.itemName,
                    quantity = slotData.quantity
                });
                Debug.Log($"  [Second] Slot {i}: {slotData.item.itemName} x{slotData.quantity}");
            }
        }

        // Save main inventory as JSON string
        string mainJson = JsonConvert.SerializeObject(mainInventoryData, Formatting.Indented);
        Debug.Log($"[Firebase] Saving main inventory JSON:\n{mainJson}");
        
        reference.Child(userId).Child("main")
            .SetValueAsync(mainJson)  // ✅ Lưu JSON string để tránh format mismatch
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("❌ Lỗi SAVE main inventory: " + task.Exception);
                else
                    Debug.Log($"✓ Đã lưu main inventory ({mainInventoryData.Count} items) → /{userId}/main");
            });

        // Save second inventory as JSON string
        string secondJson = JsonConvert.SerializeObject(secondInventoryData, Formatting.Indented);
        Debug.Log($"[Firebase] Saving second inventory JSON:\n{secondJson}");
        
        reference.Child(userId).Child("second")
            .SetValueAsync(secondJson)  // ✅ Lưu JSON string để tránh format mismatch
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("❌ Lỗi SAVE second inventory: " + task.Exception);
                else
                    Debug.Log($"✓ Đã lưu second inventory ({secondInventoryData.Count} items) → /{userId}/second");
            });
    }

    // ============================================================
    // LOAD INVENTORY (cả 2 inventory: main + second)
    // ============================================================
    public void LoadInventoryFromFirebase(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG LOAD INVENTORY");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager không tìm thấy");
            return;
        }

        // 🔧 Ensure ItemDatabase is initialized
        if (ItemDatabase.Instance == null)
        {
            Debug.LogWarning("[Firebase] ItemDatabase not found, creating it...");
            GameObject dbGO = new GameObject("ItemDatabase");
            dbGO.AddComponent<ItemDatabase>();
        }

        // Load main inventory
        reference.Child(userId).Child("main")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogWarning("Load main inventory lỗi hoặc không có dữ liệu: " + task.Exception);
                    return;
                }

                DataSnapshot snap = task.Result;
                Debug.Log($"[Firebase] Main inventory snap.Value is null: {snap.Value == null}");
                
                if (snap.Value != null)
                {
                    Debug.Log($"[Firebase] snap.Value type: {snap.Value.GetType()}, value: {snap.Value}");
                    
                    List<SlotItemData> mainInventoryData = new List<SlotItemData>();
                    
                    try
                    {
                        // Load as JSON string (consistent format)
                        string json = snap.Value.ToString();
                        Debug.Log($"[Firebase] Main inventory JSON string: {json}");
                        
                        mainInventoryData = JsonConvert.DeserializeObject<List<SlotItemData>>(json);
                        
                        if (mainInventoryData == null)
                        {
                            Debug.LogWarning("[Firebase] mainInventoryData is NULL after deserialize!");
                            mainInventoryData = new List<SlotItemData>();
                        }
                        else
                        {
                            Debug.Log($"[Firebase] ✅ Loaded main inventory from JSON: {mainInventoryData.Count} items");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Firebase] ❌ Failed to deserialize main inventory: {ex.Message}");
                        mainInventoryData = new List<SlotItemData>();
                    }

                    // Xóa inventory cũ
                    InventoryManager.Instance.ClearInventory();
                    Debug.Log("[Firebase] Cleared main inventory");

                    // Load items vào main inventory
                    if (mainInventoryData != null && mainInventoryData.Count > 0)
                    {
                        Debug.Log($"[Firebase] 🔄 Loading {mainInventoryData.Count} items into main inventory");
                        foreach (var item in mainInventoryData)
                        {
                            // Tìm ItemData từ InventoryManager
                            ItemData itemData = InventoryManager.Instance.GetItemDataByName(item.itemName);
                            
                            if (itemData != null)
                            {
                                Debug.Log($"  ✅ Loaded: {item.itemName} x{item.quantity}");
                                InventoryManager.Instance.AddItem(itemData, item.quantity);
                            }
                            else
                            {
                                Debug.LogWarning($"  ✗ Item not found: '{item.itemName}' (check InventoryManager slotSlot or secondSlotSlot)");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[Firebase] ⚠️ No items to load in main inventory (count=0 or null)");
                    }

                    Debug.Log("✅ LOAD main inventory thành công!");
                }
                else
                {
                    Debug.Log("⚠️ Không có dữ liệu main inventory trên Firebase (snap.Value = null)");
                }
            });

        // Load second inventory
        reference.Child(userId).Child("second")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogWarning("Load second inventory lỗi hoặc không có dữ liệu: " + task.Exception);
                    return;
                }

                DataSnapshot snap = task.Result;
                if (snap.Value != null)
                {
                    Debug.Log($"[Firebase] snap.Value type (second): {snap.Value.GetType()}, value: {snap.Value}");
                    
                    List<SlotItemData> secondInventoryData = new List<SlotItemData>();
                    
                    try
                    {
                        // Load as JSON string (consistent format)
                        string json = snap.Value.ToString();
                        secondInventoryData = JsonConvert.DeserializeObject<List<SlotItemData>>(json);
                        
                        if (secondInventoryData == null)
                            secondInventoryData = new List<SlotItemData>();
                        
                        Debug.Log($"[Firebase] Loaded second inventory from JSON: {secondInventoryData.Count} items");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Firebase] Failed to deserialize second inventory: {ex.Message}");
                        secondInventoryData = new List<SlotItemData>();
                    }

                    // Xóa second inventory cũ
                    InventoryManager.Instance.ClearSecondInventory();
                    Debug.Log("[Firebase] Cleared second inventory");

                    // Load items vào second inventory
                    if (secondInventoryData != null && secondInventoryData.Count > 0)
                    {
                        Debug.Log($"[Firebase] Loading {secondInventoryData.Count} items into second inventory");
                        foreach (var item in secondInventoryData)
                        {
                            // Tìm ItemData từ InventoryManager
                            ItemData itemData = InventoryManager.Instance.GetItemDataByName(item.itemName);
                            
                            if (itemData != null)
                            {
                                Debug.Log($"  → Loaded: {item.itemName} x{item.quantity}");
                                InventoryManager.Instance.AddItemToSecond(itemData, item.quantity);
                            }
                            else
                            {
                                Debug.LogWarning($"  ✗ Item not found: '{item.itemName}' (check InventoryManager slotSlot or secondSlotSlot)");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[Firebase] No items to load in second inventory");
                    }

                    Debug.Log("✓ LOAD second inventory thành công!");
                    
                    // 🔧 Mark inventory as loaded
                    inventoryLoaded = true;
                }
                else
                {
                    Debug.Log("⚠ Không có dữ liệu second inventory trên Firebase");
                    
                    // 🔧 Even if no data, mark as loaded to prevent overwrites
                    inventoryLoaded = true;
                }
            });
    }

    // ============================================================
    // Serializable class cho Inventory
    // ============================================================
    [System.Serializable]
    public class SlotItemData
    {
        public int slotIndex;
        public string itemName;      // Tên item (từ ItemData.itemName)
        public int quantity;
    }

    // ============================================================
    // Serializable class cho Day and Time
    // ============================================================
    [System.Serializable]
    public class DayTimeData
    {
        public int currentDay;
        public int currentHour;
        public int currentMinute;
    }

    // Auto save farm khi thoát game
    private void OnApplicationQuit()
    {
        if (FirebaseReady)
        {
            Debug.Log("Auto SAVE farm + tiền + day/time + inventory khi thoát game");
            SaveFarmToFirebase(PlayerSession.GetCurrentUserId());
            SaveMoneyToFirebase(PlayerSession.GetCurrentUserId());
            SaveDayAndTimeToFirebase(PlayerSession.GetCurrentUserId()); // ⭐ Cũng save rain state
            
            // 🔧 Chỉ save inventory nếu đã được load từ Firebase
            // Tránh việc save inventory trống và xóa data cũ
            if (inventoryLoaded)
            {
                SaveInventoryToFirebase(PlayerSession.GetCurrentUserId());
            }
            else
            {
                Debug.LogWarning("⚠ Inventory chưa được load từ Firebase, skip save để tránh xóa data");
            }
        }
    }
    
    // 🔧 Public getter để check farm load status
    public bool IsFarmLoaded => farmLoaded;

    // ============================================================
    // INITIALIZE NEW USER DATA
    // ============================================================
    /// <summary>
    /// Reset tất cả cache khi user mới login
    /// Tránh dữ liệu của user cũ bị load cho user mới
    /// </summary>
    public void ClearCacheForNewUser()
    {
        Debug.Log("[Firebase] Clearing all cache for new user login");
        
        // Clear static cache
        CachedDayTimeData = null;
        
        // Reset instance variables
        inventoryLoaded = false;
        farmLoaded = false;
        
        // Clear FarmState
        FarmState.IsSleepTransition = false;
        FarmState.NeedSaveAfterReturn = false;
        
        // Clear RainState
        RainState.WasRaining = false;
        
        Debug.Log("[Firebase] ✅ Cache cleared");
    }

    /// <summary>
    /// Tạo dữ liệu mặc định cho user mới
    /// Gọi sau khi user đăng ký/đăng nhập thành công
    /// </summary>
    public void InitializeNewUserData(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng → KHÔNG THỂ INITIALIZE USER DATA");
            return;
        }

        Debug.Log($"[Firebase] Initializing data for new user: {userId}");

        // Dữ liệu mặc định
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            // Money
            { "Money", 1000 },

            // Day and Time
            { "DayTime/currentDay", 1 },
            { "DayTime/currentHour", 7 },
            { "DayTime/currentMinute", 0 },

            // Farm - Empty farm state
            { "Farm/farmState", "{\"crops\":[]}" },

            // Inventory - Empty inventory
            { "Inventory", "{\"slots\":[]}" }
        };

        reference.Child(userId).UpdateChildrenAsync(userData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Lỗi initialize user data: {task.Exception}");
                }
                else
                {
                    Debug.Log($"✅ Đã tạo dữ liệu mặc định cho user: {userId}");
                }
            });
    }

    /// <summary>
    /// Kiểm tra user đã có dữ liệu trong Firebase hay chưa
    /// </summary>
    public async void CheckAndInitializeUserData(string userId)
    {
        if (!FirebaseReady || reference == null)
        {
            Debug.LogError("Firebase chưa sẵn sàng");
            return;
        }

        try
        {
            DataSnapshot snapshot = await reference.Child(userId).GetValueAsync();
            
            if (!snapshot.Exists)
            {
                // User chưa có dữ liệu → Tạo dữ liệu mặc định
                Debug.Log($"[Firebase] User {userId} có dữ liệu mới, initializing...");
                InitializeNewUserData(userId);
            }
            else
            {
                Debug.Log($"[Firebase] User {userId} đã có dữ liệu, không cần initialize");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi check user data: {e.Message}");
        }
    }
}