using UnityEngine;

public class Crop : MonoBehaviour
{
    [Header("Stages cây (0 → cuối)")]
    public Sprite[] stages;

    [Header("Icon nước")]
    public GameObject waterIconPrefab;
    private GameObject waterIcon;

    [Header("Icon thu hoạch / dọn cây")]
    public GameObject harvestIconPrefab;
    private GameObject harvestIcon;

    [Header("Sprite cây chết")]
    public Sprite deadSprite;

    private SpriteRenderer sr;
    private DayAndNightManager clock;

    // ===== TRẠNG THÁI CÂY =====
    private int currentStage = 0;
    private bool isDead = false;

    // ===== LOGIC NGÀY TƯỚI =====
    private int lastWaterDay = 0;
    private bool isWateredToday = false;
    private int plantedDay = 0;

    // ===== GIỜ TƯỚI =====
    public int morningHour = 6;
    public int eveningHour = 18;

    // ===== CROP ID =====
    public string CropID { get; private set; }

    // getter để save
    public int CurrentStage => currentStage;
    public bool IsDead => isDead;
    public int LastWaterDay => lastWaterDay;
    public bool IsWateredToday => isWateredToday;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        clock = DayAndNightManager.Instance;

        // tạo ID duy nhất
        CropID = System.Guid.NewGuid().ToString();

        sr.sprite = stages[0];

        plantedDay = clock.GetCurrentDay();
        lastWaterDay = plantedDay;  // ngày trồng xem như đã tưới

        SpawnIcons();

        DayAndNightEvents.OnNewDay += HandleNewDay;

        // lưu vào danh sách cây
        CropSaveSystem.AddCrop(this);
    }

    private void OnDestroy()
    {
        DayAndNightEvents.OnNewDay -= HandleNewDay;
    }

    // ========================== SPAWN ICON ==========================
    private void SpawnIcons()
    {
        if (waterIconPrefab != null)
        {
            waterIcon = Instantiate(waterIconPrefab, transform);
            waterIcon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            waterIcon.SetActive(true);
        }

        if (harvestIconPrefab != null)
        {
            harvestIcon = Instantiate(harvestIconPrefab, transform);
            harvestIcon.transform.localPosition = new Vector3(0f, 1f, 0f);
            harvestIcon.SetActive(false);
        }
    }

    // ========================== NGÀY MỚI ==========================
    private void HandleNewDay(int newDay)
    {
        if (isDead) return;

        int yesterday = newDay - 1;

        // ❌ hôm qua không tưới → chết
        if (lastWaterDay < yesterday)
        {
            Die();
            return;
        }

        // ✔ nếu hôm qua có tưới → lớn lên
        if (isWateredToday)
            Grow();

        // reset trạng thái ngày mới
        isWateredToday = false;

        if (!isDead && currentStage < stages.Length - 1)
            waterIcon.SetActive(true);
    }

    // ========================== TƯỚI ==========================
    public void Water()
    {
        if (isDead) return;

        int hour = clock.GetCurrentHour();

        if (hour < morningHour || hour > eveningHour)
        {
            Debug.Log("🌙 Ban đêm không thể tưới.");
            return;
        }

        isWateredToday = true;
        lastWaterDay = clock.GetCurrentDay();

        waterIcon.SetActive(false);
        Debug.Log("💧 Đã tưới!");
    }

    // ========================== LỚN LÊN ==========================
    private void Grow()
    {
        if (currentStage < stages.Length - 1)
        {
            currentStage++;
            sr.sprite = stages[currentStage];
        }

        if (currentStage == stages.Length - 1)
        {
            harvestIcon.SetActive(true);
            waterIcon.SetActive(false);
        }
        else
        {
            waterIcon.SetActive(true);
        }
    }

    // ========================== CHẾT ==========================
    private void Die()
    {
        isDead = true;

        sr.sprite = deadSprite;

        waterIcon.SetActive(false);
        harvestIcon.SetActive(true);

        CropSaveSystem.RemoveCrop(this);

        Debug.Log("💀 Cây chết vì không tưới hôm qua!");
    }

    // ========================== THU HOẠCH ==========================
    public void Harvest()
    {
        CropSaveSystem.RemoveCrop(this);
        Destroy(gameObject);

        Debug.Log("🌾 Thu hoạch / Dọn cây!");
    }

    // ========================== LOAD LẠI ==========================
    public void LoadFromData(CropData d)
    {
        // ====== GÁN LẠI DỮ LIỆU ======
        this.CropID = d.cropID;
        this.currentStage = d.stage;
        this.isDead = d.isDead;
        this.lastWaterDay = d.lastWaterDay;
        this.isWateredToday = d.isWateredToday;

        // ====== GÁN LẠI REFERENCE QUAN TRỌNG ======
        clock = DayAndNightManager.Instance;
        sr = GetComponent<SpriteRenderer>();

        // ====== ĐĂNG KÝ LẠI SỰ KIỆN NGÀY MỚI (BẮT BUỘC) ======
        DayAndNightEvents.OnNewDay += HandleNewDay;

        // ====== CẬP NHẬT SPRITE ======
        sr.sprite = isDead ? deadSprite : stages[currentStage];

        // ====== XÓA ICON CŨ ======
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // ====== TẠO LẠI ICON ======
        waterIcon = Instantiate(waterIconPrefab, transform);
        waterIcon.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        harvestIcon = Instantiate(harvestIconPrefab, transform);
        harvestIcon.transform.localPosition = new Vector3(0f, 1f, 0f);

        // ====== SET ICON ĐÚNG TRẠNG THÁI ======
        if (isDead)
        {
            waterIcon.SetActive(false);
            harvestIcon.SetActive(true);
            return;
        }

        if (currentStage == stages.Length - 1)
        {
            waterIcon.SetActive(false);
            harvestIcon.SetActive(true);
            return;
        }

        // Nếu hôm nay đã tưới rồi → tắt icon nước
        waterIcon.SetActive(!isWateredToday);
        harvestIcon.SetActive(false);
    }

}
