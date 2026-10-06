using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Project.Audio;
using Game.Core.Interfaces;

namespace Project.Player
{
    public enum MaterialType
    {
        Paper = 0,
        Stone = 1,
        Rubber = 2
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMaterialController : MonoBehaviour, IHazardImmunity
    {
        [Header("Material State")]
        [SerializeField] private MaterialType currentMaterial = MaterialType.Paper;
        [SerializeField] private int maxTransformations = 5;
        [SerializeField] private int remainingTransformations = 5;

        [Header("Sprites")]
        [SerializeField] private Sprite paperSprite;
        [SerializeField] private Sprite stoneSprite;
        [SerializeField] private Sprite rubberSprite;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private ParticleSystem transformParticles;
        [SerializeField] private ParticleSystem bounceParticles;
        [SerializeField] private ParticleSystem doubleJumpParticles;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private LayerMask groundLayer = ~0;

        // Current physics tuning values
        [SerializeField] private float moveSpeed = 7.5f;
        [SerializeField] private float firstJumpVelocity = 11.0f;
        private float secondJumpVelocity = 15.5f;

        [Header("Rubber Progressive Bounce")]
        [SerializeField] private float rubberBounce1Velocity = 13.0f;
        [SerializeField] private float rubberBounce2Velocity = 18.5f;
        [SerializeField] private float rubberBounce3Velocity = 24.0f;
        [SerializeField] private float bounceComboWindow = 1.0f;

        [Header("Paper Wind Tuning")]
        [SerializeField] private float paperMaxWindLiftSpeed = 18.0f;
        [SerializeField] private float paperWindMultiplier = 1.2f;

        [Header("Rubber Wind Tuning")]
        [SerializeField] private float rubberWindMultiplier = 0.45f;
        [SerializeField] private float rubberMaxWindLiftSpeed = 6.5f;
        [SerializeField] private float rubberMaxFlyingHeightAboveVent = 6.5f; // Editable max height above wind vent base

        [Header("Rubber Ground Bounce Buffering")]
        [SerializeField] private float jumpBufferDuration = 0.25f;
        private float jumpBufferTimer = 0f;

        [Header("Transformation Timer")]
        [SerializeField] private float formDurationSeconds = 10f;
        private float formDurationTimer = 0f;

        private int bounceComboTier = 0; // 0 = first bounce (little), 1 = second bounce (higher), 2 = third bounce (even higher)
        private float bounceGroundedTimer = 0f;

        private float gravityScale = 1.1f;
        private float airControl = 1.0f;
        private float windMultiplier = 1.0f;
        private bool canDoubleJump = false;
        private bool hasDoubleJumped = false;
        private bool isGrounded = false;
        private float horizontalInput = 0f;
        private bool jumpPressed = false;
        private bool jumpHeld = false;

        [Header("Sprint & Config")]
        [SerializeField] private bool enableSprint = true;
        [SerializeField] private float sprintMultiplier = 1.45f;
        [SerializeField] private bool enableMaterialTransform = true;
        private bool isSprinting = false;

        public bool IsSprinting => isSprinting;
        public void SetSprintEnabled(bool enabled) => enableSprint = enabled;
        public void SetMaterialTransformEnabled(bool enabled) => enableMaterialTransform = enabled;
        public void SetSpeedAndJump(float speed, float jump) { moveSpeed = speed; firstJumpVelocity = jump; }

        // Visual squash & stretch
        private Vector3 userBaseScale = Vector3.one;
        private Vector3 baseScale = Vector3.one;
        private Vector3 currentVisualScale = Vector3.one;

        // Events
        public event Action<MaterialType, int, int> OnMaterialChanged;
        public event Action<string> OnPlayerNotice;
        public event Action<int, float> OnRubberBounceExecuted; // (tier 1..3, jumpVelocity)

        public MaterialType CurrentMaterial => currentMaterial;
        public int RemainingTransformations => remainingTransformations;
        public int MaxTransformations => maxTransformations;
        public float WindMultiplier => windMultiplier;
        public bool IsStone => currentMaterial == MaterialType.Stone;
        public int BounceComboTier => bounceComboTier;
        public float RubberBounce1Velocity => rubberBounce1Velocity;
        public float RubberBounce2Velocity => rubberBounce2Velocity;
        public float RubberBounce3Velocity => rubberBounce3Velocity;
        public float BounceComboWindow => bounceComboWindow;

        public float RubberWindMultiplier { get => rubberWindMultiplier; set => rubberWindMultiplier = value; }
        public float RubberMaxWindLiftSpeed { get => rubberMaxWindLiftSpeed; set => rubberMaxWindLiftSpeed = value; }
        public float RubberMaxFlyingHeightAboveVent { get => rubberMaxFlyingHeightAboveVent; set => rubberMaxFlyingHeightAboveVent = value; }

        public float PaperMaxWindLiftSpeed { get => paperMaxWindLiftSpeed; set => paperMaxWindLiftSpeed = value; }
        public float PaperWindMultiplier { get => paperWindMultiplier; set => paperWindMultiplier = value; }
        public bool IsGrounded => isGrounded;
        public bool CanDoubleJump => canDoubleJump;

        public void NotifyNotice(string notice)
        {
            OnPlayerNotice?.Invoke(notice);
        }

        public bool IsImmuneToHazard(string hazardType)
        {
            if (IsStone)
            {
                if (string.IsNullOrEmpty(hazardType) || hazardType.IndexOf("spike", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public float FormDurationSeconds { get => formDurationSeconds; set => formDurationSeconds = value; }
        public float FormDurationTimer => formDurationTimer;
        public bool HasFormTimerActive => currentMaterial != MaterialType.Paper && formDurationTimer > 0f;

        public void ResetBounceCombo()
        {
            bounceComboTier = 0;
            bounceGroundedTimer = 0f;
        }

        public void SetRubberBounceVelocities(float v1, float v2, float v3)
        {
            rubberBounce1Velocity = v1;
            rubberBounce2Velocity = v2;
            rubberBounce3Velocity = v3;
        }

        public void ResetToDefault(MaterialType defaultMat = MaterialType.Paper, int switches = 5)
        {
            maxTransformations = switches;
            remainingTransformations = switches;
            formDurationTimer = 0f;
            ResetBounceCombo();
            if (rubberBounce1Velocity < 10f)
            {
                rubberBounce1Velocity = 13.0f;
                rubberBounce2Velocity = 18.5f;
                rubberBounce3Velocity = 24.0f;
            }
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            LoadDefaultSpritesIfMissing();
            ApplyMaterial(defaultMat, playVfx: false);
            OnMaterialChanged?.Invoke(currentMaterial, remainingTransformations, maxTransformations);
        }

        public void LoadDefaultSpritesIfMissing()
        {
#if UNITY_EDITOR
            if (paperSprite == null)
            {
                paperSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/paper_form.png");
            }
            if (stoneSprite == null)
            {
                stoneSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/stone_form.png");
            }
            if (rubberSprite == null)
            {
                rubberSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/rubber_form.png");
            }
#endif
        }

        private void OnDisable()
        {
            currentVisualScale = userBaseScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = userBaseScale;
            }
        }

        private void Awake()
        {
            // The player's authored size lives on the controller; fall back to the transform.
            var owner = GetComponent<Game.Gameplay.Player.AutonomousPlayerController>();
            if (owner != null && owner.BaseTransformScale != Vector3.zero)
            {
                userBaseScale = owner.BaseTransformScale;
            }
            else if (transform.localScale != Vector3.zero)
            {
                userBaseScale = transform.localScale;
            }
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            if (rubberBounce1Velocity < 10f)
            {
                rubberBounce1Velocity = 13.0f;
                rubberBounce2Velocity = 18.5f;
                rubberBounce3Velocity = 24.0f;
            }

            LoadDefaultSpritesIfMissing();
            ApplyMaterial(currentMaterial, playVfx: false);
        }

        private void Start()
        {
            OnMaterialChanged?.Invoke(currentMaterial, remainingTransformations, maxTransformations);
        }

        private void Update()
        {
            ReadInput();

            if (isGrounded)
            {
                if (bounceComboTier > 0)
                {
                    bounceGroundedTimer += Time.deltaTime;
                    if (bounceGroundedTimer > bounceComboWindow)
                    {
                        ResetBounceCombo();
                    }
                }
            }
            else
            {
                bounceGroundedTimer = 0f;
            }

            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
                if (jumpBufferTimer < 0f) jumpBufferTimer = 0f;
            }

            // Temporary Transformation Expiration Timer (Stone and Rubber revert to Paper in 10s)
            if (currentMaterial != MaterialType.Paper && formDurationTimer > 0f)
            {
                formDurationTimer -= Time.deltaTime;
                if (formDurationTimer <= 0f)
                {
                    formDurationTimer = 0f;
                    RevertToDefaultPaperForm();
                }
            }

            // Squash & stretch recovery
            currentVisualScale = Vector3.Lerp(currentVisualScale, baseScale, Time.deltaTime * 10f);
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = currentVisualScale;
            }
        }

        private Game.Gameplay.Player.AutonomousPlayerController playerController;

        private void FixedUpdate()
        {
            if (playerController == null) playerController = GetComponent<Game.Gameplay.Player.AutonomousPlayerController>();
            if (playerController != null && playerController.HasReachedGoal) return;   // stopped at the exit door

            CheckGround();
            HandleMovement();
            HandleJump();
        }

        private void ReadInput()
        {
            float move = 0f;
            bool jumpDown = false;
            bool jumpHold = false;
            bool key1 = false, key2 = false, key3 = false, keyCycle = false;

            bool sprint = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += 1f;

                if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                {
                    jumpDown = true;
                }
                if (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)
                {
                    jumpHold = true;
                }

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) sprint = true;

                if (enableMaterialTransform)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) key1 = true;
                    if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) key2 = true;
                    if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) key3 = true;
                    if (kb.qKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) keyCycle = true;
                }
            }
#endif

            // Fallback for legacy input if needed
            try
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += 1f;
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) jumpDown = true;
                if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) jumpHold = true;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) sprint = true;

                if (enableMaterialTransform)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) key1 = true;
                    if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) key2 = true;
                    if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) key3 = true;
                    if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E)) keyCycle = true;
                }
            }
            catch {}

            isSprinting = enableSprint && sprint;
            horizontalInput = Mathf.Clamp(move, -1f, 1f);
            jumpHeld = jumpHold;
            if (jumpDown) jumpPressed = true;

            if (enableMaterialTransform)
            {
                if (key1) TryTransform(MaterialType.Paper);
                else if (key2) TryTransform(MaterialType.Stone);
                else if (key3) TryTransform(MaterialType.Rubber);
                else if (keyCycle)
                {
                    MaterialType nextMat = (MaterialType)(((int)currentMaterial + 1) % 3);
                    TryTransform(nextMat);
                }
            }
        }

        public bool TryTransform(MaterialType targetMaterial)
        {
            if (targetMaterial == currentMaterial)
            {
                // Already in this form, do not consume a charge
                return false;
            }

            if (remainingTransformations <= 0)
            {
                // No transformations left!
                OnPlayerNotice?.Invoke("NO TRANSFORMATIONS REMAINING!");
                ProceduralAudio.Instance?.PlayFail();
                return false;
            }

            remainingTransformations--;
            ApplyMaterial(targetMaterial, playVfx: true);
            if (targetMaterial != MaterialType.Paper)
            {
                formDurationTimer = formDurationSeconds;
            }
            else
            {
                formDurationTimer = 0f;
            }
            OnMaterialChanged?.Invoke(currentMaterial, remainingTransformations, maxTransformations);
            return true;
        }

        public void RevertToDefaultPaperForm()
        {
            if (currentMaterial == MaterialType.Paper) return;

            formDurationTimer = 0f;
            ApplyMaterial(MaterialType.Paper, playVfx: true);
            OnPlayerNotice?.Invoke("FORM DURATION EXPIRED! REVERTED TO PAPER");
            OnMaterialChanged?.Invoke(currentMaterial, remainingTransformations, maxTransformations);
        }

        private void ApplyMaterial(MaterialType mat, bool playVfx)
        {
            currentMaterial = mat;
            hasDoubleJumped = false;
            ResetBounceCombo();

            if (currentMaterial == MaterialType.Paper)
            {
                formDurationTimer = 0f;
            }

            switch (currentMaterial)
            {
                case MaterialType.Paper:
                    // Paper: White origami, light & floaty, full wind sensitivity
                    moveSpeed = 7.2f;
                    firstJumpVelocity = 9.5f;
                    gravityScale = 2.2f;
                    airControl = 1.0f;
                    windMultiplier = 1.0f;
                    canDoubleJump = false;
                    baseScale = userBaseScale; // every material keeps the player's size; only brief squash/stretch pops change it
                    rb.mass = 0.5f;

                    if (spriteRenderer != null)
                    {
                        if (paperSprite != null) spriteRenderer.sprite = paperSprite;
                        spriteRenderer.color = Color.white;
                    }
                    break;

                case MaterialType.Stone:
                    // Stone: Dark slate rock, heavy, falls fast, low jump, ignores wind completely
                    moveSpeed = 5.0f;
                    firstJumpVelocity = 5.0f; // Noticeably reduced jump height!
                    gravityScale = 4.2f;      // Falls visibly much faster!
                    airControl = 0.35f;
                    windMultiplier = 0.0f;    // Wind does NOT move stone!
                    canDoubleJump = false;
                    baseScale = userBaseScale; // every material keeps the player's size; only brief squash/stretch pops change it
                    rb.mass = 3.5f;

                    if (spriteRenderer != null)
                    {
                        if (stoneSprite != null) spriteRenderer.sprite = stoneSprite;
                        spriteRenderer.color = new Color(0.85f, 0.88f, 0.95f, 1f);
                    }
                    break;

                case MaterialType.Rubber:
                    // Rubber: Vibrant bouncy cyan/lime, 3-tier progressive bounce, minor wind lift
                    moveSpeed = 8.5f;
                    firstJumpVelocity = rubberBounce1Velocity;
                    secondJumpVelocity = rubberBounce2Velocity;
                    gravityScale = 2.2f;
                    airControl = 1.0f;
                    windMultiplier = rubberWindMultiplier; // Minor wind lift (editable)
                    canDoubleJump = false; // NO MID-AIR DOUBLE JUMP: Bounces only when touching ground!
                    baseScale = userBaseScale; // every material keeps the player's size; only brief squash/stretch pops change it
                    rb.mass = 1.0f;

                    if (spriteRenderer != null)
                    {
                        if (rubberSprite != null) spriteRenderer.sprite = rubberSprite;
                        spriteRenderer.color = Color.white;
                    }
                    break;
            }

            rb.gravityScale = gravityScale;

            if (playVfx)
            {
                TriggerTransformVFX();
                ProceduralAudio.Instance?.PlayTransform();
            }
        }

        private void TriggerTransformVFX()
        {
            currentVisualScale = baseScale * 1.4f; // Flash pop
            if (transformParticles != null)
            {
                var main = transformParticles.main;
                Color col = currentMaterial == MaterialType.Paper ? Color.white :
                            currentMaterial == MaterialType.Stone ? new Color(0.4f, 0.45f, 0.55f) :
                            new Color(0.0f, 1.0f, 0.8f);
                main.startColor = col;
                transformParticles.Play();
            }
        }

        private void CheckGround()
        {
            if (groundLayer.value == 0) groundLayer = ~0;
            Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.45f;
            var cols = Physics2D.OverlapCircleAll(checkPos, groundCheckRadius, groundLayer);
            Collider2D solidGround = null;
            foreach (var c in cols)
            {
                if (c == null || c.isTrigger || c.transform.IsChildOf(transform) || c.transform == transform) continue;
                solidGround = c;
                break;
            }

            if (rb != null && rb.linearVelocity.y > 0.5f)
            {
                // Moving upwards from jump - not grounded
                solidGround = null;
            }

            bool wasGrounded = isGrounded;
            isGrounded = solidGround != null;

            if (isGrounded)
            {
                hasDoubleJumped = false;

                // Just landed!
                if (!wasGrounded)
                {
                    OnLand(rb.linearVelocity.y);
                }
                else if (currentMaterial == MaterialType.Rubber && jumpHeld)
                {
                    jumpPressed = true;
                }
            }
        }

        private void OnLand(float downwardVelocity)
        {
            if (currentMaterial == MaterialType.Rubber)
            {
                bounceGroundedTimer = 0f;

                // Squash on landing
                currentVisualScale = Vector3.Scale(new Vector3(1.35f, 0.7f, 1f), userBaseScale);
                if (bounceParticles != null) bounceParticles.Play();
                ProceduralAudio.Instance?.PlayRubberBounce();

                // If falling fast, provide a natural rubber bounce recoil
                if (downwardVelocity < -4f)
                {
                    float bounceSpeed = Mathf.Abs(downwardVelocity) * 0.45f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceSpeed);
                }

                // If jump is held or buffered when touching ground, bounce immediately!
                if (jumpHeld || jumpBufferTimer > 0f)
                {
                    jumpPressed = true;
                }
            }
            else if (currentMaterial == MaterialType.Stone)
            {
                // Heavy stone thud
                currentVisualScale = Vector3.Scale(new Vector3(1.3f, 0.8f, 1f), userBaseScale);
                if (bounceParticles != null) bounceParticles.Play();
            }
            else
            {
                // Gentle paper landing
                currentVisualScale = Vector3.Scale(new Vector3(1.1f, 0.9f, 1f), userBaseScale);
            }
        }

        private void HandleMovement()
        {
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1.0f);
            float targetSpeed = horizontalInput * currentSpeed;
            float accel = isGrounded ? 12f : (12f * airControl);
            float newVx = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime * 10f);
            rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);

            // Sprite facing
            if (horizontalInput > 0.05f) spriteRenderer.flipX = false;
            else if (horizontalInput < -0.05f) spriteRenderer.flipX = true;
        }

        public void ExecuteRubberBounce()
        {
            float jumpVel;
            int executedTier = bounceComboTier + 1; // 1, 2, or 3

            switch (bounceComboTier)
            {
                case 0:
                    // 1st bounce: bounces a little
                    jumpVel = rubberBounce1Velocity;
                    currentVisualScale = Vector3.Scale(new Vector3(0.85f, 1.25f, 1f), userBaseScale);
                    ProceduralAudio.Instance?.PlayJump();
                    OnPlayerNotice?.Invoke("RUBBER BOUNCE 1!");
                    bounceComboTier = 1;
                    break;

                case 1:
                    // 2nd bounce: bounces a little higher
                    jumpVel = rubberBounce2Velocity;
                    currentVisualScale = Vector3.Scale(new Vector3(0.75f, 1.4f, 1f), userBaseScale);
                    if (bounceParticles != null) bounceParticles.Play();
                    ProceduralAudio.Instance?.PlayDoubleJump();
                    OnPlayerNotice?.Invoke("RUBBER BOUNCE 2!");
                    bounceComboTier = 2;
                    break;

                case 2:
                default:
                    // 3rd bounce: a little more higher (super bounce)
                    jumpVel = rubberBounce3Velocity;
                    currentVisualScale = Vector3.Scale(new Vector3(0.6f, 1.65f, 1f), userBaseScale);
                    if (doubleJumpParticles != null) doubleJumpParticles.Play();
                    ProceduralAudio.Instance?.PlayDoubleJump();
                    OnPlayerNotice?.Invoke("RUBBER SUPER BOUNCE 3!");
                    bounceComboTier = 0; // Cycles back to tier 1
                    break;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVel);
            bounceGroundedTimer = 0f;
            OnRubberBounceExecuted?.Invoke(executedTier, jumpVel);
        }

        private void HandleJump()
        {
            if (currentMaterial == MaterialType.Rubber)
            {
                // Rubber only bounces from touching the ground - NO mid-air double jump!
                if (!isGrounded)
                {
                    if (jumpPressed)
                    {
                        jumpBufferTimer = jumpBufferDuration;
                        jumpPressed = false;
                    }
                    return;
                }

                if (jumpPressed || jumpBufferTimer > 0f)
                {
                    jumpPressed = false;
                    jumpBufferTimer = 0f;
                    ExecuteRubberBounce();
                }
                return;
            }

            if (!jumpPressed) return;
            jumpPressed = false;

            if (isGrounded)
            {
                // First Jump (Paper / Stone)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, firstJumpVelocity);
                currentVisualScale = Vector3.Scale(new Vector3(0.75f, 1.35f, 1f), userBaseScale); // Stretch
                ProceduralAudio.Instance?.PlayJump();
            }
            else if (canDoubleJump && !hasDoubleJumped)
            {
                hasDoubleJumped = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, secondJumpVelocity);
                currentVisualScale = Vector3.Scale(new Vector3(0.65f, 1.5f, 1f), userBaseScale); // Super Stretch
                if (doubleJumpParticles != null) doubleJumpParticles.Play();
                ProceduralAudio.Instance?.PlayDoubleJump();
            }
        }

        public void ApplyWindForce(Vector2 force, float ventBaseY = float.MinValue)
        {
            if (currentMaterial == MaterialType.Stone) return; // Stone ignores wind completely!

            if (currentMaterial == MaterialType.Rubber)
            {
                if (rubberWindMultiplier <= 0f) return;

                // Check if Rubber has reached its editable max flying height above the vent
                if (ventBaseY > float.MinValue + 100f && rubberMaxFlyingHeightAboveVent > 0f)
                {
                    float currentHeight = transform.position.y - ventBaseY;
                    if (currentHeight >= rubberMaxFlyingHeightAboveVent)
                    {
                        // Hover at max flying height, dampening upward velocity
                        if (rb.linearVelocity.y > 0.5f)
                        {
                            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.MoveTowards(rb.linearVelocity.y, 0f, 15f * Time.fixedDeltaTime));
                        }
                        return;
                    }
                }

                // Rubber lifts up to its editable height with super heavy wind
                float targetVy = Mathf.Min(rb.linearVelocity.y + force.y * Time.fixedDeltaTime * rubberWindMultiplier, rubberMaxWindLiftSpeed);
                if (targetVy < 3.0f) targetVy = 3.0f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x + force.x * Time.fixedDeltaTime * rubberWindMultiplier, targetVy);
                return;
            }

            // Paper: super heavy wind rapidly sweeps and launches paper up!
            if (paperWindMultiplier <= 0f) return;
            float paperVy = Mathf.Min(rb.linearVelocity.y + force.y * Time.fixedDeltaTime * paperWindMultiplier, paperMaxWindLiftSpeed);
            if (paperVy < 6.0f) paperVy = 6.0f; // Rapid buoyant blast
            rb.linearVelocity = new Vector2(rb.linearVelocity.x + force.x * Time.fixedDeltaTime, paperVy);
        }

        public void ResetToCheckpoint(Vector3 position)
        {
            transform.position = position;
            if (rb != null)
            {
                rb.position = position;
                rb.linearVelocity = Vector2.zero;
            }
            currentVisualScale = baseScale;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.45f;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
        }
    }
}
