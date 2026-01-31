using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class NinjaController : MonoBehaviour
{
    public enum ColorProfile
    {
        Red,
        Blue,
        Orange,
        Purple
    }

    [System.Serializable]
    public struct PowerProfile
    {
        public string profileName;
        public ColorProfile colorProfile;

        [Header("Powers")]
        public bool enableDoubleJump;
        public bool enableSlide;
        public bool enableDash;
        public bool enableInvisibility;

        [Header("Visuals")]
        public Material normalMaterial;
        public Material invisibleMaterial;
        public GameObject visibleModel;
        public GameObject invisibleModel;
    }

    [Header("Profiles")]
    public PowerProfile[] profiles;
    public int currentProfileIndex = 0;

    bool canDoubleJump;
    bool canSlide;
    bool canDash;
    bool canInvisibility;

    [Header("Visual")]
    public Transform visual;
    public Renderer visualRenderer;

    [Header("Movement")]
    public float maxSpeed = 7f;
    public float groundAccel = 90f;
    public float groundDecel = 130f;
    public float airAccel = 40f;
    public float airDecel = 30f;

    [Header("Jump")]
    public float jumpImpulse = 7.5f;
    public float doubleJumpImpulse = 6.2f;

    [Header("Jump Feel")]
    public float coyoteTime = 0.1f;        // grace after leaving ground
    public float jumpBufferTime = 0.1f;    // grace before landing
    public float fallMultiplier = 3.5f;    // faster fall
    public float lowJumpMultiplier = 2.2f; // quick tap = short hop

    [Header("Jump Spin")]
    public float jumpSpinSpeed = 720f; // degrees per second

    [Header("Slide")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.45f;
    public float slideAccel = 60f;
    public float slideHeightFactor = 0.6f;
    public float slideSmooth = 8f;
    public float slideVisualDrop = 0.35f;

    [Header("Dash")]
    public float dashImpulse = 12f;
    public float dashCooldown = 0.6f;

    [Header("Tilt")]
    public float runTiltAngle = 18f;
    public float airTiltAngle = 24f;
    public float slideTiltAngle = 65f;
    public float tiltSmooth = 14f;

    Rigidbody rb;
    CapsuleCollider capsule;

    float moveInput;
    bool jumpQueued;
    bool slideQueued;
    bool dashQueued;
    bool invisQueued;

    bool isGrounded;
    bool isSliding;
    bool isInvisible;

    int facingDir = 1;
    int slideDir;
    int jumpCount;

    float slideTimer;
    float dashTimer;
    float slideLerp;

    float jumpSpinZ;
    bool jumpSpinning;
    float jumpSpinDir;

    float baseCapsuleHeight;
    Vector3 baseCapsuleCenter;
    Vector3 baseVisualLocalPos;

    float coyoteTimer;
float jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        baseCapsuleHeight = capsule.height;
        baseCapsuleCenter = capsule.center;

        if (visual != null)
            baseVisualLocalPos = visual.localPosition;

        ApplyProfile(currentProfileIndex);
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        moveInput =
            ((Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) ? 1f : 0f) -
            ((Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) ? 1f : 0f);

        if (Mathf.Abs(moveInput) > 0.1f)
            facingDir = (int)Mathf.Sign(moveInput);

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpQueued = true;

        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            slideQueued = true;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            dashQueued = true;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            invisQueued = true;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) ApplyProfile(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) ApplyProfile(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ApplyProfile(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) ApplyProfile(3);
    }

    void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (isGrounded)
            jumpCount = 0;

        dashTimer -= Time.fixedDeltaTime;

        // Timers
        coyoteTimer -= Time.fixedDeltaTime;
        jumpBufferTimer -= Time.fixedDeltaTime;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpCount = 0;
            jumpSpinZ = 0f;
            jumpSpinning = false;
        }

        if (jumpSpinning)
        {
            jumpSpinZ += -jumpSpinDir * jumpSpinSpeed * Time.fixedDeltaTime;

            if (Mathf.Abs(jumpSpinZ) >= 360f)
            {
                jumpSpinZ = 0f;
                jumpSpinning = false;
            }
        }

        HandleHorizontal();
        HandleJump();
        ApplyBetterJumpGravity();
        HandleSlide();
        HandleDash();
        HandleInvisibility();
        UpdateSlideHeight();
        UpdateVisuals();
    }

    void ApplyProfile(int index)
    {
        if (profiles == null || profiles.Length == 0)
            return;

        currentProfileIndex = Mathf.Clamp(index, 0, profiles.Length - 1);
        PowerProfile p = profiles[currentProfileIndex];

        canDoubleJump = p.enableDoubleJump;
        canSlide = p.enableSlide;
        canDash = p.enableDash;
        canInvisibility = p.enableInvisibility;

        isInvisible = false;

        if (visualRenderer && p.normalMaterial)
            visualRenderer.material = p.normalMaterial;

        if (p.visibleModel) p.visibleModel.SetActive(true);
        if (p.invisibleModel) p.invisibleModel.SetActive(false);
    }

    void HandleHorizontal()
    {
        if (isSliding)
            return;

        float targetSpeed = moveInput * maxSpeed;
        float accel = Mathf.Abs(targetSpeed) > 0.01f
            ? (isGrounded ? groundAccel : airAccel)
            : (isGrounded ? groundDecel : airDecel);

        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            accel * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(newX, rb.linearVelocity.y, 0f);
    }

    void StartJumpSpin()
    {
        jumpSpinZ = 0f;

        if (Mathf.Abs(moveInput) > 0.1f)
            jumpSpinDir = Mathf.Sign(moveInput);
        else
            jumpSpinDir = facingDir;

        jumpSpinning = true;
        
    }

    void HandleJump()
    {
        if (jumpBufferTimer <= 0f)
            return;

        // Ground or coyote jump
        if (coyoteTimer > 0f)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            jumpCount = 1;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            return;
        }

        // Double jump
        if (jumpCount == 1 && canDoubleJump)
        {
            jumpBufferTimer = 0f;
            jumpCount++;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
            rb.AddForce(Vector3.up * doubleJumpImpulse, ForceMode.Impulse);
            StartJumpSpin();
        }

    }

    void ApplyBetterJumpGravity()
    {
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


    void HandleSlide()
    {
        if (!canSlide)
            return;

        if (!isSliding && slideQueued && isGrounded)
        {
            slideQueued = false;
            isSliding = true;
            slideTimer = slideDuration;
            slideDir = facingDir;
        }

        if (isSliding)
        {
            slideTimer -= Time.fixedDeltaTime;

            float targetX = slideDir * slideSpeed;
            float newX = Mathf.MoveTowards(
                rb.linearVelocity.x,
                targetX,
                slideAccel * Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector3(newX, rb.linearVelocity.y, 0f);

            if (slideTimer <= 0f)
                isSliding = false;
        }
    }

    void HandleDash()
    {
        if (!canDash || !dashQueued)
            return;

        dashQueued = false;

        if (dashTimer > 0f)
            return;

        dashTimer = dashCooldown;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.AddForce(Vector3.right * facingDir * dashImpulse, ForceMode.Impulse);
    }

    void HandleInvisibility()
    {
        if (!canInvisibility || !invisQueued)
            return;

        invisQueued = false;
        isInvisible = !isInvisible;

        PowerProfile p = profiles[currentProfileIndex];

        if (visualRenderer)
            visualRenderer.material = isInvisible ? p.invisibleMaterial : p.normalMaterial;

        if (p.visibleModel) p.visibleModel.SetActive(!isInvisible);
        if (p.invisibleModel) p.invisibleModel.SetActive(isInvisible);
    }

    void UpdateSlideHeight()
    {
        float target = isSliding ? 1f : 0f;

        slideLerp = Mathf.MoveTowards(
            slideLerp,
            target,
            slideSmooth * Time.fixedDeltaTime
        );

        capsule.height = Mathf.Lerp(
            baseCapsuleHeight,
            baseCapsuleHeight * slideHeightFactor,
            slideLerp
        );

        capsule.center = Vector3.Lerp(
            baseCapsuleCenter,
            baseCapsuleCenter * slideHeightFactor,
            slideLerp
        );

        if (visual != null)
        {
            Vector3 v = baseVisualLocalPos;
            v.y -= slideVisualDrop * slideLerp;
            visual.localPosition = v;
        }
    }

    void UpdateVisuals()
    {
        if (!visual)
            return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDir;
        visual.localScale = scale;

        float lockedY = visual.localEulerAngles.y;

        float baseTiltZ =
            isSliding ? slideTiltAngle * slideDir :
            !isGrounded ? -airTiltAngle * facingDir :
            -runTiltAngle * (Mathf.Abs(rb.linearVelocity.x) / maxSpeed) * facingDir;

        float finalZ = baseTiltZ + jumpSpinZ;

        Quaternion targetRot = Quaternion.Euler(
            0f,
            lockedY,
            finalZ
        );

        visual.localRotation = Quaternion.Slerp(
            visual.localRotation,
            targetRot,
            tiltSmooth * Time.fixedDeltaTime
        );

    }

    bool CheckGrounded()
    {
        Vector3 origin = transform.position;
        origin.y = capsule.bounds.min.y + 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.15f);
    }

    public void Kill()
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        enabled = false;
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;

        isSliding = false;
        isInvisible = false;
        jumpCount = 0;
        slideTimer = 0f;
        dashTimer = 0f;

        ApplyProfile(currentProfileIndex);
        enabled = true;
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
        GUILayout.Label("Q       : Toggle Invisibility");
        GUILayout.Label("Press 1-4 to change profiles");
        GUILayout.Label("Current Profile: " + profiles[currentProfileIndex].profileName);
        GUILayout.EndArea();
    }
}
