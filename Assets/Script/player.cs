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

        // 戻し中は回さない
        if (_isAnimating || _inputLocked || !_isHolding || _dir == 0) return;

        float z = GetSignedZ();
        z += _dir * holdRotateSpeed * Time.fixedDeltaTime;
        z = Mathf.Clamp(z, -angleLimit, +angleLimit);
        SetSignedZ(z);
    
    }
    // Input System の "Player" Action Map と名前を合わせる
    // Input Action "Move" にバインドしたときに呼ばれる
    public void OnMove(InputAction.CallbackContext context)
    {
        Debug.Log($"Player {playerId} Move Input Received");
        Vector2 input = context.ReadValue<Vector2>();
        moveX = input.x;
    }


    [Header("押している間の回転（FixedUpdate）")]
    [Tooltip("押下中の角速度 [deg/sec]")]
    public float holdRotateSpeed = 180f;

    [Tooltip("押下中の角度上限（±で適用）[deg]")]
    public float angleLimit = 70f;

    [Header("離してからの戻し")]
    [Tooltip("現在角度の負の値まで戻す所要時間 [sec]（等速）")]
    public float toNegativeDuration = 0.15f;

    [Tooltip("0°に戻すときの1フレームあたりの回転量 [deg/frame]")]
    public float zeroReturnStepDeg = 4f;   // フレーム依存：毎フレーム一定値で回す

    // 入力状態
    private int  _dir = 0;          // -1:右回り(時計), +1:左回り(反時計), 0:停止
    private bool _isHolding = false;

    // アニメーション／入力ロック
    private bool _isAnimating = false;     // 何らかの戻しアニメ中（回転はスクリプト制御）
    private bool _inputLocked = false;     // 0°に戻り切るまで入力を無視
    private Coroutine _animCo = null;

    /// <summary>
    /// 新Input System: PlayerInput(Behavior=SendMessages) から呼ばれる
    /// Action名は "Flick" を想定
    /// </summary>
/// <summary>
    /// 新Input Systemのコールバック（PlayerInput: Send Messages）
    /// Action名 "Flick" を想定
    /// </summary>
    public void OnFlick(InputAction.CallbackContext ctx)
    {
        // 戻しシーケンス中は完全に無視
        if (_inputLocked) return;

        // 押下中は値を読む（Axis / Vector2 / Button どれでもOK）
        if (ctx.started || ctx.performed)
        {
            float x;

            x = ctx.ReadValue<float>();

            int d = (x > 0.5f) ? +1 : (x < -0.5f ? -1 : 0);

            if (d != 0)
            {
                _dir = d;
                _isHolding = true;

                // 押し直しで戻し中なら中断（ただし今回はロックで入らない運用）
                // if (_isAnimating && _animCo != null)
                // {
                //     StopCoroutine(_animCo);
                //     _isAnimating = false;
                // }
            }
            else
            {
                // Axisが0へ戻った＝離した
                HandleRelease();
            }
        }
        else if (ctx.canceled)
        {
            HandleRelease();
        }
    }

    private void HandleRelease()
    {
        if (!_isHolding) return;

        _isHolding = false;
        _dir = 0;

        // 離した瞬間から0°に戻るまで入力を無視
        _inputLocked = true;

        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(ReturnSequence());
    }



    // ───────── 戻しシーケンス ─────────
    // Step1: 現在角度 -> 負角度（時間指定・等速）
    // Step2: そこから -> 0°（毎フレーム一定角度ステップ）
    private IEnumerator ReturnSequence()
    {
        _isAnimating = true;

        // Step1：等速（所要時間で一定角速度）
        yield return RotateToAngleConstantSpeed(-GetSignedZ(), toNegativeDuration);

        // Step2：毎フレーム・一定角度ステップで 0° へ
        yield return RotateStepToZeroPerFrame(zeroReturnStepDeg);

        _isAnimating = false;
        _inputLocked = false; // 0°に到達したら解除
    }

    /// <summary>
    /// 指定時間で等速に目的角へ（フレーム時間に依存しない一定角速度）
    /// </summary>
    private IEnumerator RotateToAngleConstantSpeed(float targetSignedDeg, float duration)
    {
        float start = GetSignedZ();
        float end   = Mathf.Clamp(targetSignedDeg, -180f, 180f);

        float total = Mathf.Abs(Mathf.DeltaAngle(start, end));
        float speed = (duration > 0f) ? (total / duration) : float.PositiveInfinity; // [deg/sec]

        float t = 0f;
        while (true)
        {
            float current = GetSignedZ();
            float next = Mathf.MoveTowardsAngle(current, end, speed * Time.deltaTime);
            SetSignedZ(next);

            bool reached = Mathf.Approximately(Mathf.DeltaAngle(next, end), 0f);
            t += Time.deltaTime;
            if (reached || t >= duration) break;

            yield return null;
        }

        SetSignedZ(end);
    }

    /// <summary>
    /// 0°まで、毎フレーム一定角度ステップで回転（フレーム依存）
    /// </summary>
    private IEnumerator RotateStepToZeroPerFrame(float stepDegPerFrame)
    {
        // 0以下は無効にならないよう保護
        float step = Mathf.Max(0.0001f, Mathf.Abs(stepDegPerFrame));

        while (true)
        {
            float z = GetSignedZ();
            if (Mathf.Approximately(z, 0f)) break;

            // 0 へ向かう符号
            float dir = (z > 0f) ? -1f : +1f;

            // 次フレーム角度（オーバーシュートなら 0° にスナップ）
            float next = z + dir * step;
            if (Mathf.Sign(z) != Mathf.Sign(next) || Mathf.Abs(next) < step)
            {
                next = 0f;
            }

            SetSignedZ(next);
            yield return null; // 毎フレーム進める
        }
    }

    // ───────── 角度ユーティリティ（-180..180 を扱う）─────────
    private float GetSignedZ()
    {
        float z = transform.localEulerAngles.z;
        if (z > 180f) z -= 360f; // 0..360 → -180..180
        return z;
    }

    private void SetSignedZ(float signedDeg)
    {
        float z = (signedDeg % 360f + 360f) % 360f; // -∞..∞ → 0..360
        var e = transform.localEulerAngles;
        e.z = z;
        transform.localEulerAngles = e;
    }
}
