using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class player : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;
    private float moveX;
    private Rigidbody2D rb;


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
        // 方向は「常にワールドX」
        Vector2 worldDir = Vector2.right; // (1, 0) 固定。ローカル回転の影響を受けない
        Vector2 delta = worldDir * (moveX * moveSpeed * Time.fixedDeltaTime);

        // ワールド座標で安全に移動
        rb.MovePosition(rb.position + delta);
    }
    // Input System の "Player" Action Map と名前を合わせる
    // Input Action "Move" にバインドしたときに呼ばれる
    void OnMove(InputValue context)
    {
        Debug.Log($"Player {playerId} Move Input Received");
        Vector2 input = context.Get<Vector2>();
        moveX = input.x;
    }
}
