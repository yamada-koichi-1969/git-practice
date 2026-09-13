using UnityEngine;

public class EdgeGuard : MonoBehaviour
{
    [SerializeField] private float lookAheadDistance = 0.5f;
    [SerializeField] private float raycastLength = 1.5f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude < 0.01f) return;

        // ノックバックのような高速移動時は、速度に応じて先読み距離を伸ばす
        float dynamicLookAhead = Mathf.Max(
            lookAheadDistance,
            horizontalVelocity.magnitude * Time.fixedDeltaTime * 2.0f
        );

        Vector3 checkPos = transform.position + horizontalVelocity.normalized * dynamicLookAhead;
        bool groundAhead = Physics.Raycast(checkPos, Vector3.down, raycastLength, groundLayer);

        if (!groundAhead)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
}