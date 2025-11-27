using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections;

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
    public GameObject cropPrefab;
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
    public Light2D globalLight;   // Drag Global Light vào đây
    public float fadeDuration = 2f;

    private float originalLightIntensity;


    private bool isNearBed = false;
    private bool isSleeping = false;

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
            if (Keyboard.current.gKey.wasPressedThisFrame) TriggerWateringAction();
            if (Keyboard.current.fKey.wasPressedThisFrame) TryPlantCrop();
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
        if (isSleeping) return;

        isSleeping = true;

        // Dịch chuyển lên giường
        transform.position = new Vector3(sleepPosition.x, sleepPosition.y, transform.position.z);

        // Ngắt hành động + khóa di chuyển
        ForceStopAllActions();
        animator.SetBool("isSleeping", true);
        CloseSleepDialog();
        LockPlayerMovement(true);

        // ✅ Tối 2s → sáng lại 2s NGAY LẬP TỨC
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
    private void ForceStopAllActions()
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

    private void LockPlayerMovement(bool lockIt)
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
        animator.SetTrigger(absY >= absX ? (dir.y > 0 ? "WateringUp" : "WateringDown") : "WateringRight");
    }

    private void TryPlantCrop()
    {
        if (isSleeping || cropPrefab == null || moveScript == null) return;

        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;

        Vector2 pos = (Vector2)transform.position + dir * interactDistance;
        Vector2 center = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));

        Collider2D soil = Physics2D.OverlapBox(center, new Vector2(0.4f, 0.4f), 0f);
        if (soil == null || !soil.CompareTag(plantableTag)) return;

        if (Physics2D.OverlapBoxAll(center, new Vector2(0.9f, 0.9f), 0f, cropLayer).Length > 0) return;
        if (!IsSurroundingCellsEmpty(center)) return;

        Instantiate(cropPrefab, new Vector3(center.x, center.y, 0f), Quaternion.identity);
        TriggerPlantAction(dir);
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

    private IEnumerator SleepLightRoutine()
    {
        // 🌑 Tối 2s
        yield return StartCoroutine(FadeLight(originalLightIntensity, 0f, fadeDuration));

        // ✅ SANG NGÀY MỚI + SET 7:00
        DayAndNightManager dayNight = Object.FindFirstObjectByType<DayAndNightManager>();
        if (dayNight != null)
        {
            dayNight.SleepToNextDay();
        }

        // ☀️ Sáng 2s
        yield return StartCoroutine(FadeLight(0f, originalLightIntensity, fadeDuration));
    }
}