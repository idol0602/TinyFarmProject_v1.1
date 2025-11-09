using UnityEngine;
using UnityEngine.InputSystem;

// Đảm bảo PlayerHandler có trên Game Object
[RequireComponent(typeof(PlayerHandler))]
public class moving : MonoBehaviour
{
    private Rigidbody2D rb;
    public float speed = 5f;
    private Vector2 movement;

    // LastMoveDirection phải là public để PlayerHandler đọc được
    [HideInInspector] public Vector2 lastMoveDirection = new Vector2(1, 0);

    private PlayerHandler handler; // Tham chiếu đến Handler

    private Vector3 targetPosition;
    private bool isMovingToClick = false;

    // Các biến Animator và SpriteRenderer đã được xóa khỏi đây

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        handler = GetComponent<PlayerHandler>();

        // Đảm bảo Animator và SpriteRenderer đã được lấy trong PlayerHandler.cs
        targetPosition = transform.position;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        Vector2 inputMovement = Vector2.zero;

        // 1. Lấy Input Di chuyển (Bàn phím)
        if (keyboard.wKey.isPressed) inputMovement.y += 1;
        if (keyboard.sKey.isPressed) inputMovement.y -= 1;
        if (keyboard.aKey.isPressed) inputMovement.x -= 1;
        if (keyboard.dKey.isPressed) inputMovement.x += 1;

        // 2. Xử lý Di chuyển (Bàn phím hoặc Click)
        if (inputMovement != Vector2.zero)
        {
            isMovingToClick = false;
            movement = inputMovement.normalized;
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = 0;
            targetPosition = mouseWorld;
            isMovingToClick = true;
        }

        if (isMovingToClick)
        {
            Vector3 dir = (targetPosition - transform.position);

            if (dir.magnitude < 0.1f)
            {
                isMovingToClick = false;
                movement = Vector2.zero;
            }
            else
            {
                movement = dir.normalized;
            }
        }
        else if (inputMovement == Vector2.zero && !isMovingToClick)
        {
            movement = Vector2.zero;
        }

        // 3. Cập nhật Hướng Cuối cùng (Chỉ khi đang di chuyển)
        if (movement.magnitude > 0.01f)
        {
            lastMoveDirection = movement.normalized;
        }
    }

    private void FixedUpdate()
    {
        // Áp dụng di chuyển vật lý
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);

        // Gửi dữ liệu cho Handler xử lý animation (dùng vận tốc thực tế)
        if (handler != null)
        {
            handler.UpdateMovementAnimation(rb.linearVelocity, movement);
        }
    }
}