using UnityEngine;
using UnityEngine.InputSystem;

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
    [Tooltip("Prefab của cây bạn muốn trồng (có script Crop hoặc tương tự)")]
    public GameObject cropPrefab;

    [Tooltip("Tag của đất trồng cây (dattrongcay)")]
    public string plantableTag = "dattrongcay";    // Tag để kiểm tra đất có trồng được không

    [Tooltip("Layer của cây đã trồng (để tránh trồng chồng/sát)")]
    public LayerMask cropLayer;                     // Layer của cây (Crop)

    [Tooltip("Khoảng cách kiểm tra phía trước nhân vật")]
    public float interactDistance = 1f;

    [Tooltip("Kiểm tra bao gồm đường chéo? (8 hướng)")]
    public bool checkDiagonals = false;             // true: 8 hướng (rộng hơn), false: 4 hướng (chỉ up/down/left/right)

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        moveScript = GetComponent<moving>();
    }

    void Update()
    {
        // Cuốc đất - Phím H
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            CheckTileInFront();
            TriggerHoeAction();
        }

        // Tưới nước - Phím G
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            TriggerWateringAction();
        }

        // Trồng cây - Phím F
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryPlantCrop();
        }
    }

    public void UpdateMovementAnimation(Vector2 velocity, Vector2 movementInput)
    {
        if (animator == null || moveScript == null) return;

        Vector2 lastDir = moveScript.lastMoveDirection.normalized;
        float currentSpeed = movementInput.magnitude;

        animator.SetFloat("speed", currentSpeed);
        animator.SetFloat("horizontal", Mathf.Abs(lastDir.x));
        animator.SetFloat("vertical", lastDir.y);

        UpdateSpriteDirection(lastDir.x);
    }

    // Kiểm tra và in ra tile phía trước (cải tiến: snap vào giữa ô)
    private void CheckTileInFront()
    {
        if (moveScript == null) return;

        Vector2 lastDir = moveScript.lastMoveDirection.normalized;
        if (lastDir == Vector2.zero) lastDir = Vector2.down;

        Vector2 tentativePos = (Vector2)transform.position + lastDir * interactDistance;
        Vector2 checkCellCenter = new Vector2(Mathf.Round(tentativePos.x), Mathf.Round(tentativePos.y));

        Collider2D hit = Physics2D.OverlapBox(checkCellCenter, new Vector2(0.4f, 0.4f), 0f);
        if (hit != null)
        {
            Debug.Log($"Tile phía trước (giữa ô) tại {checkCellCenter} - Tag: {hit.tag}, Layer: {LayerMask.LayerToName(hit.gameObject.layer)}");
        }
        else
        {
            Debug.Log($"Tile phía trước (giữa ô) tại {checkCellCenter} - Không có gì");
        }
    }

    // Cuốc đất
    public void TriggerHoeAction()
    {
        if (animator == null || moveScript == null) return;

        Vector2 lastDir = moveScript.lastMoveDirection.normalized;
        if (lastDir == Vector2.zero) lastDir = Vector2.down;

        StopMovementAndFaceDirection(lastDir);

        float absX = Mathf.Abs(lastDir.x);
        float absY = Mathf.Abs(lastDir.y);

        if (absY >= absX)
        {
            animator.SetTrigger(lastDir.y > 0 ? "HoeUp" : "HoeDown");
        }
        else
        {
            animator.SetTrigger("HoeRight");
        }
    }

    // Tưới nước
    public void TriggerWateringAction()
    {
        if (animator == null || moveScript == null) return;

        Vector2 lastDir = moveScript.lastMoveDirection.normalized;
        if (lastDir == Vector2.zero) lastDir = Vector2.down;

        StopMovementAndFaceDirection(lastDir);

        float absX = Mathf.Abs(lastDir.x);
        float absY = Mathf.Abs(lastDir.y);

        if (absY >= absX)
        {
            animator.SetTrigger(lastDir.y > 0 ? "WateringUp" : "WateringDown");
        }
        else
        {
            animator.SetTrigger("WateringRight");
        }
    }

    // Trồng cây - Phím F (CẢI TIẾN: Snap giữa ô + Không trồng chồng + KHÔNG SÁT CÂY KHÁC 1 Ô)
    private void TryPlantCrop()
    {
        if (cropPrefab == null)
        {
            Debug.LogWarning("Chưa gán Crop Prefab trong PlayerHandler!");
            return;
        }

        if (moveScript == null) return;

        Vector2 direction = moveScript.lastMoveDirection.normalized;
        if (direction == Vector2.zero) direction = Vector2.down;

        // 1. Tính vị trí dự kiến và SNAP VÀO GIỮA Ô (grid 1x1)
        Vector2 tentativePos = (Vector2)transform.position + direction * interactDistance;
        Vector2 plantCellCenter = new Vector2(Mathf.Round(tentativePos.x), Mathf.Round(tentativePos.y));

        // 2. Kiểm tra tile ĐẤT có tag "dattrongcay" tại TRUNG TÂM Ô (vùng nhỏ 0.4x0.4)
        Collider2D soilHit = Physics2D.OverlapBox(plantCellCenter, new Vector2(0.4f, 0.4f), 0f);
        if (soilHit == null || !soilHit.CompareTag(plantableTag))
        {
            Debug.Log($"❌ Không thể trồng: Ô tại {plantCellCenter} phải có tag '{plantableTag}'!");
            return;
        }

        // 3. Kiểm tra CÂY CHỒNG trong ô này (vùng lớn 0.9x0.9)
        Collider2D[] cropHitsInCell = Physics2D.OverlapBoxAll(plantCellCenter, new Vector2(0.9f, 0.9f), 0f, cropLayer);
        if (cropHitsInCell.Length > 0)
        {
            Debug.Log($"❌ Không thể trồng: Đã có cây chồng trong ô tại {plantCellCenter}!");
            return;
        }

        // 4. CẢI TIẾN: Kiểm tra CÂY SÁT Ô XUNG QUANH (4 hoặc 8 hướng)
        if (!IsSurroundingCellsEmpty(plantCellCenter))
        {
            Debug.Log($"❌ Không thể trồng: Phải cách cây khác ÍT NHẤT 1 Ô (xung quanh)!");
            return;
        }

        // 5. TRỒNG CÂY NGAY GIỮA Ô + Z=0
        Vector3 finalPlantPos = new Vector3(plantCellCenter.x, plantCellCenter.y, 0f);
        GameObject newCrop = Instantiate(cropPrefab, finalPlantPos, Quaternion.identity);
        Debug.Log($"✅ Đã trồng cây NGAY GIỮA Ô tại {plantCellCenter} (an toàn xung quanh)");

        // 6. Animation
        TriggerPlantAction(direction);
    }

    // Kiểm tra các ô xung quanh có rỗng không (không có cây)
    private bool IsSurroundingCellsEmpty(Vector2 centerCell)
    {
        Vector2[] offsets;
        if (checkDiagonals)
        {
            // 8 hướng (bao gồm chéo)
            offsets = new Vector2[] {
                Vector2.up, Vector2.down, Vector2.left, Vector2.right,
                new Vector2(1,1), new Vector2(1,-1), new Vector2(-1,1), new Vector2(-1,-1)
            };
        }
        else
        {
            // 4 hướng (chỉ cardinal)
            offsets = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        }

        foreach (Vector2 offset in offsets)
        {
            Vector2 neighborCell = centerCell + offset;
            Collider2D[] neighborCrops = Physics2D.OverlapBoxAll(neighborCell, new Vector2(0.9f, 0.9f), 0f, cropLayer);
            if (neighborCrops.Length > 0)
            {
                Debug.Log($"   Phát hiện cây tại ô lân cận: {neighborCell}");
                return false;
            }
        }
        return true;
    }

    // Animation trồng cây
    private void TriggerPlantAction(Vector2 direction)
    {
        if (animator == null) return;

        StopMovementAndFaceDirection(direction);

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absY >= absX)
        {
            animator.SetTrigger(direction.y > 0 ? "PlantUp" : "PlantDown");
        }
        else
        {
            animator.SetTrigger("PlantRight");
        }
    }

    // Dừng di chuyển + quay mặt đúng hướng
    private void StopMovementAndFaceDirection(Vector2 direction)
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        animator.SetFloat("speed", 0f);
        UpdateSpriteDirection(direction.x);
    }

    // Lật sprite khi đi sang trái
    private void UpdateSpriteDirection(float lastMoveX)
    {
        if (spriteRenderer == null) return;

        if (lastMoveX < 0)
            spriteRenderer.flipX = true;
        else if (lastMoveX > 0)
            spriteRenderer.flipX = false;
    }

    // Gizmo debug: Vẽ vùng kiểm tra SNAP GIỮA Ô + XUNG QUANH
    private void OnDrawGizmosSelected()
    {
        if (moveScript == null) return;

        Vector2 dir = moveScript.lastMoveDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;

        Vector2 tentative = (Vector2)transform.position + dir * interactDistance;
        Vector2 cellCenter = new Vector2(Mathf.Round(tentative.x), Mathf.Round(tentative.y));

        // Vùng kiểm tra ĐẤT (nhỏ, xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(cellCenter, new Vector3(0.4f, 0.4f, 0));

        // Vùng kiểm tra CÂY CHỒNG trong ô (lớn, đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(cellCenter, new Vector3(0.9f, 0.9f, 0));

        // Vùng kiểm tra XUNG QUANH (cam, 4 hoặc 8 ô)
        Gizmos.color = Color.magenta;
        Vector2[] offsets = checkDiagonals ? Get8Directions() : Get4Directions();
        foreach (Vector2 offset in offsets)
        {
            Vector2 neighbor = cellCenter + offset;
            Gizmos.DrawWireCube(neighbor, new Vector3(0.9f, 0.9f, 0));
        }

        // Điểm giữa ô (xanh dương)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(cellCenter, 0.05f);

        // Line từ player đến cell center
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, cellCenter);
    }

    private Vector2[] Get4Directions() => new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    private Vector2[] Get8Directions() => new Vector2[] {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1,1), new Vector2(1,-1), new Vector2(-1,1), new Vector2(-1,-1)
    };
}