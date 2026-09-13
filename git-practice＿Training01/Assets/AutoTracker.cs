using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(KnockbackHandler))]
public class AutoTracker : MonoBehaviour, IKnockbackTarget
{
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

    [Header("落下回避の設定 (Fall Prevention)")]
    [Tooltip("足元の地面をチェックする前方の距離")]
    public float checkDistance = 1.2f;

    [Tooltip("地面と判定するチェック線の長さ（下方向）")]
    public float rayLength = 2.0f;

    [Tooltip("回避行動時に回転する角度（度）")]
    public float avoidTurnAngle = 120.0f;

    private float currentSpeed = 0.0f;
    private float stateTimer = 0.0f;
    private bool isTracking = false;
    private bool hasStarted = false;

    private bool isAvoiding = false;
    private Quaternion avoidTargetRotation;

    private bool isWandering = false;
    private float wanderTimer = 0.0f;
    private Quaternion wanderTargetRotation;

    private KnockbackHandler knockback;

    public float CurrentSpeed => currentSpeed;

    void Start()
    {
        knockback = GetComponent<KnockbackHandler>();
        stateTimer = initialDelay;
    }

    void Update()
    {
        if (target == null) return;

        if (knockback.IsStunned)
        {
            currentSpeed = 0.0f;
            return;
        }

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

        if (isAvoiding)
        {
            currentSpeed = 0.0f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, avoidTargetRotation, turnSpeed * 2.0f * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, avoidTargetRotation) < 5.0f && CheckGroundAhead(transform.forward))
            {
                isAvoiding = false;
            }
            return;
        }

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

        Quaternion candidateRotation = transform.rotation;

        if (isWandering)
        {
            wanderTimer -= Time.deltaTime;
            candidateRotation = Quaternion.RotateTowards(transform.rotation, wanderTargetRotation, turnSpeed * Time.deltaTime);

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
                candidateRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        Vector3 candidateForward = candidateRotation * Vector3.forward;

        if (!CheckGroundAhead(candidateForward))
        {
            isAvoiding = true;
            isWandering = false;
            currentSpeed = 0.0f;
            avoidTargetRotation = transform.rotation * Quaternion.Euler(0, avoidTurnAngle, 0);
            return;
        }

        transform.rotation = candidateRotation;

        if (currentSpeed > 0.0f)
        {
            Vector3 moveDirection = transform.forward * currentSpeed * Time.deltaTime;
            transform.position += moveDirection;
        }
    }

    public void OnKnockbackStunned(float duration)
    {
        currentSpeed = 0.0f;
        isAvoiding = false;
        isWandering = false;
    }

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

    private bool CheckGroundAhead(Vector3 forward)
    {
        Vector3 checkPosition = transform.position + forward * checkDistance;
        Ray ray = new Ray(checkPosition, Vector3.down);
        return Physics.Raycast(ray, rayLength);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = transform.position + transform.forward * checkDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * rayLength);
    }
}