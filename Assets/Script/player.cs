using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; 

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
        if (sr != null) sr.color = (playerId == 0) ? new Color(0.2f, 0.7f, 1f) : new Color(1f, 0.4f, 0.4f);
    }

    void FixedUpdate()
    {
        // 方向は「常にワールドX」
        Vector2 worldDir = Vector2.right; // (1, 0) 固定。ローカル回転の影響を受けない
        Vector2 delta = worldDir * (moveX * moveSpeed * Time.fixedDeltaTime);

        // ワールド座標で安全に移動
        rb.MovePosition(rb.position + delta);

        //以下回転処理
        if (_isAnimating) return; // 戻し中は手動回転しない

        int dir = 0;
        // 同時押しは停止優先
        if (_positiveHeld && !_negativeHeld) dir = +1;     // 左回り（+）
        else if (_negativeHeld && !_positiveHeld) dir = -1; // 右回り（-）

        if (dir != 0)
        {
            float z = GetSignedZ();
            z += dir * holdRotateSpeed * Time.fixedDeltaTime;
            z = Mathf.Clamp(z, -angleLimit, +angleLimit);
            SetSignedZ(z);
        }
    }
    // Input System の "Player" Action Map と名前を合わせる
    // Input Action "Move" にバインドしたときに呼ばれる
    void OnMove(InputValue context)
    {
        Debug.Log($"Player {playerId} Move Input Received");
        Vector2 input = context.Get<Vector2>();
        moveX = input.x;
    }

    [Header("回転設定（押下中）")]
    [Tooltip("押している間の角速度[deg/sec]")]
    public float holdRotateSpeed = 180f;

    [Tooltip("左右の最大角度[deg]（±で適用）")]
    public float angleLimit = 70f;

    [Header("離した後の戻し設定（等速）")]
    [Tooltip("現在角度の符号反転角まで戻す所要時間[sec]")]
    public float toNegativeDuration = 0.15f;

    [Tooltip("その後 0° まで戻す所要時間[sec]")]
    public float toZeroDuration = 0.20f;

    // 入力状態
    private bool _positiveHeld; // 左回り（+）
    private bool _negativeHeld; // 右回り（-）
    private bool _wasHeld;      // 直前フレームまで押されていたか

    // アニメーション中フラグ
    private bool _isAnimating;

// ====== InputSystem コールバック ======
    // 例：2D Vectorや1D Axisをアクションに割り当てて呼ぶ
    // ・Vector2なら x>0 を positive, x<0 を negative と解釈
    // ・Axis/ボタンでも 1/0/-1 を解釈できるように対応
    void OnFlick(InputValue context)
    {
        // どの型で来ても左右がわかるように頑張って読む
        float x = 0f;
        // Vector2
        // if (context.valueType == typeof(Vector2))
        // {
        //     Vector2 v = context.Get<Vector2>();
        //     x = v.x;
        // }
        // // float（1D Axis）
        // else if (context.valueType == typeof(float))
        // {
            x = context.Get<float>();
        // }
        // bool（ボタン）: この場合は「どちらのボタンか」でアクションを分けるのが普通ですが、
        // 単一メソッド要求に合わせ、x=+1 側として扱います（必要なら別アクションを作成して OnFlickNegative を用意してください）
        // else if (context.valueType == typeof(bool))
        // {
            // bool pressed = context.Get<bool>();
            // x = pressed ? 1f : 0f;
        // }

        bool positive = x > 0.5f;
        bool negative = x < -0.5f;

        // 状態更新
        bool hadInputBefore = _positiveHeld || _negativeHeld;
        _positiveHeld = positive;
        _negativeHeld = negative;

        // 押下→離し の立ち下がりを検出（両方falseになった瞬間）
        if (hadInputBefore && !(_positiveHeld || _negativeHeld))
        {
            // 押し終えたので戻しシーケンス開始
            if (!_isAnimating)
            {
                StopAllCoroutines();
                StartCoroutine(ReturnSequence());
            }
        }

        // 立ち上がり記録
        _wasHeld = _positiveHeld || _negativeHeld;
    }


    // ====== 戻しシーケンス（等速） ======
    private IEnumerator ReturnSequence()
    {
        _isAnimating = true;

        // Step1: 現在角度 -> その符号反転角（-current）
        yield return RotateToAngleConstantSpeed(targetSignedDeg: -GetSignedZ(), duration: toNegativeDuration);

        // Step2: そこから -> 0°
        yield return RotateToAngleConstantSpeed(targetSignedDeg: 0f, duration: toZeroDuration);

        _isAnimating = false;
    }

    private IEnumerator RotateToAngleConstantSpeed(float targetSignedDeg, float duration)
    {
        // 等速で回転させる：必要角度 / 時間 = 角速度
        float start = GetSignedZ();
        float end = Mathf.Clamp(targetSignedDeg, -180f, 180f); // 念のため正規化範囲
        float remain = Mathf.Abs(Mathf.DeltaAngle(start, end));
        float speed = (duration > 0f) ? (remain / duration) : float.PositiveInfinity;

        float t = 0f;
        while (true)
        {
            float current = GetSignedZ();
            // MoveTowardsAngleは角度差に応じて等速で近づけられる
            float next = Mathf.MoveTowardsAngle(current, end, speed * Time.deltaTime);
            SetSignedZ(next);

            t += Time.deltaTime;
            if (Mathf.Approximately(Mathf.DeltaAngle(next, end), 0f) || t >= duration)
                break;

            yield return null;
        }

        // 最終スナップ
        SetSignedZ(end);
    }

    // ====== 角度ユーティリティ ======
    // localEulerAngles.z は 0..360 表記なので、-180..180 に直す
    private float GetSignedZ()
    {
        return Mathf.DeltaAngle(0f, transform.localEulerAngles.z);
    }

    private void SetSignedZ(float signedDeg)
    {
        Vector3 e = transform.localEulerAngles;
        e.z = Normalize360(signedDeg);
        transform.localEulerAngles = e;
    }

    private float Normalize360(float signedDeg)
    {
        // -180..180 を 0..360 に
        float a = signedDeg % 360f;
        if (a < -180f) a += 360f;
        if (a > 180f) a -= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

}
