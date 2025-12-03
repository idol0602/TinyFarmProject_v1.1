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

    [Header("Cây chết")]
    public Sprite deadSprite;

    private SpriteRenderer sr;
    private DayAndNightManager clock;

    // ======= Trạng thái tăng trưởng =======
    private int currentStage = 0;
    private bool isDead = false;

    // ======= Logic tưới theo NGÀY =======
    private int lastWaterDay = 0;
    private bool isWateredToday = false;
    private int plantedDay = 0;

    // ======= Giới hạn tưới trong ngày =======
    public int morningHour = 6;
    public int eveningHour = 18;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        clock = DayAndNightManager.Instance;

        sr.sprite = stages[0];

        // Ngày trồng → xem như đã tưới để không chết ngay lập tức
        plantedDay = clock.GetCurrentDay();
        lastWaterDay = plantedDay;

        SpawnIcons();

        // Đăng ký sự kiện ngày mới
        DayAndNightEvents.OnNewDay += HandleNewDay;
    }

    private void OnDestroy()
    {
        DayAndNightEvents.OnNewDay -= HandleNewDay;
    }

    private void SpawnIcons()
    {
        // Icon nước
        if (waterIconPrefab != null)
        {
            waterIcon = Instantiate(waterIconPrefab, transform);
            waterIcon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            waterIcon.name = "WaterIcon";
            waterIcon.SetActive(true);
        }

        // Icon thu hoạch
        if (harvestIconPrefab != null)
        {
            harvestIcon = Instantiate(harvestIconPrefab, transform);
            harvestIcon.transform.localPosition = new Vector3(0f, 1f, 0f);
            harvestIcon.name = "HarvestIcon";
            harvestIcon.SetActive(false);
        }
    }

    // ========================================================
    // ===============   NGÀY MỚI   ===========================
    // ========================================================

    private void HandleNewDay(int newDay)
    {
        if (isDead) return;

        int yesterday = newDay - 1;

        // ❌ Nếu hôm qua không tưới → cây chết
        if (lastWaterDay < yesterday)
        {
            Die();
            return;
        }

        // ✔ Nếu hôm qua có tưới → cây lớn lên 1 stage
        if (isWateredToday)
        {
            Grow();
        }

        // Reset trạng thái tưới cho NGÀY HÔM NAY
        isWateredToday = false;

        // Nếu chưa trưởng thành → icon nước bật lại
        if (!isDead && currentStage < stages.Length - 1)
        {
            waterIcon.SetActive(true);
        }
    }

    // ========================================================
    // =====================  TƯỚI NƯỚC  =======================
    // ========================================================

    public void Water()
    {
        if (isDead) return;

        int hour = clock.GetCurrentHour();

        // Ban đêm không được tưới
        if (hour < morningHour || hour > eveningHour)
        {
            Debug.Log("🌙 Ban đêm không được tưới!");
            return;
        }

        int today = clock.GetCurrentDay();

        lastWaterDay = today;
        isWateredToday = true;

        if (waterIcon != null)
            waterIcon.SetActive(false);

        Debug.Log($"💧 Tưới cây thành công (Ngày {today})");
    }

    // ========================================================
    // =====================  LỚN LÊN  =========================
    // ========================================================

    private void Grow()
    {
        if (currentStage < stages.Length - 1)
        {
            currentStage++;
            sr.sprite = stages[currentStage];

            Debug.Log($"🌱 Cây lớn lên stage {currentStage}");
        }

        // Stage cuối → icon thu hoạch bật
        if (currentStage == stages.Length - 1)
        {
            harvestIcon.SetActive(true);
            waterIcon.SetActive(false);
        }
        else
        {
            // Stage mới → cần tưới tiếp
            waterIcon.SetActive(true);
        }
    }

    // ========================================================
    // =====================  CHẾT  ============================
    // ========================================================

    private void Die()
    {
        isDead = true;

        if (deadSprite != null)
            sr.sprite = deadSprite;

        waterIcon.SetActive(false);
        harvestIcon.SetActive(true);

        Debug.Log("💀 Cây chết vì hôm qua không tưới!");
    }

    // ========================================================
    // =====================  THU HOẠCH  =======================
    // ========================================================

    public void Harvest()
    {
        Debug.Log("🌾 Thu hoạch / Dọn cây!");
        Destroy(gameObject);
    }
}
