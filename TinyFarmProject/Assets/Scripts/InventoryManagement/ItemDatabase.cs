using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    private Dictionary<string, ItemData> itemCache = new Dictionary<string, ItemData>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeDatabase();
    }

    /// <summary>
    /// Load tất cả ItemData assets từ Resources folder
    /// </summary>
    private void InitializeDatabase()
    {
        if (isInitialized)
            return;

        Debug.Log("[ItemDatabase] Initializing ItemDatabase...");

        // Tìm tất cả ItemData assets trong Resources folder
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        
        Debug.Log($"[ItemDatabase] Found {allItems.Length} ItemData assets");

        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                itemCache[item.itemName] = item;
                Debug.Log($"[ItemDatabase] ✅ Cached: {item.itemName}");
            }
        }

        isInitialized = true;
        Debug.Log($"[ItemDatabase] Database initialized with {itemCache.Count} items");
    }

    /// <summary>
    /// Get ItemData bằng tên item
    /// </summary>
    public ItemData GetItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("[ItemDatabase] itemName is null or empty");
            return null;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[ItemDatabase] Database not initialized yet!");
            InitializeDatabase();
        }

        if (itemCache.TryGetValue(itemName, out ItemData item))
        {
            Debug.Log($"[ItemDatabase] ✅ Found item: {itemName}");
            return item;
        }

        Debug.LogWarning($"[ItemDatabase] ❌ Item NOT found: {itemName}");
        return null;
    }

    /// <summary>
    /// Get ItemData bằng ItemSubtype
    /// </summary>
    public ItemData GetItemBySubtype(ItemSubtype subtype)
    {
        if (!isInitialized)
            InitializeDatabase();

        foreach (var kvp in itemCache)
        {
            if (kvp.Value != null && kvp.Value.itemSubtype == subtype)
            {
                Debug.Log($"[ItemDatabase] ✅ Found item with subtype {subtype}: {kvp.Key}");
                return kvp.Value;
            }
        }

        Debug.LogWarning($"[ItemDatabase] ❌ No item found with subtype: {subtype}");
        return null;
    }

    /// <summary>
    /// Get tất cả items trong database
    /// </summary>
    public ItemData[] GetAllItems()
    {
        if (!isInitialized)
            InitializeDatabase();

        ItemData[] items = new ItemData[itemCache.Count];
        itemCache.Values.CopyTo(items, 0);
        return items;
    }

    /// <summary>
    /// Check xem item có trong database không
    /// </summary>
    public bool ContainsItem(string itemName)
    {
        if (!isInitialized)
            InitializeDatabase();

        return itemCache.ContainsKey(itemName);
    }

    /// <summary>
    /// Print tất cả items (debug)
    /// </summary>
    public void PrintAllItems()
    {
        if (!isInitialized)
            InitializeDatabase();

        Debug.Log($"[ItemDatabase] Total items: {itemCache.Count}");
        foreach (var kvp in itemCache)
        {
            Debug.Log($"  - {kvp.Key}");
        }
    }
}
