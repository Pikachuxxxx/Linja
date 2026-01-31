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

    // ================== POWER PROFILE ==================

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

    // ================== REFERENCES ==================

    [Header("Visual")]
    public Transform visual;
    public Renderer visualRenderer;

    // ================== MOVEMENT ==================

    [Header("Movement")]
    public float maxSpeed = 7f;
    public float groundAccel = 90f;
    public float groundDecel = 130f;
    public float airAccel = 40f;
    public float airDecel = 30f;

    // ================== JUMP ==================

    [Header("Jump")]
    public float jumpImpulse = 7.5f;
    public float doubleJumpImpulse = 6.2f;

    // ================== SLIDE ==================

    [Header("Slide")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.45f;

    // ================== DASH ==================

    [Header("Dash")]
    public float dashImpulse = 12f;
    public float dashCooldown = 0.6f;

    // ================== TILT (RESTORED) ==================

    [Header("Tilt")]
    public float runTiltAngle = 18f;
    public float airTiltAngle = 24f;
    public float slideTiltAngle = 65f;
    public float tiltSmooth = 14f;

    // ================== INTERNAL ==================

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

    // ================== UNITY ==================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        ApplyProfile(currentProfileIndex);
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // A/D + arrows
        moveInput =
            ((Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) ? 1f : 0f) -
            ((Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) ? 1f : 0f);

        if (Mathf.Abs(moveInput) > 0.1f)
            facingDir = (int)Mathf.Sign(moveInput);

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpQueued = true;

        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            slideQueued = true;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            dashQueued = true;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            invisQueued = true;

        // TEMP profile switch keys
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

        HandleHorizontal();
        HandleJump();
        HandleSlide();
        HandleDash();
        HandleInvisibility();
        UpdateVisuals();
    }

    // ================== PROFILE ==================

    public void ApplyProfile(int index)
    {
        if (profiles == null || profiles.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, profiles.Length - 1);
        currentProfileIndex = index;

        PowerProfile p = profiles[index];

        canDoubleJump = p.enableDoubleJump;
        canSlide = p.enableSlide;
        canDash = p.enableDash;
        canInvisibility = p.enableInvisibility;

        isInvisible = false;

        if (visualRenderer != null && p.normalMaterial != null)
            visualRenderer.material = p.normalMaterial;

        if (p.visibleModel != null)
            p.visibleModel.SetActive(true);

        if (p.invisibleModel != null)
            p.invisibleModel.SetActive(false);
    }

    // ================== MOVEMENT ==================

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

    // ================== JUMP ==================

    void HandleJump()
    {
        if (!jumpQueued)
            return;

        jumpQueued = false;

        if (jumpCount == 0)
        {
            jumpCount++;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            return;
        }

        if (jumpCount == 1 && canDoubleJump)
        {
            jumpCount++;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
            rb.AddForce(Vector3.up * doubleJumpImpulse, ForceMode.Impulse);
        }
    }

    // ================== SLIDE ==================

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
            rb.linearVelocity = new Vector3(slideDir * slideSpeed, rb.linearVelocity.y, 0f);

            if (slideTimer <= 0f)
                isSliding = false;
        }
    }

    // ================== DASH ==================

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

    // ================== INVISIBILITY ==================

    void HandleInvisibility()
    {
        if (!canInvisibility || !invisQueued)
            return;

        invisQueued = false;
        isInvisible = !isInvisible;

        PowerProfile p = profiles[currentProfileIndex];

        if (visualRenderer != null)
            visualRenderer.material = isInvisible ? p.invisibleMaterial : p.normalMaterial;

        if (p.visibleModel != null)
            p.visibleModel.SetActive(!isInvisible);

        if (p.invisibleModel != null)
            p.invisibleModel.SetActive(isInvisible);
    }

    // ================== VISUALS (TILT RESTORED) ==================

    void UpdateVisuals()
    {
        if (visual == null)
            return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDir;
        visual.localScale = scale;

        float targetZ;

        if (isSliding)
            targetZ = slideTiltAngle * slideDir;
        else if (!isGrounded)
            targetZ = -airTiltAngle * facingDir;
        else
            targetZ = -runTiltAngle * (Mathf.Abs(rb.linearVelocity.x) / maxSpeed) * facingDir;

        visual.localRotation = Quaternion.Slerp(
            visual.localRotation,
            Quaternion.Euler(0f, 0f, targetZ),
            tiltSmooth * Time.fixedDeltaTime
        );
    }

    // ================== GROUND ==================

    bool CheckGrounded()
    {
        Vector3 origin = transform.position;
        origin.y = capsule.bounds.min.y + 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.15f);
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