using UnityEngine;

public class AutoTracker : MonoBehaviour
{
    // ================================================
    // インスペクター（設定画面）で調整できる項目
    // ================================================

    [Header("ターゲットの設定")]
    [Tooltip("自動で追尾する対象（Cube_Playerなど）")]
    public Transform target;

    [Header("タイマーの設定")]
    [Tooltip("ゲーム開始から最初の追尾が始まるまでの待ち時間（秒）")]
    public float initialDelay = 2.0f;

    [Tooltip("追尾を続けて行う時間（秒）")]
    public float activeDuration = 5.0f;

    [Tooltip("追尾を一時停止する時間（秒）")]
    public float pauseDuration = 3.0f;

    [Header("行動の揺らぎ（脇道への逸れ）")]
    [Tooltip("追尾中にランダム行動（揺らぎ）が発生する確率（0%〜100%）")]
    [Range(0, 100)]
    public float wanderChance = 40.0f;

    [Tooltip("逸れて進む時間の長さ（秒）")]
    public float wanderDuration = 2.0f;

    [Header("移動・回転の設定")]
    [Tooltip("移動の最大スピード")]
    public float maxSpeed = 5.0f;

    [Tooltip("動き出しから最大スピードになるまでの時間（秒）")]
    public float accelerationTime = 0.5f;

    [Tooltip("最大スピードから完全に止まるまでの時間（秒）")]
    public float decelerationTime = 0.3f;

    [Tooltip("ターゲットの方向へ向く旋回スピード")]
    public float turnSpeed = 150.0f;

    [Header("ノックバック（衝突吹っ飛び）の設定")]
    [Tooltip("ノックバックで吹き飛ぶ力倍率")]
    public float knockbackMultiplier = 2.0f;

    [Tooltip("ノックバック後に制御を取り戻すまでの時間（秒）")]
    public float knockbackStunDuration = 0.4f;

    [Header("落下回避の設定 (Fall Prevention)")]
    [Tooltip("足元の地面をチェックする前方の距離")]
    public float checkDistance = 1.2f;

    [Tooltip("地面と判定するチェック線の長さ（下方向）")]
    public float rayLength = 2.0f;

    [Tooltip("回避行動時に回転する角度（度）")]
    public float avoidTurnAngle = 120.0f;

    // ================================================
    // 内部処理用の変数
    // ================================================
    private float currentSpeed = 0.0f;
    private float stateTimer = 0.0f;
    private bool isTracking = false;
    private bool hasStarted = false;

    // 落下回避用
    private bool isAvoiding = false;
    private Quaternion avoidTargetRotation;

    // 行動の揺らぎ（徘徊）用
    private bool isWandering = false;
    private float wanderTimer = 0.0f;
    private Quaternion wanderTargetRotation;

    // ノックバック制御用
    private Rigidbody rb;
    private float knockbackTimer = 0.0f;

    // 外部からスピードを取得するためのプロパティ
    public float CurrentSpeed => currentSpeed;

    // ------------------------------------------------
    // ゲーム開始時の処理
    // ------------------------------------------------
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stateTimer = initialDelay;
    }

    // ------------------------------------------------
    // 毎フレームの処理
    // ------------------------------------------------
    void Update()
    {
        if (target == null) return;

        // 【ノックバック中（硬直状態）の処理】
        if (knockbackTimer > 0.0f)
        {
            knockbackTimer -= Time.deltaTime;
            currentSpeed = 0.0f;
            return; // 吹き飛んでいる間は自力移動や落下回避を行わず物理挙動に任せる
        }

        // 【1. ゲーム開始直後のカウントダウン】
        if (!hasStarted)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0.0f)
            {
                hasStarted = true;
                StartNewTrackingPhase();
            }
            return;
        }

        // 【2. 「追尾/逸れ」と「停止」の定期切り替え】
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0.0f)
        {
            isTracking = !isTracking;

            if (isTracking)
            {
                StartNewTrackingPhase();
            }
            else
            {
                isWandering = false;
                stateTimer = pauseDuration;
            }
        }

        // 【3. 崖のチェック】
        bool isGroundAhead = CheckGroundAhead();

        if (!isGroundAhead && !isAvoiding)
        {
            isAvoiding = true;
            isWandering = false;
            avoidTargetRotation = transform.rotation * Quaternion.Euler(0, avoidTurnAngle, 0);
        }

        // 【4. 落下回避処理】
        if (isAvoiding)
        {
            currentSpeed = 0.0f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, avoidTargetRotation, turnSpeed * 2.0f * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, avoidTargetRotation) < 5.0f && isGroundAhead)
            {
                isAvoiding = false;
            }
            return;
        }

        // 【5. スピードの自動加減速処理】
        if (isTracking)
        {
            float accelRate = (accelerationTime > 0.0f) ? (maxSpeed / accelerationTime) : maxSpeed;
            currentSpeed += accelRate * Time.deltaTime;
            if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
        }
        else
        {
            float decelRate = (decelerationTime > 0.0f) ? (maxSpeed / decelerationTime) : maxSpeed;
            currentSpeed -= decelRate * Time.deltaTime;
            if (currentSpeed < 0.0f) currentSpeed = 0.0f;
        }

        // 【6. 回転・進行方向の処理】
        if (isWandering)
        {
            wanderTimer -= Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanderTargetRotation, turnSpeed * Time.deltaTime);

            if (wanderTimer <= 0.0f)
            {
                isWandering = false;
            }
        }
        else
        {
            Vector3 targetDirection = target.position - transform.position;
            targetDirection.y = 0.0f;

            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        // 【7. 移動処理】
        if (currentSpeed > 0.0f)
        {
            Vector3 moveDirection = transform.forward * currentSpeed * Time.deltaTime;
            transform.position += moveDirection;
        }
    }

    // ------------------------------------------------
    // 衝突判定（ノックバック計算）
    // ------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        float mySpeed = currentSpeed;
        float otherSpeed = 0.0f;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        AutoTracker otherTracker = collision.gameObject.GetComponent<AutoTracker>();

        if (player != null)
        {
            otherSpeed = player.CurrentSpeed;
        }
        else if (otherTracker != null)
        {
            otherSpeed = otherTracker.CurrentSpeed;
        }

        // 速度比較（自分が勝っている、または動いている場合）
        if (mySpeed >= otherSpeed && mySpeed > 0.1f)
        {
            Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
            pushDirection.y = 0.2f;

            float speedDifference = mySpeed - otherSpeed + 1.0f;
            float forceMagnitude = speedDifference * knockbackMultiplier;

            // 1. 敗者側（大きなノックバックを受ける）
            Rigidbody otherRb = collision.gameObject.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                otherRb.AddForce(pushDirection * forceMagnitude, ForceMode.Impulse);

                if (player != null)
                {
                    player.ApplyKnockbackStun(knockbackStunDuration);
                }
                else if (otherTracker != null)
                {
                    otherTracker.ApplyKnockbackStun(knockbackStunDuration);
                }
            }

            // 2. 勝者（自分）側（1/2 のノックバックを受ける）
            if (rb != null)
            {
                rb.AddForce(-pushDirection * (forceMagnitude * 0.5f), ForceMode.Impulse);
                ApplyKnockbackStun(knockbackStunDuration * 0.5f);
            }
        }
    }

    // ------------------------------------------------
    // ノックバック時の硬直（制御停止）を設定
    // ------------------------------------------------
    public void ApplyKnockbackStun(float duration)
    {
        knockbackTimer = duration;
        isAvoiding = false;
        isWandering = false;
    }

    // ------------------------------------------------
    // 新しい移動フェーズ（追尾か逸れか）を決定する
    // ------------------------------------------------
    private void StartNewTrackingPhase()
    {
        float roll = Random.Range(0.0f, 100.0f);

        if (roll < wanderChance)
        {
            isWandering = true;
            wanderTimer = wanderDuration;
            stateTimer = activeDuration;

            float randomAngle = Random.Range(60.0f, 140.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
            wanderTargetRotation = transform.rotation * Quaternion.Euler(0, randomAngle, 0);
        }
        else
        {
            isWandering = false;
            stateTimer = activeDuration;
        }
    }

    // ------------------------------------------------
    // 足元前方に地面があるか確認する処理（Raycast）
    // ------------------------------------------------
    private bool CheckGroundAhead()
    {
        Vector3 checkPosition = transform.position + transform.forward * checkDistance;
        Ray ray = new Ray(checkPosition, Vector3.down);
        return Physics.Raycast(ray, rayLength);
    }

    // ------------------------------------------------
    // シーン画面にデバッグ用の線を引く
    // ------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = transform.position + transform.forward * checkDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * rayLength);
    }
}