using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D body;
    private Animator anim;
    private InputSystem_Actions controls;
    private Vector2 moveInput;

    [SerializeField] public float baseJumpForce = 250f;
    [SerializeField] public float maxJumpForce = 750f;
    [SerializeField] public float maxChargeTime = 1f;
    [SerializeField] public float maxSideForce = 400f;

    private float chargeTime = 0f;
    private bool isCharging = false;
    private bool isGrounded;
    private bool canJump = true; // NEW: prevents holding jump from retriggering

    private void Awake()
    {
        //Grab references
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        controls = new InputSystem_Actions();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Jump.started += ctx =>
        {
            // Start charging only if grounded and jump was freshly pressed
            if (isGrounded && canJump)
            {
                anim.SetBool("IsCharging", true);
                isCharging = true;
                chargeTime = 0f;

                // Reset vertical velocity ONLY at jump start to avoid velocity problems
                Vector2 currentVel = body.linearVelocity;
                body.linearVelocity = new Vector2(currentVel.x, 0f);

                canJump = false; // block recharging until jump is released
            }
        };

        controls.Player.Jump.canceled += ctx =>
        {
            if (isCharging)
            {
                anim.SetBool("IsJumping", true);
                float jumpStrength = Mathf.Lerp(baseJumpForce, maxJumpForce, chargeTime / maxChargeTime);
                float sideStrength = Mathf.Lerp(0, maxSideForce, chargeTime / maxChargeTime);

                Vector2 impulse = new Vector2(moveInput.x * sideStrength, jumpStrength);
                body.AddForce(impulse, ForceMode2D.Impulse);

                isCharging = false;
                chargeTime = 0f;
                isGrounded = false;
            }

            // Reset charge flag and animator state
            canJump = true;
            anim.SetBool("IsCharging", false);
        };
    }

    private void Update()
    {
        // Flip sprite based on input direction
        if (moveInput.x > 0.1f)
            transform.localScale = new Vector3(2, 2, 2);
        else if (moveInput.x < -0.1f)
            transform.localScale = new Vector3(-2, 2, 2);

        if (isCharging)
        {
            chargeTime += Time.deltaTime;
            chargeTime = Mathf.Min(chargeTime, maxChargeTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        anim.SetBool("IsJumping", false);

        if (collision.gameObject.tag == "Solid")
        {
            isGrounded = true;
            Debug.Log("Landed! isGrounded set to TRUE");
        }
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Disable();
        }
    }
}
