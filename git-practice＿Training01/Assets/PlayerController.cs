using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(KnockbackHandler))]
public class PlayerController : MonoBehaviour, IKnockbackTarget
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float turnSpeed = 100f;

    private Rigidbody rb;
    private KnockbackHandler knockback;
    private bool isGrounded;

    private float currentInputSpeed = 0.0f;

    public float CurrentSpeed => currentInputSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        knockback = GetComponent<KnockbackHandler>();
    }

    void Update()
    {
        if (knockback.IsStunned)
        {
            currentInputSpeed = 0.0f;
            return;
        }

        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        currentInputSpeed = Mathf.Abs(moveInput) * moveSpeed;

        transform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);

        Vector3 moveDirection = transform.forward * moveInput * moveSpeed * Time.deltaTime;
        transform.position += moveDirection;

        bool jumpInput = Input.GetKeyDown(KeyCode.Space) ||
                         Input.GetMouseButtonDown(0) ||
                         Input.GetKeyDown(KeyCode.JoystickButton0);

        if (jumpInput && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
    }

    // IKnockbackTarget実装：スタン発生時のリセット処理
    public void OnKnockbackStunned(float duration)
    {
        currentInputSpeed = 0.0f;
    }
}