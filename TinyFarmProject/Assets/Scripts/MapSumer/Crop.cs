using UnityEngine;

namespace MapSummer
{
    [DefaultExecutionOrder(10)]
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
        }

        private void OnDestroy()
        {
            DayAndNightEvents.OnNewDay -= HandleNewDay;
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

                lastWaterDay = clock.GetCurrentDay();
                isWateredToday = false;

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

            waterIcon?.SetActive(!isWateredToday);
            harvestIcon?.SetActive(false);
        }

        // ============================================================
        //  SỰ KIỆN NGÀY MỚI
        // ============================================================
        private void HandleNewDay(int newDay)
        {
            if (isDead) return;

            // Chưa tưới hôm qua → chết
            if (lastWaterDay < newDay - 1)
            {
                Die();
                return;
            }

            // Nếu đã tưới → lớn
            if (isWateredToday)
            {
                lastWaterDay = newDay - 1;   // FIX QUAN TRỌNG
                Grow();
            }

            // Reset tưới
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
    }
}
