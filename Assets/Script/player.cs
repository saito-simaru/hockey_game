using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;


[RequireComponent(typeof(Rigidbody2D))]
public class player : MonoBehaviour
{
    public float slowFactor = 0.5f;

    [Header("Restert")]
    public GameObject gm;

    [Header("Move")]
    public float moveSpeed = 5f;
    private float moveX;
    private Rigidbody2D rb;
    Vector2 worldDir; // (1, 0) 固定。ローカル回転の影響を受けない


    [Header("Identity")]
    public int playerId; // 0 or 1 で識別
    private PlayerInput playerInput;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        // 見た目の滑らかさ重視：物理は Interpolate 推奨
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        // PlayerInput の playerIndex をそのままIDに
        playerId = playerInput.playerIndex;

        //0度の時は物理エンジンによる回転を無効化
        rb.freezeRotation = true;

        // 移動方向は「常にワールドX」だが、プレイヤーIDによって変更
        worldDir = (playerId == 0) ? Vector2.right : -Vector2.right; // P1なら(1, 0)それ以外は(-1,0) 。ローカル回転の影響を受けない
        // 色分け（簡易）
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = (playerId == 0) ? new Color(0.2f, 0.7f, 1f) : new Color(1f, 0.4f, 0.4f);

    }

    public void OnRestart(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // ❌ 間違い: GameObject.Instance
        // ⭕ 正しい: GameManager.Instance
        gamemanager.Instance.OnRestart();
    }

    void FixedUpdate()
    {
        
        Vector2 delta = worldDir * (moveX * moveSpeed * Time.fixedDeltaTime);

        // ワールド座標で安全に移動 移動範囲を-2から2に指定
        rb.MovePosition(new Vector2(Math.Clamp(rb.position.x + delta.x, -2.45f, 2.45f) , rb.position.y + delta.y));

        //以下回転処理

        if (_isAnimating || _inputLocked || !_isHolding || _dir == 0) 
        {
            if (driveMode == RotationDrive.DynamicAngularVelocity) rb.angularVelocity = 0f;
            return;
        }

        // 押下中の手動回転
        float z = GetSignedZ();                            // -180..180
        float step = _dir * holdRotateSpeed * Time.fixedDeltaTime;
        float next = Mathf.Clamp(z + step, -angleLimit, +angleLimit);

        ApplyAngle(next);                                  // 物理方式で反映

    
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 衝突相手が動的なオブジェクトなら減速
        if (collision.collider.attachedRigidbody != null)
        {
            Rigidbody2D _rb = collision.rigidbody;
            if (_rb != null)
            {
                Debug.Log("slow");
                _rb.velocity *= slowFactor;
            }
        }
    }

    // Input System の "Player" Action Map と名前を合わせる
    // Input Action "Move" にバインドしたときに呼ばれる
    public void OnMove(InputAction.CallbackContext context)
    {
        //Debug.Log($"Player {playerId} Move Input Received");
        Vector2 input = context.ReadValue<Vector2>();
        moveX = input.x;
    }

    public enum RotationDrive { KinematicMoveRotation, DynamicAngularVelocity }

    [Header("駆動モード")]
    public RotationDrive driveMode = RotationDrive.KinematicMoveRotation;

    [Header("押している間の回転（FixedUpdate）")]
    [Tooltip("押下中の角速度 [deg/sec]")]
    public float holdRotateSpeed = 180f;

    [Tooltip("押下中の角度上限（±で適用）[deg]")]
    public float angleLimit = 70f;

    [Header("離してからの戻し")]
    [Tooltip("現在角度の負の値まで戻す所要時間 [sec]（等速）")]
    public float toNegativeDuration = 0.15f;

    [Tooltip("0°に戻すときの1フィックスドフレームあたりの回転量 [deg/step]")]
    public float zeroReturnStepDegPerFixed = 4f;

    [Header("DynamicAngularVelocity用（任意）")]
    [Tooltip("物理駆動時の最大角速度 [deg/sec]")]
    public float maxAngularSpeed = 720f;

    // 入力状態
    private int  _dir = 0;          // -1:右回り, +1:左回り, 0:停止
    private bool _isHolding = false;

    // アニメ/ロック
    private bool _isAnimating = false;
    private bool _inputLocked = false;
    private Coroutine _animCo = null;



    // ===== 新Input Systemコールバック =====
    public void OnFlick(InputAction.CallbackContext ctx)
    {
        if (_inputLocked) return; // 戻し中は完全無視

        if (ctx.started || ctx.performed)
        {
            float x = (ctx.control.valueType == typeof(Vector2))
                ? ctx.ReadValue<Vector2>().x
                : ctx.ReadValue<float>();

            int d = (x > 0.5f) ? +1 : (x < -0.5f ? -1 : 0);

            if (d != 0)
            {
                _dir = d;
                _isHolding = true;
                rb.freezeRotation = false;

                if (_isAnimating && _animCo != null)
                {
                    StopCoroutine(_animCo);
                    _isAnimating = false;
                }
            }
            else
            {
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

        // 0°に戻り切るまで入力を無視
        _inputLocked = true;

        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(ReturnSequenceFixed());


    }

    // ===== 戻しシーケンス（物理ステップ同期） =====
    private IEnumerator ReturnSequenceFixed()
    {
        _isAnimating = true;

        // Step1: 現在角度 -> 符号反転角（時間指定・等速）
        yield return RotateToAngleConstantSpeed_Fixed(-GetSignedZ(), toNegativeDuration);

        // Step2: そこから -> 0°（毎フィックスドフレーム一定角度ステップ）
        yield return RotateStepToZeroPerFixed(zeroReturnStepDegPerFixed);

        _isAnimating = false;
        _inputLocked = false;
        rb.freezeRotation = true;
    }

    // ---- 等速（所要時間で一定角速度）：FixedUpdate相当で進める
    private IEnumerator RotateToAngleConstantSpeed_Fixed(float targetSignedDeg, float duration)
    {
        float start = GetSignedZ();
        float end   = Mathf.Clamp(targetSignedDeg, -180f, 180f);
        float total = Mathf.Abs(Mathf.DeltaAngle(start, end));
        float speed = (duration > 0f) ? (total / duration) : float.PositiveInfinity; // [deg/sec]

        float t = 0f;
        while (true)
        {
            float current = GetSignedZ();
            float maxStep = speed * Time.fixedDeltaTime;
            float next = Mathf.MoveTowardsAngle(current, end, maxStep);

            // Kinematic: 直接角度指定、Dynamic: 角速度指定（最終到達でスナップ）
            if (driveMode == RotationDrive.KinematicMoveRotation)
            {
                ApplyAngle(next);
            }
            else // DynamicAngularVelocity
            {
                float required = Mathf.DeltaAngle(current, next) / Time.fixedDeltaTime; // [deg/sec]
                required = Mathf.Clamp(required, -maxAngularSpeed, maxAngularSpeed);
                rb.angularVelocity = required;

                // 最終到達（オーバーシュート防止にスナップ）
                if (Mathf.Approximately(Mathf.DeltaAngle(next, end), 0f))
                {
                    ApplyAngle(end);
                    rb.angularVelocity = 0f;
                }
            }

            t += Time.fixedDeltaTime;
            if (Mathf.Approximately(Mathf.DeltaAngle(next, end), 0f) || t >= duration) break;

            yield return new WaitForFixedUpdate();
        }

        ApplyAngle(end);
        rb.angularVelocity = 0f;
        yield return new WaitForFixedUpdate();
    }

    // ---- 毎フィックスドフレーム一定角度ステップで 0°へ
    private IEnumerator RotateStepToZeroPerFixed(float stepDegPerFixed)
    {
        float step = Mathf.Max(0.0001f, Mathf.Abs(stepDegPerFixed));

        while (true)
        {
            float z = GetSignedZ();
            if (Mathf.Approximately(z, 0f)) break;

            float dir = (z > 0f) ? -1f : +1f;
            float next = z + dir * step;

            // オーバーシュートしたら 0 へスナップ
            if (Mathf.Sign(z) != Mathf.Sign(next) || Mathf.Abs(next) < step)
                next = 0f;

            if (driveMode == RotationDrive.KinematicMoveRotation)
            {
                ApplyAngle(next);
            }
            else // DynamicAngularVelocity
            {
                float required = Mathf.DeltaAngle(z, next) / Time.fixedDeltaTime; // [deg/sec]
                required = Mathf.Clamp(required, -maxAngularSpeed, maxAngularSpeed);
                rb.angularVelocity = required;

                if (next == 0f)
                {
                    ApplyAngle(0f);
                    rb.angularVelocity = 0f;
                }
            }

            yield return new WaitForFixedUpdate();
        }

        ApplyAngle(0f);
        rb.angularVelocity = 0f;
        yield return new WaitForFixedUpdate();
    }

    // ===== 角度ユーティリティ（Rigidbody2Dベース, ワールドZ） =====
    private float GetSignedZ()
    {
        // Rigidbody2D.rotation は度数法。符号付き角に正規化
        return Mathf.DeltaAngle(0f, rb.rotation);
    }

    private void ApplyAngle(float signedDeg)
    {
        // モードごとに適用：MoveRotation は物理ステップで衝突解決に参加
        float absolute = Normalize360(signedDeg);
        rb.MoveRotation(absolute);             // Dynamic でも Kinematic でも可
        // 注意：Parentが回転している場合はワールド角になる点に留意
    }

    private static float Normalize360(float signedDeg)
    {
        float a = signedDeg % 360f;
        if (a < 0f) a += 360f;
        return a; // 0..360
    }
}

