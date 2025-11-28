using UnityEngine;

public class Crop : MonoBehaviour
{
    [Header("Stages")]
    public Sprite[] stages;

    [Header("Mỗi stage cần bao nhiêu GIỜ game")]
    public float hoursPerStage = 1f;

    [Header("ICON NƯỚC")]
    public GameObject waterIconPrefab;
    private GameObject waterIcon;

    private SpriteRenderer sr;
    private DayAndNightManager clock;
    private int currentStage = 0;
    private float lastHourCheck;

    // Trạng thái tưới của CHU KỲ hiện tại (1 giờ)
    private bool isWatered = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        clock = FindFirstObjectByType<DayAndNightManager>();

        sr.sprite = stages[0];
        lastHourCheck = GetHour();

        // 🧹 XÓA ICON THỪA CÓ SẴN TRONG PREFAB
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("watericon"))
            {
                Destroy(child.gameObject);
            }
        }

        // Sau đó TẠO ICON ĐÚNG
        if (waterIconPrefab != null)
        {
            waterIcon = Instantiate(waterIconPrefab, transform);
            waterIcon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            waterIcon.SetActive(true);
        }
    }

    void Update()
    {
        float hour = GetHour();

        // Qua 1 giờ → xử lý stage
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
        // ❌ Nếu CHƯA TƯỚI trong 1 giờ → KHÔNG lớn
        if (!isWatered)
        {
            if (waterIcon != null)
                waterIcon.SetActive(true); // Hiện để nhắc tưới

            return;
        }

        // ✔ Đã tưới → LỚN
        Grow();

        // Reset cho CHU KỲ 1 GIỜ tiếp theo
        isWatered = false;

        // BẬT lại icon để nhắc tưới cho giờ kế tiếp
        if (waterIcon != null)
            waterIcon.SetActive(true);
    }

    void Grow()
    {
        if (currentStage < stages.Length - 1)
        {
            currentStage++;
            sr.sprite = stages[currentStage];
        }
    }

    // 🌧 Gọi từ Player
    public void Water()
    {
        isWatered = true;

        if (waterIcon != null)
        {
            waterIcon.SetActive(false);

            // TẮT TẤT CẢ CON TRONG ICON
            foreach (Transform child in waterIcon.transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        Debug.Log("💧 Đã tưới — icon tắt");
    }

}
