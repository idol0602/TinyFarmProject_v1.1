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

        // ⭐ FLAG NGĂN TƯỚI 2 LẦN CÙNG 1 SỰKIỆN MƯA
        private bool hasAutoWateredThisRain = false;

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

        // ⭐ UPDATE: KIỂM TRA MƯA LIÊN TỤC
        // FIX: CÂY SPAWN SAU KHI SỰ KIỆN MƯA PHÁT SINH CẦN ĐƯỢC TƯỚI
        private void Update()
        {
            if (isDead) return;
            if (isWateredToday) return;  // Đã tưới hôm nay rồi
            if (RainManager.Instance == null) return;

            // ⭐ NẾU TRỜI ĐANG MƯA & CHƯA TƯỚI → TƯỚI NGAY
            if (RainManager.Instance.isRaining && !hasAutoWateredThisRain)
            {
                Water();
                hasAutoWateredThisRain = true;  // Đánh dấu đã tưới từ sự kiện mưa này
                Debug.Log($"🌧️ Update detect: Trời mưa → Tưới {cropType} ngay");
            }
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

                lastWaterDay = today;
                isWateredToday = false;

                sr.sprite = stages[currentStage];

                SpawnIcons();
                UpdateIcons();
                
                // ⭐ NẾU TRỜI ĐANG MƯA KHI TRỒNG → TƯỚI NGAY
                if (RainManager.Instance != null && RainManager.Instance.isRaining)
                {
                    Water();
                }
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

            // ⭐ RESET isWateredToday
            isWateredToday = false;
            hasAutoWateredThisRain = false;  // ⭐ RESET flag mưa cho ngày mới
            
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
            // ⭐ NẾU CÂY ĐÃ CHẾT → CHỈ XÓA ĐI, KHÔNG THU HOẠCH
            if (isDead)
            {
                Debug.Log($"💀 {cropType} đã chết, xóa khỏi farm");
                Destroy(gameObject);
                return;
            }

            // ⭐ KIỂM TRA CÂY PHẢI CHÍN (STAGE CUỐI) MỚI CÓ THỂ THU HOẠCH
            if (currentStage != stages.Length - 1)
            {
                Debug.Log($"⚠️ {cropType} chưa chín! Stage hiện tại: {currentStage}/{stages.Length - 1}");
                return;
            }

            // ⭐ THÊM SẢN PHẨM VÀO TÚI ĐỒ
            string productName = cropType + "Crop"; // VD: "Chili" → "ChiliCrop"
            
            ItemDatabase itemDB = ItemDatabase.Instance;
            if (itemDB != null)
            {
                ItemData productItem = itemDB.GetItemByName(productName);
                if (productItem != null)
                {
                    InventoryManager.Instance.AddItem(productItem, 1);
                    Debug.Log($"✅ Thu hoạch thành công! Thêm {productName} vào túi");

                    // ⭐ SAVE INVENTORY VÀO FIREBASE
                    if (FirebaseDatabaseManager.Instance != null && FirebaseDatabaseManager.FirebaseReady)
                    {
                        FirebaseDatabaseManager.Instance.SaveInventoryToFirebase(PlayerSession.GetCurrentUserId());
                        Debug.Log("💾 Save Inventory sau khi thu hoạch");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Không tìm thấy item: {productName}");
                }
            }
            
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
            // ⭐ TRỜI MƯA → TƯỚI NGAY TẤT CẢ CÂY HIỆN TẠI
            if (isRaining && !isDead && !isWateredToday && !hasAutoWateredThisRain)
            {
                Water();
                hasAutoWateredThisRain = true;  // Đánh dấu đã tưới từ sự kiện này
                Debug.Log($"🌧️ Event detect: Trời mưa → Tưới {cropType} ngay");
            }
            
            // ⭐ TRỜI HẾT MƯA → RESET FLAG
            if (!isRaining)
            {
                hasAutoWateredThisRain = false;
            }

            UpdateIcons();
        }

    }

}
