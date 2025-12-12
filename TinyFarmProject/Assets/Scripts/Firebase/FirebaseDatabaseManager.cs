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
    
    // 🔧 Track xem inventory đã được load từ Firebase hay chưa
    private bool inventoryLoaded = false;

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
        
        Debug.Log($"[Firebase] Saving money: {money:N0}đ → /Money/{userId}");

        reference.Child("Money").Child(userId)
            .SetValueAsync(money)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Lỗi SAVE tiền: " + task.Exception);
                else
                    Debug.Log($"✓ Đã lưu tiền: {money:N0}đ → /Money/{userId}");
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

        Debug.Log($"[Firebase] Loading money from /Money/{userId}...");
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

    // Auto save farm khi thoát game
    private void OnApplicationQuit()
    {
        if (FirebaseReady)
        {
            Debug.Log("Auto SAVE farm + tiền + inventory khi thoát game");
            SaveFarmToFirebase("Player1");
            SaveMoneyToFirebase("Player1");
            
            // 🔧 Chỉ save inventory nếu đã được load từ Firebase
            // Tránh việc save inventory trống và xóa data cũ
            if (inventoryLoaded)
            {
                SaveInventoryToFirebase("Player1");
            }
            else
            {
                Debug.LogWarning("⚠ Inventory chưa được load từ Firebase, skip save để tránh xóa data");
            }
        }
    }
}