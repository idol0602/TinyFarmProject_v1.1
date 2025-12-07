using UnityEngine;

public class Move : MonoBehaviour
{
    [Header("=== Cấu hình di chuyển ===")]
    [Tooltip("Tốc độ ban đầu khi lao xuống")]
    public float speed = 5f;

    [Tooltip("Gia tốc tăng dần khi rơi xuống (càng lớn càng nhanh)")]
    public float acceleration = 15f;

    [Header("=== Các vị trí (gán trong Inspector) ===")]
    public Transform startPoint;    // Cao nhất – nơi xe hiện ra sau khi teleport
    public Transform spawnPoint;    // Vị trí xe đứng ban đầu và trở về cuối cùng
    public Transform endPoint;      // Dưới cùng – điểm kết thúc

    [Header("=== Chậm dần khi về vị trí cũ ===")]
    [Tooltip("Thời gian chậm dần (0.5 = nhanh, 1.2 = chậm mượt)")]
    public float decelerationTime = 0.8f;

    [Header("=== TỰ ĐỘNG TEST ===")]
    [Tooltip("Bật để xe tự chạy 1 lần khi vào Play mode (rất tiện test)")]
    public bool autoRunOnStart = true;

    [Tooltip("Thời gian chờ trước khi tự chạy (giây)")]
    public float autoRunDelay = 1.5f;

    // Trạng thái nội bộ
    private enum State { Idle, AcceleratingDown, TeleportBack, DeceleratingToSpawn }
    private State currentState = State.Idle;

    private Vector3 smoothDampVelocity;
    private float currentSpeed;

    public bool IsRunning => currentState != State.Idle;

    private void Awake()
    {
        if (spawnPoint != null)
            transform.position = spawnPoint.position;
    }

    private void Start()
    {
        // TỰ ĐỘNG CHẠY KHI VÀO PLAY (để test nhanh)
        if (autoRunOnStart)
        {
            Invoke(nameof(Run), autoRunDelay);
        }
    }

    /// <summary>
    /// GỌI HÀM NÀY ĐỂ KÍCH HOẠT XE CHẠY (từ UI, OrderManager, v.v.)
    /// </summary>
    public void Run()
    {
        if (IsRunning)
        {
            Debug.Log("[Move] Xe đang chạy rồi, không cho chạy chồng!");
            return;
        }

        if (startPoint == null || spawnPoint == null || endPoint == null)
        {
            Debug.LogError("[Move] Chưa gán đủ 3 điểm vị trí! Kiểm tra Inspector.");
            return;
        }

        // Bắt đầu chu trình
        currentState = State.AcceleratingDown;
        currentSpeed = speed;
        Debug.Log("[Move] XE BẮT ĐẦU LAO XUỐNG!");
    }

    private void Update()
    {
        if (!IsRunning) return;

        switch (currentState)
        {
            case State.AcceleratingDown:
                AccelerateDown();
                break;
            case State.TeleportBack:
                TeleportToStart();
                break;
            case State.DeceleratingToSpawn:
                DecelerateToSpawn();
                break;
        }
    }

    private void AccelerateDown()
    {
        currentSpeed += acceleration * Time.deltaTime;
        transform.position += Vector3.down * currentSpeed * Time.deltaTime;

        if (transform.position.y <= endPoint.position.y)
        {
            currentState = State.TeleportBack;
        }
    }

    private void TeleportToStart()
    {
        transform.position = startPoint.position;
        currentState = State.DeceleratingToSpawn;
        smoothDampVelocity = Vector3.down * 3f; // rơi nhẹ từ trên xuống
    }

    private void DecelerateToSpawn()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            spawnPoint.position,
            ref smoothDampVelocity,
            decelerationTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        if (Vector3.Distance(transform.position, spawnPoint.position) < 0.05f)
        {
            transform.position = spawnPoint.position;
            smoothDampVelocity = Vector3.zero;
            currentState = State.Idle;
            Debug.Log("[Move] XE ĐÃ VỀ BẾN – Sẵn sàng giao đơn tiếp theo!");
        }
    }

    // Dùng để gọi từ bên ngoài (OrderManager, Button, v.v.)
    public bool CanRun() => !IsRunning;

    public void ForceStopAndReset()
    {
        currentState = State.Idle;
        if (spawnPoint) transform.position = spawnPoint.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnPoint) { Gizmos.color = Color.green; Gizmos.DrawSphere(spawnPoint.position, 0.4f); Gizmos.DrawIcon(spawnPoint.position, "d_Transform Icon", true); }
        if (startPoint) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(startPoint.position, 0.4f); }
        if (endPoint) { Gizmos.color = Color.red; Gizmos.DrawSphere(endPoint.position, 0.4f); }

        if (startPoint && endPoint)
        {
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
    }
#endif
}