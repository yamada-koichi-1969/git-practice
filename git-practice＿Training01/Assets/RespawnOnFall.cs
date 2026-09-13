using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RespawnOnFall : MonoBehaviour
{
    [Header("復活の条件")]
    [Tooltip("このY座標より下に落ちたら復活させる")]
    public float fallThresholdY = -5.0f;

    [Header("復活位置")]
    [Tooltip("復活させる位置（未設定ならゲーム開始時の位置に戻す）")]
    public Transform respawnPoint;

    private Rigidbody rb;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
    }

    void Update()
    {
        if (transform.position.y < fallThresholdY)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        Vector3 position = respawnPoint != null ? respawnPoint.position : defaultPosition;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : defaultRotation;

        // 落下中の勢いを消してから戻す（戻した瞬間また落ちるのを防ぐ）
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = position;
        rb.rotation = rotation;
    }
}