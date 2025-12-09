using UnityEngine;

namespace MapSummer
{
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

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            clock = DayAndNightManager.Instance;

            // 🌱 CÂY MỚI TRỒNG
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

            // CÂY LOAD TỪ SAVE không SpawnIcons ở đây (đã spawn trong LoadFromData)

            DayAndNightEvents.OnNewDay += HandleNewDay;
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
            if (isDead) return;

            // Không tưới → chết
            if (lastWaterDay < newDay - 1)
            {
                Die();
                return;
            }

            // Tưới rồi → phát triển
            if (isWateredToday)
                Grow();

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
