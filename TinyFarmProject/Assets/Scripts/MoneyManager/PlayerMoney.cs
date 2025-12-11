using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance { get; private set; }

    [Header("=== Cấu hình tiền ===")]
    [SerializeField] private int defaultMoney = 1000;

    [Header("=== UI (kéo TextMeshPro vào đây) ===")]
    [SerializeField] private TextMeshProUGUI moneyTextUI;

    public int CurrentMoney { get; private set; }

    public UnityEvent<int> OnMoneyChanged = new UnityEvent<int>();

    private const string PLAYER_ID = "Player1";

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
        // Tự động load tiền khi game chạy
        if (FirebaseDatabaseManager.Instance != null && FirebaseDatabaseManager.FirebaseReady)
        {
            FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, ApplyLoadedMoney);
        }
        else
        {
            // Nếu Firebase chưa sẵn, đợi một chút rồi thử lại
            Invoke(nameof(TryLoadMoney), 1f);
        }
    }

    private void TryLoadMoney()
    {
        if (FirebaseDatabaseManager.FirebaseReady)
            FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, ApplyLoadedMoney);
    }

    // Hàm này được gọi từ Firebase khi load xong
    private void ApplyLoadedMoney(int loadedMoney)
    {
        CurrentMoney = loadedMoney;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
        Debug.Log($"ĐÃ LOAD TIỀN TỪ FIREBASE: {CurrentMoney:N0}đ");
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
        if (CurrentMoney < amount)
        {
            Debug.LogWarning("Không đủ tiền!");
            return false;
        }
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
        else if (Application.isPlaying)
            Debug.Log($"Tiền hiện tại: {CurrentMoney:N0}đ (chưa gán UI)");
    }

    // TỰ ĐỘNG LƯU KHI TIỀN THAY ĐỔI
    private void OnEnable() => OnMoneyChanged.AddListener(SaveOnChange);
    private void OnDisable() => OnMoneyChanged.RemoveListener(SaveOnChange);

    private void SaveOnChange(int _) => FirebaseDatabaseManager.Instance?.SaveMoneyToFirebase(PLAYER_ID);

    private void OnApplicationQuit() => FirebaseDatabaseManager.Instance?.SaveMoneyToFirebase(PLAYER_ID);

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}