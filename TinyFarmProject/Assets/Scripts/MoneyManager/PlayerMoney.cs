using UnityEngine;
using UnityEngine.UI;        // Nếu dùng Text cũ
using TMPro;                  // Dùng TextMeshPro
using UnityEngine.Events;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance { get; private set; }

    [Header("=== Cấu hình tiền ===")]
    [SerializeField] private int defaultMoney = 1000;

    [Header("=== UI (kéo Text vào đây) ===")]
    [SerializeField] private TextMeshProUGUI moneyTextUI;        // Kéo TextMeshPro ở đây
    // [SerializeField] private Text moneyTextUI;                // Nếu dùng Text cũ thì bỏ comment dòng này

    // Giá trị tiền hiện tại (public readonly)
    public int CurrentMoney { get; private set; }

    // Event công khai cho các hệ thống khác muốn nghe (Inventory, Shop, v.v.)
    public UnityEvent<int> OnMoneyChanged = new UnityEvent<int>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo tiền
        CurrentMoney = defaultMoney;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
    }

    // Gọi từ bất kỳ đâu để thêm tiền
    public bool Add(int amount)
    {
        if (amount <= 0) return false;

        CurrentMoney += amount;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
        return true;
    }

    // Gọi từ bất kỳ đâu để trừ tiền (có kiểm tra đủ)
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

    // Lấy số tiền hiện tại
    public int GetCurrentMoney() => CurrentMoney;

    // Reset về mặc định (dùng khi New Game)
    public void ResetMoney()
    {
        CurrentMoney = defaultMoney;
        UpdateMoneyUI();
        OnMoneyChanged.Invoke(CurrentMoney);
    }

    // Hàm duy nhất chịu trách nhiệm cập nhật UI (gọi mỗi khi tiền thay đổi)
    private void UpdateMoneyUI()
    {
        if (moneyTextUI != null)
        {
            moneyTextUI.text = CurrentMoney.ToString("N0") + "đ";
        }
        // Nếu chưa gán Text → vẫn log để dev biết
        else if (Application.isPlaying)
        {
            Debug.Log($"Tiền hiện tại: {CurrentMoney:N0}đ (chưa gán UI Text)");
        }
    }

    // Bonus: Dọn dẹp singleton khi destroy
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}