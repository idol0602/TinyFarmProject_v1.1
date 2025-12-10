using UnityEngine;

namespace MapSummer

{
    [DefaultExecutionOrder(10)]

    public class Crop : MonoBehaviour
    {
        // ⭐ LOẠI CÂY (Corn, Chili, Tomato...)
        public string cropType;

        [Header("Sprites theo từng Stage")]
        public Sprite[] stages;

        [Header("Icon")]
        public GameObject waterIconPrefab;
        public GameObject harvestIconPrefab;
        public Sprite deadSprite;

        private GameObject waterIcon;
        private GameObject harvestIcon;

        private SpriteRenderer sr;
        private DayAndNightManager clock;

        // Stage / Life
        private int currentStage = 0;
        private bool isDead = false;
        private int lastWaterDay = 0;
        private bool isWateredToday = false;

        public string CropID { get; private set; }
        public int CurrentStage => currentStage;
        public bool IsDead => isDead;
        public int LastWaterDay => lastWaterDay;
        public bool IsWateredToday => isWateredToday;

        private bool isLoadedFromSave = false;
        public static int LastNewDayEvent = -1;


        private void OnEnable()
        {
            Debug.Log($"[CROP ENABLE] {cropType} ENABLE");

            DayAndNightEvents.OnNewDay += HandleNewDay;

            // ⭐ Nếu cây spawn sau khi DayManager bắn event → tự xử lý
            int today = DayAndNightManager.LastNewDayEvent;
            if (today != -1 && clock != null)
            {
                if (today == clock.GetCurrentDay())
                {
                    Debug.Log($"[CROP] {cropType} missed event → replay OnNewDay({today})");
                    HandleNewDay(today);
                }
            }
        }

        private void OnDisable()
        {
            DayAndNightEvents.OnNewDay -= HandleNewDay;
        }



       

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            clock = DayAndNightManager.Instance;

            if (!isLoadedFromSave)
            {
                CropID = System.Guid.NewGuid().ToString();
                currentStage = 0;

                lastWaterDay = clock.GetCurrentDay();
                isWateredToday = false;

                sr.sprite = stages[currentStage];

                SpawnIcons();
                UpdateIcons();
            }
        }


        private void OnDestroy()
        {
            DayAndNightEvents.OnNewDay -= HandleNewDay;
        }

        // ============================================
        // ICON
        // ============================================
        private void SpawnIcons()
        {
            // Xóa icon cũ trước khi spawn icon mới
            if (waterIcon != null) Destroy(waterIcon);
            if (harvestIcon != null) Destroy(harvestIcon);

            waterIcon = Instantiate(waterIconPrefab, transform);
            waterIcon.transform.localPosition = new Vector3(0, 0.8f, 0);

            harvestIcon = Instantiate(harvestIconPrefab, transform);
            harvestIcon.transform.localPosition = new Vector3(0, 1.2f, 0);
        }

        private void UpdateIcons()
        {
            if (isDead)
            {
                waterIcon?.SetActive(false);
                harvestIcon?.SetActive(true);
                return;
            }

            if (currentStage == stages.Length - 1)
            {
                harvestIcon?.SetActive(true);
                waterIcon?.SetActive(false);
                return;
            }

            waterIcon?.SetActive(!isWateredToday);
            harvestIcon?.SetActive(false);
        }

        // ============================================
        // NGÀY MỚI
        // ============================================
        private void HandleNewDay(int newDay)
        {
            Debug.Log($"🌱 [CROP EVENT] {cropType} nhận OnNewDay({newDay}) | " +
                      $"stage={currentStage} | wateredToday={isWateredToday} | lastWaterDay={lastWaterDay}");

            if (isDead)
            {
                Debug.Log($"💀 [CROP] {cropType} chết → bỏ qua");
                return;
            }

            // kiểm tra bỏ đói 1 ngày
            if (lastWaterDay < newDay - 1)
            {
                Debug.Log($"💀 [CROP] {cropType} chết vì không tưới hôm qua (lastWaterDay={lastWaterDay}, newDay-1={newDay - 1})");
                Die();
                return;
            }

            if (isWateredToday)
            {
                Debug.Log($"⬆ [CROP] {cropType} LỚN LÊN! Stage {currentStage} → {currentStage + 1}");
                Grow();
            }
            else
            {
                Debug.Log($"⚠ [CROP] {cropType} KHÔNG LỚN vì hôm nay không tưới");
            }

            // reset
            isWateredToday = false;
            UpdateIcons();
        }


        public void Water()
        {
            isWateredToday = true;
            lastWaterDay = clock.GetCurrentDay();
            UpdateIcons();
        }

        private void Grow()
        {
            if (currentStage < stages.Length - 1)
            {
                currentStage++;
                sr.sprite = stages[currentStage];
            }

            UpdateIcons();
        }

        private void Die()
        {
            isDead = true;
            sr.sprite = deadSprite;
            UpdateIcons();
        }

        public void Harvest()
        {
            Debug.Log($"🌾 Thu hoạch cây {cropType} thành công!");
            Destroy(gameObject);
        }

        // ============================================
        // LOAD DỮ LIỆU TỪ FIREBASE
        // ============================================
        public void LoadFromData(CropData d)
        {
            isLoadedFromSave = true;

            CropID = d.cropID;
            cropType = d.cropType;

            currentStage = d.stage;
            isDead = d.isDead;
            lastWaterDay = d.lastWaterDay;
            isWateredToday = d.isWateredToday;

            sr = GetComponent<SpriteRenderer>();
            sr.sprite = isDead ? deadSprite : stages[currentStage];

            // Spawn icon mới (không cần tag)
            SpawnIcons();
            UpdateIcons();

            // Reset collider nếu có
            Collider2D col = GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
                col.enabled = true;
            }
        }
    }
}
