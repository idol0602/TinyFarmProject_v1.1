using UnityEngine;

namespace MapSummer
{
    [DefaultExecutionOrder(50)]
    public class Crop : MonoBehaviour
    {
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

        // ============================================================
        //  ĐĂNG KÝ EVENT — chỉ chạy 1 lần / crop
        // ============================================================
        private void Awake()
        {
            DayAndNightEvents.OnNewDay -= HandleNewDay;
            DayAndNightEvents.OnNewDay += HandleNewDay;

            // ⭐ LẮNG NGHE MƯA
            RainManager.OnRainChanged -= HandleRainChanged;
            RainManager.OnRainChanged += HandleRainChanged;

        }

        private void OnDestroy()
        {
            DayAndNightEvents.OnNewDay -= HandleNewDay;
            RainManager.OnRainChanged -= HandleRainChanged;

        }

        // ============================================================
        //  KHỞI TẠO CÂY
        // ============================================================
        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            clock = DayAndNightManager.Instance;

            if (!isLoadedFromSave)
            {
                CropID = System.Guid.NewGuid().ToString();
                currentStage = 0;

                int today = clock.GetCurrentDay();

                // ⭐ FIX QUAN TRỌNG
                if (RainManager.Instance != null && RainManager.Instance.isRaining)
                {
                    // 🌧️ Trồng lúc mưa → coi như đã tưới
                    lastWaterDay = today;
                    isWateredToday = true;
                    Debug.Log($"🌧️ {cropType} trồng lúc mưa → auto tưới");
                }
                else
                {
                    lastWaterDay = today;
                    isWateredToday = false;
                }

                sr.sprite = stages[currentStage];

                SpawnIcons();
                UpdateIcons();
            }
        }


        // ============================================================
        //  ICON
        // ============================================================
        private void SpawnIcons()
        {
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

            // ⭐ CHỈ PHỤ THUỘC TRẠNG THÁI CÂY
            waterIcon?.SetActive(!isWateredToday);
            harvestIcon?.SetActive(false);
        }



        // ============================================================
        //  SỰ KIỆN NGÀY MỚI
        // ============================================================
        private void HandleNewDay(int newDay)
        {
            if (isDead) return;

            // ❌ Không tưới hôm qua → chết
            if (!isWateredToday && lastWaterDay < newDay - 1)
            {
                Die();
                return;
            }

            // 🌱 Có tưới → lớn
            if (isWateredToday)
            {
                Grow();
                lastWaterDay = newDay - 1;
            }

            isWateredToday = false;
            UpdateIcons();
        }



        // ============================================================
        //  TƯỚI / LỚN / CHẾT / THU HOẠCH
        // ============================================================
        public void Water()
        {
            isWateredToday = true;
            lastWaterDay = clock.GetCurrentDay();
            UpdateIcons();
        }

        private void Grow()
        {
            if (currentStage < stages.Length - 1)
                currentStage++;

            sr.sprite = stages[currentStage];
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
            Destroy(gameObject);
        }

        // ============================================================
        //  LOAD TỪ FIREBASE
        // ============================================================
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

            SpawnIcons();
            UpdateIcons();
        }
        private void HandleRainChanged(bool isRaining)
        {
            // 🌧️ Nếu đang mưa → coi như đã tưới trong ngày
            if (isRaining && !isDead)
            {
                isWateredToday = true;
            }

            UpdateIcons();
        }

    }

}
