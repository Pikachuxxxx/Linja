using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class NinjaController : MonoBehaviour
{
    [Header("References")]
    public Transform visual;

    [Header("Movement")]
    public float maxSpeed = 7f;
    public float crouchSpeed = 3.5f;
    public float groundAccel = 90f;
    public float groundDecel = 130f;
    public float airAccel = 40f;
    public float airDecel = 30f;

    [Header("Jump")]
    public float jumpImpulse = 7.5f;
    public float doubleJumpImpulse = 6.2f;
    public int maxJumps = 2;
    public float doubleJumpCooldown = 0.15f;
    public float fallMultiplier = 3.5f;
    public float lowJumpMultiplier = 2.3f;

    [Header("Slide")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.45f;
    public float slideRecoveryTime = 0.25f;

    [Header("Boost")]
    public float boostImpulse = 12f;
    public float boostCooldown = 0.6f;

    [Header("Visual Tilt (VALUES ONLY)")]
    public float runTiltAngle = 18f;
    public float airTiltAngle = 24f;
    public float slideTiltAngle = 65f;
    public float tiltSmooth = 14f;

    [Header("Crouch")]
    public float crouchHeightFactor = 0.6f;
    public float crouchSmooth = 12f;
    public float crouchVisualDrop = 0.35f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.15f;

    Rigidbody rb;
    CapsuleCollider capsule;

    float moveInput;
    bool jumpQueued;
    bool crouchHeld;
    bool slideQueued;
    bool boostQueued;

    bool isGrounded;
    bool isSliding;

    int facingDir = 1;
    int slideDir;
    int jumpCount;

    float slideTimer;
    float slideRecoveryTimer;
    float boostTimer;
    float doubleJumpTimer;
    float crouchLerp;

    float originalCapsuleHeight;
    Vector3 originalCapsuleCenter;
    Vector3 visualBaseLocalPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        originalCapsuleHeight = capsule.height;
        originalCapsuleCenter = capsule.center;

        if (visual != null)
            visualBaseLocalPos = visual.localPosition;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        moveInput =
            (Keyboard.current.dKey.isPressed ? 1f : 0f) -
            (Keyboard.current.aKey.isPressed ? 1f : 0f);

        if (Mathf.Abs(moveInput) > 0.1f)
            facingDir = (int)Mathf.Sign(moveInput);

        crouchHeld = Keyboard.current.hKey.isPressed;

        if (Keyboard.current.sKey.wasPressedThisFrame)
            slideQueued = true;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpQueued = true;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            boostQueued = true;
    }

    void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            jumpCount = 0;
            doubleJumpTimer = 0f;
        }

        boostTimer -= Time.fixedDeltaTime;
        doubleJumpTimer -= Time.fixedDeltaTime;
        slideRecoveryTimer -= Time.fixedDeltaTime;

        UpdateCrouch();
        HandleSlide();
        HandleBoost();
        HandleHorizontal();
        HandleJump();
        ApplyBetterJumpPhysics();
        UpdateVisuals();
    }

    // ---------------- MOVEMENT ----------------

    void HandleHorizontal()
    {
        if (isSliding)
            return;

        float speed = Mathf.Lerp(maxSpeed, crouchSpeed, crouchLerp);
        float targetSpeed = moveInput * speed;

        float accel =
            Mathf.Abs(targetSpeed) > 0.01f
                ? (isGrounded ? groundAccel : airAccel)
                : (isGrounded ? groundDecel : airDecel);

        if (slideRecoveryTimer > 0f)
            accel *= 0.35f;

        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            accel * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(newX, rb.linearVelocity.y, 0f);
    }

    // ---------------- JUMP ----------------

    void HandleJump()
    {
        if (!jumpQueued)
            return;

        jumpQueued = false;

        if (jumpCount >= maxJumps)
            return;

        if (jumpCount > 0 && doubleJumpTimer > 0f)
            return;

        float impulse = jumpCount == 0 ? jumpImpulse : doubleJumpImpulse;

        jumpCount++;
        if (jumpCount > 1)
            doubleJumpTimer = doubleJumpCooldown;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 0f), 0f);
        rb.AddForce(Vector3.up * impulse, ForceMode.Impulse);
    }

    void ApplyBetterJumpPhysics()
    {
        if (isGrounded)
            return;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y *
                           (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f &&
                 Keyboard.current != null &&
                 !Keyboard.current.spaceKey.isPressed)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y *
                           (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // ---------------- CROUCH ----------------

    void UpdateCrouch()
    {
        float target = (crouchHeld || isSliding) ? 1f : 0f;
        crouchLerp = Mathf.MoveTowards(crouchLerp, target, crouchSmooth * Time.fixedDeltaTime);

        capsule.height = Mathf.Lerp(
            originalCapsuleHeight,
            originalCapsuleHeight * crouchHeightFactor,
            crouchLerp
        );

        capsule.center = Vector3.Lerp(
            originalCapsuleCenter,
            originalCapsuleCenter * crouchHeightFactor,
            crouchLerp
        );

        if (visual != null)
        {
            Vector3 v = visualBaseLocalPos;
            v.y -= crouchVisualDrop * crouchLerp;
            visual.localPosition = v;
        }
    }

    // ---------------- SLIDE ----------------

    void HandleSlide()
    {
        if (!isSliding && slideQueued)
        {
            slideQueued = false;

            if (!isGrounded || Mathf.Abs(rb.linearVelocity.x) < 1f)
                return;

            isSliding = true;
            slideTimer = slideDuration;
            slideDir = facingDir;

            rb.linearVelocity = new Vector3(slideDir * slideSpeed, rb.linearVelocity.y, 0f);
        }

        if (isSliding)
        {
            slideTimer -= Time.fixedDeltaTime;

            // CONSTANT SPEED SLIDE (FUN PRESERVED)
            rb.linearVelocity = new Vector3(slideDir * slideSpeed, rb.linearVelocity.y, 0f);

            if (slideTimer <= 0f)
            {
                isSliding = false;
                slideRecoveryTimer = slideRecoveryTime;
            }
        }
    }

    // ---------------- BOOST ----------------

    void HandleBoost()
    {
        if (!boostQueued)
            return;

        boostQueued = false;

        if (boostTimer > 0f)
            return;

        boostTimer = boostCooldown;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.AddForce(Vector3.right * facingDir * boostImpulse, ForceMode.Impulse);
    }

    // ---------------- VISUALS (RESTORED LOGIC) ----------------

    void UpdateVisuals()
    {
        if (visual == null)
            return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDir;
        visual.localScale = scale;

        float targetZ;

        if (isSliding)
        {
            targetZ = slideTiltAngle * slideDir;
        }
        else if (!isGrounded)
        {
            targetZ = -airTiltAngle * facingDir;
        }
        else
        {
            targetZ = -runTiltAngle * (Mathf.Abs(rb.linearVelocity.x) / maxSpeed) * facingDir;
        }

        visual.localRotation = Quaternion.Slerp(
            visual.localRotation,
            Quaternion.Euler(0f, 0f, targetZ),
            tiltSmooth * Time.fixedDeltaTime
        );
    }

    // ---------------- GROUND ----------------

    bool CheckGrounded()
    {
        Vector3 origin = transform.position;
        origin.y = capsule.bounds.min.y + 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance);
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 300, 210), "Ninja Controls");

        GUILayout.BeginArea(new Rect(20, 40, 280, 170));
        GUILayout.Label("Controls:");
        GUILayout.Label("A / D   : Move");
        GUILayout.Label("Space   : Jump / Double Jump");
        GUILayout.Label("H       : Crouch (Hold)");
        GUILayout.Label("S       : Slide");
        GUILayout.Label("E       : Boost");

        GUILayout.Space(10);

        GUILayout.Label("State:");
        GUILayout.Label($"Grounded : {isGrounded}");
        GUILayout.Label($"Sliding  : {isSliding}");
        GUILayout.Label($"JumpCnt  : {jumpCount}");
        GUILayout.Label($"Facing   : {(facingDir == 1 ? "Right" : "Left")}");
        GUILayout.EndArea();
    }

}
