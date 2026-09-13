using UnityEngine;

public class LandingEffect : MonoBehaviour
{
    [SerializeField] private GameObject landingEffectPrefab;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    private bool wasGrounded = true;

    void Update()
    {
        bool isGrounded = CheckGrounded();

        // 空中 → 接地 に変わった瞬間だけ発火
        if (!wasGrounded && isGrounded)
        {
            Instantiate(landingEffectPrefab, transform.position, Quaternion.identity);
        }

        wasGrounded = isGrounded;
    }

    private bool CheckGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
}