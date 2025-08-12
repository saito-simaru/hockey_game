using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 6f;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 12f;
    public float fireCooldown = 0.25f;
    private float lastFireTime;

    [Header("Identity")]
    public int playerId; // 0 or 1 で識別
    private PlayerInput playerInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        // PlayerInput の playerIndex をそのままIDに
        playerId = playerInput.playerIndex;

        // 色分け（簡易）
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = (playerId == 0) ? new Color(0.2f,0.7f,1f) : new Color(1f,0.4f,0.4f);
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
        // 向き（発射方向）を入力方向に（入力があれば）
        if (moveInput.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }
    }

    // Input System の "Player" Action Map と名前を合わせる
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnFire()
    {
        Debug.Log($"ファイアを入力");
        if (Time.time - lastFireTime < fireCooldown) return;
        
        lastFireTime = Time.time;

        if (bulletPrefab != null && firePoint != null)
        {
            Debug.Log($"Player {playerId} fired!");
            var go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            var brb = go.GetComponent<Rigidbody2D>();
            if (brb != null)
            {
                Vector2 dir = (Vector2)firePoint.right; // プレイヤーの右向きを前方とする
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
                brb.velocity = dir.normalized * bulletSpeed;
            }

            // 弾に「撃った側」のIDを伝える
            var b = go.GetComponent<bullet>();
            if (b != null) b.ownerId = playerId;
        }
    }
}
