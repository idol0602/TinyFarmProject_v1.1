using UnityEngine;

public class Crop : MonoBehaviour
{
    [Header("Stages (0 → cuối)")]
    public Sprite[] stages;

    [Header("Thời gian (GIỜ GAME) mỗi stage")]
    public float hoursPerStage = 3f;

    [Header("Icon nước")]
    public GameObject waterIconPrefab;
    private GameObject waterIcon;

    [Header("Icon thu hoạch / dọn cây")]
    public GameObject harvestIconPrefab;
    private GameObject harvestIcon;

    [Header("Cây chết")]
    public Sprite deadSprite;
    public float maxNoWaterHours = 5f;

    private SpriteRenderer sr;
    private DayAndNightManager clock;

    private int currentStage = 0;
    private float lastHourCheck = 0f;
    private bool isWatered = false;

    private float lastWaterHour = 0f;
    private bool isDead = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        clock = FindFirstObjectByType<DayAndNightManager>();
        sr.sprite = stages[0];

        lastHourCheck = GetHour();
        lastWaterHour = GetHour();

        // Xóa icon cũ
        foreach (Transform child in transform)
        {
            string n = child.name.ToLower();
            if (n.Contains("watericon") || n.Contains("harvesticon"))
                Destroy(child.gameObject);
        }

        // Icon nước
        if (waterIconPrefab != null)
        {
            waterIcon = Instantiate(waterIconPrefab, transform);
            waterIcon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            waterIcon.SetActive(true);
        }

        // Icon thu hoạch / dọn
        if (harvestIconPrefab != null)
        {
            harvestIcon = Instantiate(harvestIconPrefab, transform);
            harvestIcon.transform.localPosition = new Vector3(0f, 1f, 0f);
            harvestIcon.SetActive(false);
        }
    }

    void Update()
    {
        float hour = GetHour();

        // ====== 1. CHẾT CÂY ======
        if (!isDead && hour - lastWaterHour >= maxNoWaterHours)
        {
            Die();
            return;
        }

        // Nếu cây CHẾT → hiện icon gặt để dọn
        if (isDead)
        {
            if (harvestIcon != null) harvestIcon.SetActive(true);
            return;
        }

        // ====== 2. Stage cuối → hiện icon thu hoạch ======
        if (currentStage == stages.Length - 1)
        {
            if (harvestIcon != null) harvestIcon.SetActive(true);
            if (waterIcon != null) waterIcon.SetActive(false);
            return;
        }

        // ====== 3. Lớn lên theo giờ ======
        if (hour - lastHourCheck >= hoursPerStage)
        {
            ProcessStage();
            lastHourCheck = hour;
        }
    }

    float GetHour()
    {
        return clock.GetCurrentGameSeconds() / 3600f;
    }

    void ProcessStage()
    {
        if (!isWatered)
        {
            if (waterIcon != null) waterIcon.SetActive(true);
            return;
        }

        Grow();
        isWatered = false;

        if (currentStage < stages.Length - 1)
        {
            if (waterIcon != null) waterIcon.SetActive(true);
        }
        else
        {
            // Stage cuối → icon thu hoạch
            if (harvestIcon != null) harvestIcon.SetActive(true);
        }
    }

    void Grow()
    {
        if (currentStage < stages.Length - 1)
        {
            currentStage++;
            sr.sprite = stages[currentStage];
        }
    }

    public void Water()
    {
        isWatered = true;
        lastWaterHour = GetHour(); // reset giờ tưới

        if (waterIcon != null) waterIcon.SetActive(false);

        Debug.Log("💧 Đã tưới");
    }

    // 🌾 Thu hoạch hoặc dọn cây chết
    public void Harvest()
    {
        Debug.Log("🌾 Thu hoạch / Dọn cây!");

        Destroy(gameObject); // xóa cây
    }

    // 💀 Cây chết
    void Die()
    {
        isDead = true;

        Debug.Log("💀 Cây đã chết!");

        // đổi sprite chết
        if (deadSprite != null)
            sr.sprite = deadSprite;

        // tắt icon nước
        if (waterIcon != null) waterIcon.SetActive(false);

        // bật icon gặt để dọn
        if (harvestIcon != null) harvestIcon.SetActive(true);
    }
}
