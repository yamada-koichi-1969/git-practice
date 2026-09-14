using UnityEngine;

// キャラクター同士の衝突判定・吹き飛ばし処理をまとめて担当するコンポーネント。
// PlayerController・AutoTrackerなど、IKnockbackTargetを実装したスクリプトと
// 同じGameObjectに付けて使う。
[RequireComponent(typeof(Rigidbody))]
public class KnockbackHandler : MonoBehaviour
{
    [Tooltip("ノックバックで吹き飛ぶ力倍率")]
    public float knockbackMultiplier = 2.0f;

    [Tooltip("ノックバックの力の最大値（これ以上は増えない。画面外まで飛ばされるのを防ぐ）")]
    public float maxKnockbackForce = 8.0f;

    [Tooltip("ノックバック後に制御を取り戻すまでの時間（秒）")]
    public float knockbackStunDuration = 0.4f;

    private Rigidbody rb;
    private IKnockbackTarget target;
    private float stunTimer = 0.0f;

    public bool IsStunned => stunTimer > 0.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        target = GetComponent<IKnockbackTarget>();

        if (target == null)
        {
            Debug.LogWarning($"{name} : KnockbackHandlerにはIKnockbackTargetを実装したコンポーネントが必要です。");
        }
    }

    void Update()
    {
        if (stunTimer > 0.0f)
        {
            stunTimer -= Time.deltaTime;
        }
    }

    public void ApplyStun(float duration)
    {
        stunTimer = duration;
        target?.OnKnockbackStunned(duration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (target == null) return;

        KnockbackHandler otherHandler = collision.gameObject.GetComponent<KnockbackHandler>();
        if (otherHandler == null || otherHandler.target == null) return;

        float mySpeed = target.CurrentSpeed;
        float otherSpeed = otherHandler.target.CurrentSpeed;

        // 速度で負けている、またはほぼ静止している場合は何もしない
        if (mySpeed < otherSpeed || mySpeed <= 0.1f) return;

        Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
        pushDirection.y = 0.2f;

        float speedDifference = mySpeed - otherSpeed + 1.0f;
        float forceMagnitude = speedDifference * knockbackMultiplier;

        // 力の最大値を制限する（画面外まで飛ばされるのを防ぐ）
        forceMagnitude = Mathf.Min(forceMagnitude, maxKnockbackForce);

        // 1. 敗者側（大きなノックバックを受ける）
        otherHandler.rb.AddForce(pushDirection * forceMagnitude, ForceMode.Impulse);
        otherHandler.ApplyStun(otherHandler.knockbackStunDuration);

        // 2. 勝者（自分）側（1/2 のノックバックを受ける。こちらも同じ上限の半分でキャップされる）
        float selfForceMagnitude = Mathf.Min(forceMagnitude * 0.5f, maxKnockbackForce * 0.5f);
        rb.AddForce(-pushDirection * selfForceMagnitude, ForceMode.Impulse);
        ApplyStun(knockbackStunDuration * 0.5f);
    }
}