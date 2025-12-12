using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance { get; private set; }

    [Header("=== Cấu hình tiền ===")]
    [SerializeField] private int defaultMoney = 1000;

    [Header("=== UI")]
    [SerializeField] private TextMeshProUGUI moneyTextUI;

    public int CurrentMoney { get; private set; }

    public UnityEvent<int> OnMoneyChanged = new UnityEvent<int>();

    private const string PLAYER_ID = "Player1";
    
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

    private void Start()
    {
        Debug.Log($"[PlayerMoney] Start() called. FirebaseReady: {FirebaseDatabaseManager.FirebaseReady}");
        
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

    private void UpdateMoneyUI()
    {
        if (moneyTextUI != null)
            moneyTextUI.text = CurrentMoney.ToString("N0") + "đ";
    }

    // TỰ ĐỘNG LƯU MỖI KHI TIỀN THAY ĐỔI
    private void OnEnable() => OnMoneyChanged.AddListener(AutoSave);
    private void OnDisable() => OnMoneyChanged.RemoveListener(AutoSave);

    private void AutoSave(int _)
    {
        // ⚠️ CHỈ save nếu đã load xong từ Firebase
        if (isLoadingFromFirebase)
        {
            Debug.Log("[PlayerMoney] Skipping auto-save (still loading from Firebase)");
            return;
        }
        
        Debug.Log("[PlayerMoney] Auto-saving money to Firebase...");
        FirebaseDatabaseManager.Instance?.SaveMoneyToFirebase(PLAYER_ID);
    }
}