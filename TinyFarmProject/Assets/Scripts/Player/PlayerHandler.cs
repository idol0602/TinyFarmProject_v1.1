using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections;
using MapSummer;
using UnityEngine.SceneManagement;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(moving))]
public class PlayerHandler : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private moving moveScript;
    [Header("=== TRỒNG CÂY ===")]
    public GameObject cornPrefab;
    public GameObject chiliPrefab;
    public GameObject tomatoPrefab;
    public GameObject eggplantPrefab;
    public GameObject watermelonPrefab;


    public string plantableTag = "dattrongcay";
    public LayerMask cropLayer;
    public float interactDistance = 1f;
    public bool checkDiagonals = false;
    [Header("=== NGỦ ===")]
    public GameObject sleepDialog;
    public Button yesButton;
    public Button noButton;
    public Vector2 sleepPosition = new Vector2(-23.99f, 0.4f);
    public Vector2 wakeupPosition = new Vector2(-26.6759f, -3.24255f);
    [Header("=== HIỆU ỨNG ÁNH SÁNG NGỦ ===")]
    public Light2D globalLight; // Drag Global Light vào đây
    public float fadeDuration = 2f;
    private float originalLightIntensity;
    private bool isNearBed = false;
    private bool isSleeping = false;
public string currentCropType = "Chili";


    void Awake()
    {
        if (globalLight != null)
            originalLightIntensity = globalLight.intensity;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        moveScript = GetComponent<moving>();
        if (sleepDialog == null)
            sleepDialog = GameObject.Find("SleepDialog");
        if (sleepDialog != null)
            sleepDialog.SetActive(false);
    }
    void OnEnable()
    {
        if (yesButton != null) yesButton.onClick.AddListener(OnYesButton);
        if (noButton != null) noButton.onClick.AddListener(OnNoButton);
    }
    void OnDisable()
    {
        if (yesButton != null) yesButton.onClick.RemoveAllListeners();
        if (noButton != null) noButton.onClick.RemoveAllListeners();
    }
    void Update()
    {
        // Chỉ cho phép hành động khi không ngủ
        if (!isSleeping)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame) { CheckTileInFront(); TriggerHoeAction(); }
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                TriggerWateringAction();
                TryWaterCrop(); // 👈 thêm dòng này
            }
            if (Keyboard.current.fKey.wasPressedThisFrame) TryPlantCrop();
            if (Keyboard.current.eKey.wasPressedThisFrame)
                TryHarvest();
        }
        // Tỉnh dậy khi nhấn phím di chuyển
        if (isSleeping && HasAnyMovementInput())
        {
            WakeUp();
        }
    }
    // =================== NGỦ ===================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bed") && !isSleeping)
        {
            isNearBed = true;
            sleepDialog?.SetActive(true);
            ForceStopAllActions();
            LockPlayerMovement(true);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Bed") && !isSleeping)
        {
            isNearBed = false;
            CloseSleepDialog();
        }
    }
    private void OnYesButton()
    {
        int hour = DayAndNightManager.Instance.GetCurrentHour();

        // ⭐ CHẶN NGỦ TRƯỚC 20:00 (có thể chỉnh thành 18 nếu muốn)
        if (hour < 20)
        {
            Debug.Log($"⛔ Không thể ngủ bây giờ! Hiện tại mới {hour}:00. Phải sau 20:00.");

            UIManager.ShowMessage("Ngủ sớm vậy ní!");

            // ⭐ MỞ LẠI DI CHUYỂN
            LockPlayerMovement(false);
            ForceStopAllActions();

            // ⭐ ẨN DIALOG vì không ngủ được
            CloseSleepDialog();

            return;
        }


        if (isSleeping) return;
        isSleeping = true;

        // ⭐ SAVE FARM TRƯỚC KHI NGỦ
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "MapSummer")
        {
            if (FirebaseDatabaseManager.Instance != null && FirebaseDatabaseManager.FirebaseReady)
            {
                FirebaseDatabaseManager.Instance.SaveFarmToFirebase("Player1");
                Debug.Log("💾 [Sleep] SAVE farm tại MapSummer");
            }
        }
        else
        {
            Debug.Log("⚠ Không SAVE vì đang ở scene trong nhà, không có crop!");
        }

        // ⭐ DỊCH CHUYỂN LÊN GIƯỜNG
        transform.position = new Vector3(sleepPosition.x, sleepPosition.y, transform.position.z);

        ForceStopAllActions();
        animator.SetBool("isSleeping", true);
        CloseSleepDialog();
        LockPlayerMovement(true);
        FarmState.IsSleepTransition = true;   // <--- PHẢI CÓ


        // ⭐ FADE SÁNG TỐI
        if (globalLight != null)
            StartCoroutine(SleepLightRoutine());

        Debug.Log("Đi ngủ... Đang chuyển ngày...");
    }

    private void OnNoButton()
    {
        CloseSleepDialog();
    }
    private void CloseSleepDialog()
    {
        if (sleepDialog != null) sleepDialog.SetActive(false);
        if (!isSleeping) LockPlayerMovement(false);
    }
    private void WakeUp()
    {
        isSleeping = false;
        transform.position = new Vector3(wakeupPosition.x, wakeupPosition.y, transform.position.z);
        // Ngắt sạch mọi hành động + animation
        ForceStopAllActions();
        animator.SetBool("isSleeping", false);
        LockPlayerMovement(false);
        if (globalLight != null)
            StartCoroutine(FadeLight(0f, originalLightIntensity, fadeDuration));
        Debug.Log("Tỉnh dậy rồi!");
        // FindObjectOfType<DayNightManager>()?.NextDay(); // nếu cần
    }
    // =================== NGẮT HOÀN TOÀN HÀNH ĐỘNG ===================
    public void ForceStopAllActions()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (moveScript != null) moveScript.enabled = false;
        // Reset tất cả trigger
        animator.ResetTrigger("HoeUp");
        animator.ResetTrigger("HoeDown");
        animator.ResetTrigger("HoeRight");
        animator.ResetTrigger("WateringUp");
        animator.ResetTrigger("WateringDown");
        animator.ResetTrigger("WateringRight");
        animator.ResetTrigger("PlantUp");
        animator.ResetTrigger("PlantDown");
        animator.ResetTrigger("PlantRight");
        animator.SetFloat("speed", 0f);
    }
    public void LockPlayerMovement(bool lockIt)
    {
        if (moveScript != null)
            moveScript.enabled = !lockIt && !isSleeping;
    }
    private bool HasAnyMovementInput()
    {
        return Keyboard.current.wKey.isPressed ||
               Keyboard.current.aKey.isPressed ||
               Keyboard.current.sKey.isPressed ||
               Keyboard.current.dKey.isPressed ||
               Keyboard.current.upArrowKey.isPressed ||
               Keyboard.current.downArrowKey.isPressed ||
               Keyboard.current.leftArrowKey.isPressed ||
               Keyboard.current.rightArrowKey.isPressed;
    }
    // =================== CÁC HÀM CŨ (HOÀN CHỈNH) ===================
    public void UpdateMovementAnimation(Vector2 velocity, Vector2 movementInput)
    {
        if (animator == null || moveScript == null) return;
        Vector2 lastDir = moveScript.lastMoveDirection.normalized;
        float speed = movementInput.magnitude;
        animator.SetFloat("speed", speed);
        animator.SetFloat("horizontal", Mathf.Abs(lastDir.x));
        animator.SetFloat("vertical", lastDir.y);
        UpdateSpriteDirection(lastDir.x);
    }
    private void CheckTileInFront()
    {
        if (moveScript == null) return;
        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;
        Vector2 pos = (Vector2)transform.position + dir * interactDistance;
        Vector2 center = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
        Collider2D hit = Physics2D.OverlapBox(center, new Vector2(0.4f, 0.4f), 0f);
        if (hit != null)
            Debug.Log($"Tile phía trước: {hit.tag}");
    }
    public void TriggerHoeAction()
    {
        if (isSleeping || animator == null) return;
        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;
        StopMovementAndFaceDirection(dir);
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        animator.SetTrigger(absY >= absX ? (dir.y > 0 ? "HoeUp" : "HoeDown") : "HoeRight");
    }
    public void TriggerWateringAction()
    {
        if (isSleeping || animator == null) return;
        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;
        StopMovementAndFaceDirection(dir);
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        if (absY >= absX)
            animator.SetTrigger(dir.y > 0 ? "WateringUp" : "WateringDown");
        else
            animator.SetTrigger("WateringRight");
    }
    private void TryPlantCrop()
    {
        if (isSleeping || moveScript == null) return;

        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;

        Vector2 pos = (Vector2)transform.position + dir * interactDistance;
        Vector2 center = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));

        // 1. CHECK đất trồng
        Collider2D soil = Physics2D.OverlapBox(center, new Vector2(0.4f, 0.4f), 0f);
        if (soil == null || !soil.CompareTag(plantableTag))
        {
            return;
        }

        // 2. CHECK đã có cây chưa
        Collider2D existCrop = Physics2D.OverlapBox(
            center,
            new Vector2(0.8f, 0.8f),
            0f,
            cropLayer
        );

        if (existCrop != null)
        {
            Debug.Log("❌ Ô này đã có cây, không thể trồng");
            return;
        }

        // 3. LẤY PREFAB THEO LOẠI CÂY
        GameObject prefab = GetCropPrefab(currentCropType);
        if (prefab == null)
        {
            Debug.LogError("❌ Không load được prefab loại cây: " + currentCropType);
            return;
        }

        // 4. TRỒNG CÂY
        GameObject newCrop = Instantiate(prefab, new Vector3(center.x, center.y, 0f), Quaternion.identity);

        // ⭐ GÁN LOẠI CÂY
        newCrop.GetComponent<Crop>().cropType = currentCropType;

        // Animation
        TriggerPlantAction(dir);

        Debug.Log("🌱 Trồng thành công loại: " + currentCropType);
    }


    private void TriggerPlantAction(Vector2 dir)
    {
        if (isSleeping || animator == null) return;
        StopMovementAndFaceDirection(dir);
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        animator.SetTrigger(absY >= absX ? (dir.y > 0 ? "PlantUp" : "PlantDown") : "PlantRight");
    }
    private bool IsSurroundingCellsEmpty(Vector2 center)
    {
        Vector2[] dirs = checkDiagonals ? Get8Directions() : Get4Directions();
        foreach (Vector2 d in dirs)
        {
            if (Physics2D.OverlapBoxAll(center + d, new Vector2(0.9f, 0.9f), 0f, cropLayer).Length > 0)
                return false;
        }
        return true;
    }
    private void StopMovementAndFaceDirection(Vector2 dir)
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        animator.SetFloat("speed", 0f);
        UpdateSpriteDirection(dir.x);
    }
    private void UpdateSpriteDirection(float x)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = x < 0;
    }
    private Vector2[] Get4Directions() => new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    private Vector2[] Get8Directions() => new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1,1), new Vector2(1,-1), new Vector2(-1,1), new Vector2(-1,-1)
    };
    private IEnumerator FadeLight(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            globalLight.intensity = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        globalLight.intensity = to;
    }
    // PlayerHandler.cs (SleepLightRoutine)
    private IEnumerator SleepLightRoutine()
    {
        // 🌑 Tối 2s
        yield return StartCoroutine(FadeLight(originalLightIntensity, 0f, fadeDuration));

        // ⭐ Đánh dấu là từ việc ngủ → không load khi quay lại MapSummer
        FarmState.IsSleepTransition = true;

        // ⭐ Đánh dấu cần SAVE khi quay lại MapSummer
        FarmState.NeedSaveAfterReturn = true;

        // Chuyển ngày
        DayAndNightManager.Instance.SleepToNextDay();

        // ☀️ Sáng lại 2s
        yield return StartCoroutine(FadeLight(0f, originalLightIntensity, fadeDuration));
    }




    //private void TryWaterCrop()
    //{
    //    // Lấy hướng
    //    Vector2 dir = moveScript.lastMoveDirection.normalized;
    //    if (dir == Vector2.zero) dir = Vector2.down;
    //    // Vị trí cần kiểm tra (không Round)
    //    Vector2 checkPos = (Vector2)transform.position + dir * interactDistance;
    //    // Overlap rộng hơn để không miss
    //    Vector2 boxSize = new Vector2(0.9f, 0.9f);
    //    Collider2D hit = Physics2D.OverlapBox(
    //        checkPos,
    //        boxSize,
    //        0f,
    //        cropLayer // Layer của ColliderWaterCheck
    //    );
    //    if (hit != null)
    //    {
    //        Crop crop = hit.GetComponentInParent<Crop>();
    //        if (crop != null)
    //        {
    //            crop.Water();
    //            Debug.Log("💧 Tưới thành công!");
    //            return;
    //        }
    //        Debug.Log("❌ hit được collider nhưng KHÔNG có Crop.cs");
    //    }
    //    else
    //    {
    //        Debug.Log("❌ Không tìm thấy Crop để tưới.");
    //    }
    //}

    private void TryWaterCrop()
    {
        // ❌ Không cho tưới nếu giờ trong game là 19h–5h
        if (!CanWaterNow())
        {
            Debug.Log($"⛔ Không thể tưới nước từ 19h đến 5h sáng! (Giờ hiện tại: {DayAndNightManager.Instance.GetCurrentHour()}:00)");
            return;
        }

        if (moveScript == null) return;

        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;

        // Tâm khu vực tưới phía trước người chơi
        Vector2 centerPos = (Vector2)transform.position + dir * interactDistance;
        Vector2 center = new Vector2(Mathf.Round(centerPos.x), Mathf.Round(centerPos.y));

        // 3x3 khu vực xung quanh
        Vector2[] directions = new Vector2[]
        {
        new Vector2(-1, -1), new Vector2(-1, 0), new Vector2(-1, 1),
        new Vector2( 0, -1), new Vector2( 0, 0), new Vector2( 0, 1),
        new Vector2( 1, -1), new Vector2( 1, 0), new Vector2( 1, 1)
        };

        int wateredCount = 0;

        foreach (Vector2 offset in directions)
        {
            Vector2 checkPos = center + offset;

            Collider2D hit = Physics2D.OverlapBox(
                checkPos,
                new Vector2(0.9f, 0.9f),
                0f,
                cropLayer
            );

            if (hit != null)
            {
                Crop crop = hit.GetComponentInParent<Crop>();
                if (crop != null)
                {
                    crop.Water();
                    wateredCount++;
                }
            }
        }

        if (wateredCount > 0)
        {
            Debug.Log($"💧 Tưới thành công {wateredCount} cây trong khu vực 3x3!");
        }
        else
        {
            Debug.Log("⚠ Không có cây nào để tưới trong khu vực.");
        }
    }

    private void TryHarvest()
    {
        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;
        Vector2 pos = (Vector2)transform.position + dir * interactDistance;
        Collider2D hit = Physics2D.OverlapBox(
            pos,
            new Vector2(1f, 1f),
            0f,
            cropLayer
        );
        if (hit != null)
        {
            Crop crop = hit.GetComponentInParent<Crop>();
            if (crop != null)
            {
                crop.Harvest(); // ⭐ BẬT LẠI DÒNG NÀY
                Debug.Log("🌾 Thu hoạch thành công!");
            }
        }
        // ⭐ Tự động SAVE sau khi thu hoạch
        if (FirebaseDatabaseManager.Instance != null && FirebaseDatabaseManager.FirebaseReady)
        {
            FirebaseDatabaseManager.Instance.SaveFarmToFirebase("Player1");
            Debug.Log("💾 Save Farm sau khi thu hoạch");
        }
    }
    // =================== KIỂM TRA GIỜ TRONG GAME ===================
    private bool CanWaterNow()
    {
        int hour = DayAndNightManager.Instance.GetCurrentHour();

        // ⛔ CẤM TƯỚI từ 19h → 23h59 và 0h → 4h59
        if (hour >= 19 || hour < 5)
            return false;

        return true;
    }
    private GameObject GetCropPrefab(string cropType)
    {
        switch (cropType)
        {
            case "Corn": return cornPrefab;
            case "Chili": return chiliPrefab;
            case "Tomato": return tomatoPrefab;
            case "Eggplant": return eggplantPrefab;
            case "Watermelon": return watermelonPrefab;

        }

        Debug.LogError("❌ Không có prefab cho loại cây: " + cropType);
        return null;
    }

}
