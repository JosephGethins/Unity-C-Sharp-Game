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
    private bool isFrozen = false; // For ice levels maybe and also for the room transition
    private Vector2 savedVelocity; // I want Celeste style "If jumping through a exit screen transition, contine the jump after the freeze"


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

        if (isFrozen) return;

        // Flip sprite based on input direction
        if (moveInput.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);

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

         float angle = transform.eulerAngles.z;
            if (angle > 180f) angle -= 360f;

            if (Mathf.Abs(angle) > 40f) // adjust threshold of the sprite if it didnt like go straight
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

        yield return new WaitForSeconds(1f); // stunned duration while laying sideways

        body.AddForce(Vector2.up * 2f, ForceMode2D.Impulse); // cartoony hop

        yield return new WaitForSeconds(0.1f);

        transform.rotation = Quaternion.Euler(0f, 0f, 0f); // snap upright

        yield return new WaitForSeconds(0.1f);

        // Restore rotation etc movement
        body.constraints = RigidbodyConstraints2D.None;

        isSnappingUpright = false;
        isStunned = false;
    }

    public void Freeze(bool freezeCheck)
    {
        isFrozen = freezeCheck;

        if (freezeCheck == true)
        {
            savedVelocity = body.linearVelocity; // Save my current velocity
            body.linearVelocity = Vector2.zero; // Set it to zero so it looks like my player is frozen in time while screens change
            body.bodyType = RigidbodyType2D.Kinematic; // This was grabbed from online so explanation so I dont forget below:
            // Kinematic stops gravity and physics from interfering while the player is frozen mid-air. 
            // If you just set velocity = 0 and freeze movement, Unity’s gravity will pull them down unless you disable physics.
        }
        else
        {
            body.bodyType = RigidbodyType2D.Dynamic; // Opposite of kinematic (Gives player all their physics back)
            body.linearVelocity = savedVelocity;
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
