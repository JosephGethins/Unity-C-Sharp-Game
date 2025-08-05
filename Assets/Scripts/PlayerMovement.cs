using UnityEngine;
using System.Collections;
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
    private bool canJump = true; // prevents holding jump from going when it shouldnt
    private bool isSnappingUpright = false;
    private bool isStunned = false; // disables input and movement


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
         if (isStunned) return; // skip everything if stunned

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

    private void TrySnapUpright()
    {
        float angle = transform.eulerAngles.z;

        // Convert 0–360 to -180–180 for easier logic
        if (angle > 180f)
            angle -= 360f;

        if (Mathf.Abs(angle) > 20f) // if tilted noticeably
        {
            StartCoroutine(SnapUprightRoutine());
        }
    }

    private IEnumerator SnapUprightRoutine()
    {
        isSnappingUpright = true;

        // Small hop to make it fun
        body.AddForce(Vector2.up * 1f, ForceMode2D.Impulse); // adjust force as needed

        yield return new WaitForSeconds(0.1f); // short delay to simulate hop

        // Snap rotation upright (cartoony fast)
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        yield return new WaitForSeconds(0.1f); // delay to prevent it running again instantly

        isSnappingUpright = false;
    }



    private void OnCollisionEnter2D(Collision2D collision)
    {
        anim.SetBool("IsJumping", false);

        if (collision.gameObject.tag == "Solid")
        {
            isGrounded = true;
            Debug.Log("Landed! isGrounded set to TRUE");

         float angle = transform.eulerAngles.z;
            if (angle > 180f) angle -= 360f;

            if (Mathf.Abs(angle) > 40f) // adjust threshold as needed
            {
                StartCoroutine(StunnedThenRecoverRoutine());
            }
        }
    }

    private IEnumerator StunnedThenRecoverRoutine()
    {
     isStunned = true;
      isSnappingUpright = true;

      moveInput = Vector2.zero;

      // Freeze motion (stops sliding)
      body.linearVelocity = Vector2.zero;
      body.angularVelocity = 0f;
      body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

       yield return new WaitForSeconds(0.5f); // stunned duration while laying sideways

       body.AddForce(Vector2.up * 2f, ForceMode2D.Impulse); // cartoony hop

      yield return new WaitForSeconds(0.1f);

       transform.rotation = Quaternion.Euler(0f, 0f, 0f); // snap upright

        yield return new WaitForSeconds(0.1f);

        // Restore movement
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        isSnappingUpright = false;
        isStunned = false;
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
