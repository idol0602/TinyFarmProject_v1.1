using UnityEngine;

namespace MapSummer
{
    public class Crop : MonoBehaviour
    {
        public Sprite[] stages;
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

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            clock = DayAndNightManager.Instance;

            if (!isLoadedFromSave)
            {
                CropID = System.Guid.NewGuid().ToString();
                sr.sprite = stages[0];

                lastWaterDay = clock.GetCurrentDay();
                isWateredToday = false;
            }

            SpawnIcons();
            UpdateIcons();

            DayAndNightEvents.OnNewDay += HandleNewDay;
        }

        private void OnDestroy()
        {
            DayAndNightEvents.OnNewDay -= HandleNewDay;
        }

        private void SpawnIcons()
        {
            waterIcon = Instantiate(waterIconPrefab, transform);
            waterIcon.transform.localPosition = new Vector3(0, 0.8f, 0);

            harvestIcon = Instantiate(harvestIconPrefab, transform);
            harvestIcon.transform.localPosition = new Vector3(0, 1.2f, 0);
        }

        private void UpdateIcons()
        {
            if (isDead)
            {
                waterIcon.SetActive(false);
                harvestIcon.SetActive(true);
                return;
            }

            if (currentStage == stages.Length - 1)
            {
                harvestIcon.SetActive(true);
                waterIcon.SetActive(false);
                return;
            }

            waterIcon.SetActive(!isWateredToday);
            harvestIcon.SetActive(false);
        }

        private void HandleNewDay(int newDay)
        {
            if (isDead) return;

            if (lastWaterDay < newDay - 1)
            {
                Die();
                return;
            }

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

        // ⭐⭐⭐ HÀM THU HOẠCH (BẮT BUỘC PHẢI CÓ) ⭐⭐⭐
        public void Harvest()
        {
            Debug.Log("🌾 Thu hoạch crop thành công!");
            Destroy(gameObject);
        }

        public void LoadFromData(CropData d)
        {
            isLoadedFromSave = true;

            CropID = d.cropID;
            currentStage = d.stage;
            isDead = d.isDead;
            lastWaterDay = d.lastWaterDay;
            isWateredToday = d.isWateredToday;

            sr = GetComponent<SpriteRenderer>();
            clock = DayAndNightManager.Instance;

            sr.sprite = isDead ? deadSprite : stages[currentStage];

            foreach (Transform t in transform)
                Destroy(t.gameObject);

            SpawnIcons();
            UpdateIcons();
        }
    }
}
