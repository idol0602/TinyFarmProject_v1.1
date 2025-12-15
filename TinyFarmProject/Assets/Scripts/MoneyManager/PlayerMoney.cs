using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance { get; private set; }

    [Header("=== Cấu hình tiền ===")]
    [SerializeField] private int defaultMoney = 1000;

    [Header("=== UI")]
    [SerializeField] private TextMeshProUGUI moneyTextUI;

    public int CurrentMoney { get; private set; }

    public UnityEvent<int> OnMoneyChanged = new UnityEvent<int>();

    private string PLAYER_ID => PlayerSession.GetCurrentUserId();
    
    // ⚠️ Flag để tránh auto-save khi đang load từ Firebase
    private bool isLoadingFromFirebase = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentMoney = defaultMoney;
        UpdateMoneyUI();
    }

    // 🔧 Subscribe to scene load event (tương tự DayAndNightManager)
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnMoneyChanged.AddListener(AutoSave);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnMoneyChanged.RemoveListener(AutoSave);
    }

    // 🔧 Gọi khi scene load - tìm lại UI
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerMoney] Scene loaded: {scene.name}");
        StartCoroutine(RefindUIAfterSceneLoad());
    }

    // 🔧 Coroutine đợi scene setup xong rồi tìm UI
    private IEnumerator RefindUIAfterSceneLoad()
    {
        yield return null;  // Đợi 1 frame
        
        Debug.Log("[PlayerMoney] Refinding MoneyText UI in new scene...");
        moneyTextUI = null;
        RefindMoneyTextUI();
        UpdateMoneyUI();
    }

    private void Start()
    {
        Debug.Log($"[PlayerMoney] Start() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}");
        LoadMoneyFromFirebase();
    }

    private void LoadMoneyFromFirebase()
    {
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[PlayerMoney] Firebase ready, loading money from Firebase...");
            FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, ApplyLoadedMoney);
        }
        else
        {
            Debug.LogWarning("[PlayerMoney] Firebase NOT ready, retrying in 1 second...");
            Invoke(nameof(TryLoadAgain), 1f);
        }
    }

    private void TryLoadAgain()
    {
        Debug.Log($"[PlayerMoney] TryLoadAgain() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}");
        
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.Log("[PlayerMoney] Retrying Firebase load...");
            FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, ApplyLoadedMoney);
        }
        else
        {
            Debug.LogError("[PlayerMoney] Firebase still NOT ready after retry!");
        }
    }

    private void ApplyLoadedMoney(int money)
    {
        Debug.Log($"[PlayerMoney] Loaded money from Firebase: {money}đ");
        // ⚠️ BẬT auto-save TRƯỚC khi set CurrentMoney
        isLoadingFromFirebase = false;
        Debug.Log("[PlayerMoney] Firebase load completed, auto-save enabled");
        
        // Sau đó mới set CurrentMoney (nếu invoke OnMoneyChanged nó sẽ auto-save)
        CurrentMoney = money;
        UpdateMoneyUI();
        
        // ⚠️ KHÔNG invoke OnMoneyChanged lúc load Firebase
        // (để tránh auto-save ngay lập tức)
        // OnMoneyChanged.Invoke(CurrentMoney);
    }

    public bool Add(int amount)
    {
        if (amount <= 0) return false;

        CurrentMoney += amount;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
        return true;
    }

    public bool Subtract(int amount)
    {
        if (amount <= 0) return false;
        if (CurrentMoney < amount) return false;

        CurrentMoney -= amount;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
        return true;
    }

    public int GetCurrentMoney() => CurrentMoney;

    public void ResetMoney()
    {
        CurrentMoney = defaultMoney;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
    }

    // 🔧 Reload tiền khi chuyển scene (không cần gọi từ MoneyLoader)
    public void ReloadMoneyForNewScene()
    {
        Debug.Log("[PlayerMoney] ReloadMoneyForNewScene() called");
        isLoadingFromFirebase = true;
        CurrentMoney = defaultMoney;
        LoadMoneyFromFirebase();
    }

    // 🔧 Tìm lại MoneyText UI trong scene mới
    private void RefindMoneyTextUI()
    {
        Debug.Log("[PlayerMoney] RefindMoneyTextUI() started");
        
        if (moneyTextUI != null && moneyTextUI.gameObject.activeInHierarchy)
        {
            Debug.Log("[PlayerMoney] ✅ moneyTextUI reference still valid and active");
            return;
        }

        moneyTextUI = null;
        Debug.Log("[PlayerMoney] moneyTextUI is null, searching...");

        // Cách 1: Tìm theo tag "MoneyText"
        Debug.Log("[PlayerMoney] [1/5] Searching by tag 'MoneyText'...");
        GameObject moneyTextGO = GameObject.FindWithTag("MoneyText");
        if (moneyTextGO != null)
        {
            moneyTextUI = moneyTextGO.GetComponent<TextMeshProUGUI>();
            if (moneyTextUI != null)
            {
                Debug.Log("[PlayerMoney] ✅ Found MoneyText via tag");
                return;
            }
        }

        // Cách 2: Tìm Canvas rồi tìm MoneyText con
        Debug.Log("[PlayerMoney] [2/5] Searching in Canvas hierarchy...");
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[PlayerMoney] Found Canvas: {canvas.gameObject.name}");
            
            // 2a: Tìm Transform có tên "MoneyText"
            Transform moneyTextTransform = canvas.transform.Find("MoneyText");
            if (moneyTextTransform != null)
            {
                moneyTextUI = moneyTextTransform.GetComponent<TextMeshProUGUI>();
                if (moneyTextUI != null)
                {
                    Debug.Log("[PlayerMoney] ✅ Found MoneyText by name in Canvas");
                    return;
                }
            }
            
            // 2b: Search tất cả con của Canvas tìm cái chứa "Money"
            foreach (Transform child in canvas.transform)
            {
                Debug.Log($"[PlayerMoney]   Checking child: {child.name}");
                if (child.name.Contains("Money"))
                {
                    TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        moneyTextUI = text;
                        Debug.Log($"[PlayerMoney] ✅ Found MoneyText in child: {child.name}");
                        return;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerMoney] No Canvas found in scene!");
        }

        // Cách 3: Search tất cả TextMeshProUGUI trong scene
        Debug.Log("[PlayerMoney] [3/5] Searching all TextMeshProUGUI in scene...");
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"[PlayerMoney] Found {allTexts.Length} TextMeshProUGUI objects");
        
        foreach (var text in allTexts)
        {
            Debug.Log($"[PlayerMoney]   - {text.gameObject.name}: '{text.text}'");
            if (text.name.Contains("Money"))
            {
                moneyTextUI = text;
                Debug.Log($"[PlayerMoney] ✅ Found MoneyText by name: {text.name}");
                return;
            }
        }
        
        // Cách 4: Tìm cái có text chứa "đ"
        Debug.Log("[PlayerMoney] [4/5] Searching by currency symbol...");
        foreach (var text in allTexts)
        {
            if (text.text.Contains("đ"))
            {
                moneyTextUI = text;
                Debug.Log($"[PlayerMoney] ✅ Found MoneyText with currency: {text.gameObject.name}");
                return;
            }
        }

        Debug.LogWarning("[PlayerMoney] ❌ Could NOT find MoneyText UI after ALL attempts!");
    }

    private void UpdateMoneyUI()
    {
        if (moneyTextUI == null)
        {
            RefindMoneyTextUI();
        }

        if (moneyTextUI != null)
        {
            moneyTextUI.text = CurrentMoney.ToString("N0") + "đ";
            Debug.Log($"[PlayerMoney] ✓ Updated UI: {CurrentMoney:N0}đ");
        }
        else
        {
            Debug.LogWarning($"[PlayerMoney] ⚠️ moneyTextUI is NULL! Money value: {CurrentMoney:N0}đ (will update when UI found)");
        }
    }

    // TỰ ĐỘNG LƯU MỖI KHI TIỀN THAY ĐỔI
    // (OnEnable/OnDisable đã moved to top với OnSceneLoaded)

    private void AutoSave(int _)
    {
        if (isLoadingFromFirebase)
        {
            Debug.Log("[PlayerMoney] Skipping auto-save (still loading from Firebase)");
            return;
        }
        
        Debug.Log("[PlayerMoney] Auto-saving money to Firebase...");
        FirebaseDatabaseManager.Instance?.SaveMoneyToFirebase(PLAYER_ID);
    }
}