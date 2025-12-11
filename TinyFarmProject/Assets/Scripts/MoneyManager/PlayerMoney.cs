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
        if (FirebaseDatabaseManager.FirebaseReady)
        {
            FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, ApplyLoadedMoney);
        }
        else
        {
            Invoke(nameof(TryLoadAgain), 1f);
        }
    }

    private void TryLoadAgain()
    {
        if (FirebaseDatabaseManager.FirebaseReady)
            FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, ApplyLoadedMoney);
    }

    private void ApplyLoadedMoney(int money)
    {
        CurrentMoney = money;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
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
        FirebaseDatabaseManager.Instance?.SaveMoneyToFirebase(PLAYER_ID);
    }
}