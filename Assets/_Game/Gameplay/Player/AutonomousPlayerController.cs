using UnityEngine;
using Game.Core.Events;
using Game.Core.Interfaces;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Autonomous 2D character controller.
    /// Walks forward automatically, climbs ladders, negotiates ramps, and monitors stuck timeouts.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 6 (Interfaces).
    /// </summary>
    public enum LocomotionMode
    {
        Autonomous,
        Manual,
        Material
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class AutonomousPlayerController : MonoBehaviour, IDamageable, IHazardImmunity
    {
        [Header("Locomotion Mode")]
        [SerializeField] private LocomotionMode locomotionMode = LocomotionMode.Autonomous;
        [SerializeField] private float jumpVelocity = 8.5f;
        [SerializeField] private float manualAcceleration = 40.0f;
        private PlayerGravityController gravityController;

        [Header("Manual Mode & Gravity Float")]
        [SerializeField] private float jumpBufferDuration = 0.25f;
        [SerializeField] private float coyoteTimeDuration = 0.15f;
        [SerializeField] private float moonFloatLiftSpeed = 4.2f;
        [SerializeField] private float moonMaxFallSpeed = 2.2f;
        [SerializeField] private float moonBuoyancyForce = 12f;

        // Input buffering and coyote timers
        private float cachedInputX = 0f;
        private float jumpBufferTimer = 0f;
        private float roofJumpBufferTimer = 0f;
        private float groundCoyoteTimer = 0f;
        private float roofCoyoteTimer = 0f;

        // Test input override for automated testing & validation
        private bool overrideTestInput = false;
        private float testInputX = 0f;
        private bool testJumpPressed = false;

        [Header("Locomotion Settings")]
        [SerializeField] private float walkSpeed = 7.0f;
        [SerializeField] private float climbSpeed = 6.5f;
        [SerializeField] private float slopeCheckDistance = 0.5f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Stuck Detection Settings")]
        [SerializeField] private float stuckTimeoutSeconds = 5.0f;
        [SerializeField] private float minimumProgressDelta = 0.1f;

        private Rigidbody2D rb;
        private Collider2D bodyCollider;
        private GameEvents events;
        private SpriteRenderer spriteRenderer;
        private Color defaultSpriteColor = Color.white;
        private Sprite defaultSprite;

        private bool isDead = false;
        private bool isClimbing = false;
        private IClimbable activeClimbable = null;
        private bool isPaused = false;
        private bool hasReachedGoal = false;
        private bool isSimulating = false;
        private Vector2 initialSpawnPosition;

        // Stuck tracking
        private float lastProgressX;
        private float stuckTimer = 0f;
        private float progressCheckTimer = 0f;

        public static string LastEventLog = "";
        public static System.Collections.Generic.List<string> TrajectoryLog = new System.Collections.Generic.List<string>();

        public bool IsDead => isDead;
        public bool HasReachedGoal => hasReachedGoal;
        public bool IsClimbing => isClimbing;
        public bool IsSimulating => isSimulating;
        public float WalkSpeed => walkSpeed;
        public float StuckTimer => stuckTimer;
        public float StuckTimeoutSeconds => stuckTimeoutSeconds;
        public float StuckProgress => Mathf.Clamp01(stuckTimer / stuckTimeoutSeconds);
        public LocomotionMode CurrentLocomotionMode => locomotionMode;
        public PlayerGravityController GravityController => gravityController;

        public void SetLocomotionMode(LocomotionMode mode)
        {
            locomotionMode = mode;
            if (mode == LocomotionMode.Manual || mode == LocomotionMode.Material)
            {
                stuckTimer = 0f;
            }
        }

        [Header("Scale Configuration")]
        [Tooltip("Base visual and gameplay scale multiplier. Set this in the Inspector to scale the character freely!")]
        [SerializeField] private Vector3 baseTransformScale = Vector3.one;

        public Vector3 BaseTransformScale => baseTransformScale;

        public void ResetVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultSpriteColor;
                if (defaultSprite != null) spriteRenderer.sprite = defaultSprite;
                spriteRenderer.transform.localScale = baseTransformScale;
            }
            transform.localScale = baseTransformScale;
            if (rb != null)
            {
                rb.mass = 1.0f;
                rb.gravityScale = 1.0f;
            }
        }

        public void SetGravityController(PlayerGravityController gc)
        {
            gravityController = gc;
        }

        public void Initialize(GameEvents gameEvents)
        {
            events = gameEvents;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) defaultSpriteColor = spriteRenderer.color;

            initialSpawnPosition = transform.position;
            ResetState(initialSpawnPosition);

            if (events != null)
            {
                events.SimulationStarted += OnSimulationStarted;
                events.SimulationStopped += OnSimulationStopped;
                events.LevelResetRequested += OnLevelResetRequested;
            }
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
                events.LevelResetRequested -= OnLevelResetRequested;
            }
        }

        private void OnLevelResetRequested()
        {
            ResetState(initialSpawnPosition);
        }

        private void OnSimulationStarted()
        {
            isSimulating = true;
            isDead = false;
            hasReachedGoal = false;
            isClimbing = false;
            activeClimbable = null;
            LastEventLog = "SIMULATION_ACTIVE";
            TrajectoryLog.Clear();
            lastProgressX = transform.position.x;
            stuckTimer = 0f;
            progressCheckTimer = 0f;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                if (gravityController == null || !gravityController.enabled)
                {
                    rb.gravityScale = 1f;
                }
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultSpriteColor;
            }
            Debug.Log("[AutonomousPlayerController] Simulation started! Player is walking.");
        }

        private void OnSimulationStopped()
        {
            isSimulating = false;
            ResetState(initialSpawnPosition);
            Debug.Log("[AutonomousPlayerController] Simulation stopped. Player reset to start.");
        }

        public void ResetState(Vector2 spawnPosition)
        {
            isSimulating = false;
            isDead = false;
            hasReachedGoal = false;
            isClimbing = false;
            activeClimbable = null;
            stuckTimer = 0f;
            progressCheckTimer = 0f;
            lastProgressX = spawnPosition.x;
            cachedInputX = 0f;
            jumpBufferTimer = 0f;
            roofJumpBufferTimer = 0f;
            groundCoyoteTimer = 0f;
            roofCoyoteTimer = 0f;
            transform.position = new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z);
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                if (gravityController == null || !gravityController.enabled)
                {
                    rb.gravityScale = 1f;
                }
                rb.position = spawnPosition;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultSpriteColor;
            }
        }

        public void SetSpawnPosition(Vector2 newSpawn)
        {
            initialSpawnPosition = newSpawn;
            ResetState(newSpawn);
        }

        private void Awake()
        {
            Physics2D.queriesStartInColliders = false;
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                defaultSpriteColor = spriteRenderer.color;
                defaultSprite = spriteRenderer.sprite;
            }
            if (gravityController == null) gravityController = GetComponent<PlayerGravityController>();
            if (baseTransformScale == Vector3.one && transform.localScale != Vector3.one && transform.localScale != Vector3.zero)
            {
                baseTransformScale = transform.localScale;
            }
            initialSpawnPosition = transform.position;
            lastProgressX = transform.position.x;
        }

        private void FixedUpdate()
        {
            // Once the exit is reached the player stops at the door (all locomotion modes).
            if (hasReachedGoal)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocity.y));
                return;
            }

            if (locomotionMode == LocomotionMode.Material)
            {
                // Material mode locomotion and jumping is fully owned by PlayerMaterialController
                return;
            }

            if (isDead || isPaused) return;

            if (Time.frameCount % 5 == 0)
            {
                TrajectoryLog.Add($"t={Time.time:F2}s pos={transform.position}");
                if (TrajectoryLog.Count > 100) TrajectoryLog.RemoveAt(0);
            }

            bool hasManualInput = Mathf.Abs(cachedInputX) > 0.05f || jumpBufferTimer > 0f;

            if (isClimbing && activeClimbable != null)
            {
                HandleClimbing();
            }
            else if (locomotionMode == LocomotionMode.Manual || (hasManualInput && !isSimulating))
            {
                HandleManualMovement();
            }
            else if (isSimulating)
            {
                HandleWalking();
            }
            else
            {
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }

            // Only monitor stuck condition while in autonomous mode simulating, not after reaching the goal
            if (!hasReachedGoal && locomotionMode == LocomotionMode.Autonomous && isSimulating)
            {
                CheckStuckCondition();
            }
        }

        [Header("Step & Slope Settings")]
        [SerializeField] private float maxStepHeight = 0.55f;
        [SerializeField] private float stepSearchDistance = 0.55f;

        private void Update()
        {
            if (isDead)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 90f); // Fallen pose
                return;
            }

            // Decrement input buffer & coyote timers
            if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
            if (roofJumpBufferTimer > 0f) roofJumpBufferTimer -= Time.deltaTime;
            if (groundCoyoteTimer > 0f) groundCoyoteTimer -= Time.deltaTime;
            if (roofCoyoteTimer > 0f) roofCoyoteTimer -= Time.deltaTime;

            // Capture frame-accurate keyboard inputs for manual testing and manual mode
            if (locomotionMode != LocomotionMode.Material && !isPaused)
            {
                ReadManualInputs();
            }

            // In Manual mode with GravityController, maintain exact gravity mode orientation
            if (locomotionMode == LocomotionMode.Manual && gravityController != null && gravityController.enabled)
            {
                if (gravityController.CurrentMode == GravityMode.InvertedRoof)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                }
                else if (gravityController.CurrentMode == GravityMode.Moon)
                {
                    // Gentle floating hover wobble
                    float floatAngle = Mathf.Sin(Time.time * 4f) * 3f;
                    transform.rotation = Quaternion.Euler(0f, 0f, floatAngle);
                }
                else
                {
                    transform.rotation = Quaternion.identity;
                }
                return;
            }

            if (isSimulating && !isPaused)
            {
                if (isClimbing)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 8f) * 4f);
                }
                else
                {
                    // Gentle walking bob/tilt
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 12f) * -3f);
                }
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
        }

        public void SetTestInputOverride(float inputX, bool jump = false)
        {
            overrideTestInput = true;
            testInputX = inputX;
            if (jump) testJumpPressed = true;
        }

        public void ClearTestInputOverride()
        {
            overrideTestInput = false;
            testInputX = 0f;
            testJumpPressed = false;
        }

        private void ReadManualInputs()
        {
            if (overrideTestInput)
            {
                cachedInputX = testInputX;
                if (testJumpPressed)
                {
                    jumpBufferTimer = jumpBufferDuration;
                    testJumpPressed = false;
                }
                return;
            }

            bool aHeld = false;
            bool dHeld = false;
            bool wDown = false;
            bool sDown = false;

            // Read New Input System Keyboard
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                aHeld = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                dHeld = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                wDown = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                sDown = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
            }

            // Read Gamepad
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.left.isPressed || pad.leftStick.left.isPressed) aHeld = true;
                if (pad.dpad.right.isPressed || pad.leftStick.right.isPressed) dHeld = true;
                if (pad.buttonSouth.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame) wDown = true;
                if (pad.dpad.down.wasPressedThisFrame) sDown = true;
            }

            // Cache horizontal movement
            cachedInputX = 0f;
            if (aHeld) cachedInputX -= 1f;
            if (dHeld) cachedInputX += 1f;

            // Buffer jump presses
            if (wDown)
            {
                jumpBufferTimer = jumpBufferDuration;
            }
            if (sDown)
            {
                roofJumpBufferTimer = jumpBufferDuration;
            }
        }

        private void HandleManualMovement()
        {
            GravityMode currentGravity = gravityController != null && gravityController.enabled
                ? gravityController.CurrentMode
                : GravityMode.Earth;

            bool isGrounded = CheckSurfaceGrounded(Vector2.down);
            bool isOnRoof = CheckSurfaceGrounded(Vector2.up);

            if (isGrounded) groundCoyoteTimer = coyoteTimeDuration;
            if (isOnRoof) roofCoyoteTimer = coyoteTimeDuration;

            float moveX = 0f;

            switch (currentGravity)
            {
                case GravityMode.Earth:
                {
                    // Earth Mode: A = Left, D = Right, W = Jump Up
                    moveX = cachedInputX;

                    if (jumpBufferTimer > 0f && (isGrounded || groundCoyoteTimer > 0f))
                    {
                        jumpBufferTimer = 0f;
                        groundCoyoteTimer = 0f;
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
                        rb.gravityScale = 1.0f;
                    }
                    else if (!isGrounded && rb.linearVelocity.y < 0f)
                    {
                        // Snappy, sudden fall in Earth mode
                        rb.gravityScale = 1.8f;
                    }
                    else
                    {
                        rb.gravityScale = 1.0f;
                    }
                    break;
                }

                case GravityMode.Moon:
                {
                    // Moon Mode: Floats! A = Left, D = Right, W = Float Jump / Air Thrust
                    moveX = cachedInputX;

                    // 1. Automatic float off ground: if touching ground or low altitude, gently float up!
                    if (isGrounded || transform.position.y < -1.8f)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, moonFloatLiftSpeed));
                    }

                    // 2. Upward float boost on W jump: can be pressed in mid-air or on ground
                    if (jumpBufferTimer > 0f)
                    {
                        jumpBufferTimer = 0f;
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y + 4.2f, 6.8f));
                    }

                    // 3. Gentle float fall-speed clamping (astronaut low-g float)
                    if (rb.linearVelocity.y < -moonMaxFallSpeed)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -moonMaxFallSpeed);
                    }

                    // 4. Hover buoyancy force keeping the player comfortably airborne above hazards
                    if (transform.position.y < 4.5f)
                    {
                        rb.AddForce(Vector2.up * moonBuoyancyForce, ForceMode2D.Force);
                    }

                    // 5. Downward descent if S is pressed in Moon mode
                    if (roofJumpBufferTimer > 0f)
                    {
                        roofJumpBufferTimer = 0f;
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -3.5f);
                    }
                    break;
                }

                case GravityMode.InvertedRoof:
                {
                    // Inverted Roof Mode: upside down, feet on ceiling
                    // "a right d left and s will be jump"
                    if (cachedInputX < 0f) moveX = 1f;   // A key moves Right
                    if (cachedInputX > 0f) moveX = -1f;  // D key moves Left

                    // S key jumps down off roof
                    if (roofJumpBufferTimer > 0f && (isOnRoof || roofCoyoteTimer > 0f))
                    {
                        roofJumpBufferTimer = 0f;
                        roofCoyoteTimer = 0f;
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpVelocity);
                    }
                    break;
                }
            }

            // Smooth Horizontal Velocity
            float targetVelX = moveX * walkSpeed;
            float currentVelX = rb.linearVelocity.x;
            float newVelX = Mathf.MoveTowards(currentVelX, targetVelX, manualAcceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

            // Sprite Facing Direction
            if (spriteRenderer != null && Mathf.Abs(moveX) > 0.05f)
            {
                if (currentGravity != GravityMode.InvertedRoof)
                {
                    spriteRenderer.flipX = (moveX < 0f);
                }
                else
                {
                    // Inverted rotation: flipX true faces world right, false faces world left
                    spriteRenderer.flipX = (moveX > 0f);
                }
            }
        }

        public bool CheckSurfaceGrounded(Vector2 direction)
        {
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            float halfHeight = bodyCollider != null ? bodyCollider.bounds.extents.y : 1.25f;
            float halfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0.42f;
            Vector2 boxSize = new Vector2(halfWidth * 1.4f, 0.15f);
            Vector2 center = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
            Vector2 origin = center + direction * (halfHeight - 0.08f);
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, boxSize, 0f, direction, 0.20f, groundLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider != null && h.collider != bodyCollider && !h.collider.isTrigger)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleWalking()
        {
            // Ground & slope detection using CircleCast from feet
            Vector2 castOrigin = (Vector2)transform.position + Vector2.up * 0.2f;
            RaycastHit2D hit = Physics2D.CircleCast(castOrigin, 0.2f, Vector2.down, slopeCheckDistance + 0.1f, groundLayerMask);

            Vector2 moveVelocity = new Vector2(walkSpeed, rb.linearVelocity.y);

            if (hit.collider != null && hit.normal.y > 0.1f)
            {
                // Align movement along surface slope tangent smoothly
                Vector2 slopeDirection = new Vector2(hit.normal.y, -hit.normal.x);
                if (slopeDirection.x < 0) slopeDirection = -slopeDirection;
                moveVelocity = slopeDirection.normalized * walkSpeed;
            }

            // Step assist: effortlessly step over small platform bumps and box lips
            Vector2 footPos = (Vector2)transform.position;
            RaycastHit2D lowHit = Physics2D.Raycast(footPos + Vector2.up * 0.08f, Vector2.right, stepSearchDistance, groundLayerMask);
            if (lowHit.collider != null && !lowHit.collider.isTrigger)
            {
                RaycastHit2D highHit = Physics2D.Raycast(footPos + Vector2.up * (maxStepHeight + 0.05f), Vector2.right, stepSearchDistance, groundLayerMask);
                if (highHit.collider == null)
                {
                    // Clear above step - smoothly pop up onto it!
                    rb.position += new Vector2(0.06f, 0.15f);
                    moveVelocity.y = Mathf.Max(moveVelocity.y, 1.2f);
                }
            }

            rb.linearVelocity = moveVelocity;
        }

        private void HandleClimbing()
        {
            if (activeClimbable == null || (activeClimbable as Object) == null)
            {
                DismountLadder(true);
                return;
            }

            Bounds b = activeClimbable.GetBounds();
            float ladderCenterX = b.center.x;

            // Check if player reached near top elevation of ladder OR reached platform level near the top
            bool reachedTop = transform.position.y >= activeClimbable.TopElevation - 0.25f;
            if (!reachedTop && transform.position.y >= b.max.y - 0.8f)
            {
                Vector2 rightCheck = (Vector2)transform.position + new Vector2(0.5f, -0.1f);
                RaycastHit2D ledgeHit = Physics2D.Raycast(rightCheck, Vector2.down, 0.4f, groundLayerMask);
                if (ledgeHit.collider != null && !ledgeHit.collider.isTrigger && ledgeHit.normal.y > 0.7f && transform.position.y >= ledgeHit.point.y - 0.15f)
                {
                    reachedTop = true;
                }
            }

            if (reachedTop)
            {
                CompleteLadderDismount(b);
                return;
            }

            // Check jump input to dismount early
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer = 0f;
                float jumpDir = cachedInputX != 0f ? Mathf.Sign(cachedInputX) : 1f;
                DismountLadder(true);
                rb.linearVelocity = new Vector2(jumpDir * walkSpeed, jumpVelocity * 0.9f);
                return;
            }

            // Smoothly center player X towards ladder center to prevent drifting sideways
            float currentX = rb.position.x;
            float targetX = Mathf.MoveTowards(currentX, ladderCenterX, 6.0f * Time.fixedDeltaTime);
            rb.position = new Vector2(targetX, rb.position.y);

            // Determine vertical climb speed
            float climbRate = climbSpeed * activeClimbable.ClimbSpeedMultiplier;
            float verticalVel = climbRate; // Default for Autonomous mode: auto climb up

            if (locomotionMode == LocomotionMode.Manual)
            {
                bool upHeld = false;
                bool downHeld = false;
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    upHeld = kb.wKey.isPressed || kb.upArrowKey.isPressed;
                    downHeld = kb.sKey.isPressed || kb.downArrowKey.isPressed;
                }
                var pad = UnityEngine.InputSystem.Gamepad.current;
                if (pad != null)
                {
                    if (pad.dpad.up.isPressed || pad.leftStick.up.isPressed) upHeld = true;
                    if (pad.dpad.down.isPressed || pad.leftStick.down.isPressed) downHeld = true;
                }

                if (upHeld) verticalVel = climbRate;
                else if (downHeld) verticalVel = -climbRate;
                else verticalVel = 0f;
            }

            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, verticalVel);
        }

        private void CompleteLadderDismount(Bounds b)
        {
            isClimbing = false;
            activeClimbable = null;
            rb.gravityScale = 1f;
            // Pop smoothly forward onto the upper platform
            rb.position = new Vector2(b.center.x + 0.45f, rb.position.y + 0.1f);
            rb.linearVelocity = new Vector2(walkSpeed * 1.2f, 1.2f);
        }

        private void DismountLadder(bool restoreGravity = true)
        {
            isClimbing = false;
            activeClimbable = null;
            if (restoreGravity && rb != null)
            {
                rb.gravityScale = 1f;
            }
        }

        private void CheckStuckCondition()
        {
            // Climbing ladders is active vertical progress
            if (isClimbing)
            {
                stuckTimer = 0f;
                lastProgressX = transform.position.x;
                return;
            }

            progressCheckTimer += Time.fixedDeltaTime;

            if (progressCheckTimer >= 0.5f)
            {
                float deltaX = transform.position.x - lastProgressX;
                lastProgressX = transform.position.x;
                progressCheckTimer = 0f;

                if (deltaX < minimumProgressDelta)
                {
                    // Blocked or not progressing
                    stuckTimer += 0.5f;
                    if (stuckTimer >= stuckTimeoutSeconds)
                    {
                        Kill("Player stuck without a path for more than 5 seconds!");
                    }
                }
                else
                {
                    // Making progress
                    stuckTimer = 0f;
                }
            }
        }

        private Project.Player.PlayerMaterialController materialController;

        public bool IsImmuneToHazard(string hazardType)
        {
            if (materialController == null)
            {
                materialController = GetComponent<Project.Player.PlayerMaterialController>();
            }

            if (materialController != null && materialController.enabled && materialController.IsStone)
            {
                if (string.IsNullOrEmpty(hazardType) || hazardType.IndexOf("spike", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void Kill(string cause)
        {
            if (isDead || hasReachedGoal) return;

            if (IsImmuneToHazard(cause))
            {
                // Stone form deflects spikes
                materialController?.NotifyNotice("STONE DEFLECTS SPIKES!");
                return;
            }

            LastEventLog = $"KILLED: {cause} at {transform.position}";
            isDead = true;
            isSimulating = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            Debug.Log($"[AutonomousPlayerController] Player died: {cause}");

            if (events != null)
            {
                events.PublishPlayerDied(cause);
            }
        }

        public void ReachGoal()
        {
            if (hasReachedGoal || isDead) return;

            LastEventLog = $"REACHED_GOAL at {transform.position}";
            hasReachedGoal = true;
            if (rb != null) rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocity.y));
            Debug.Log("[AutonomousPlayerController] Goal reached! Player stops at the exit door.");

            if (events != null)
            {
                events.PublishLevelCompleted();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            var trans = other.GetComponent<Game.Gameplay.Transmutation.ITransmutable>() 
                ?? other.GetComponentInParent<Game.Gameplay.Transmutation.ITransmutable>();
            if (trans != null && trans.IsInverted)
            {
                // Inverted object is safe (e.g. trampoline, alibi, phase wall)
            }
            else
            {
                var hazard = other.GetComponent<Game.Gameplay.Combat.Hazard2D>() ?? other.GetComponentInParent<Game.Gameplay.Combat.Hazard2D>();
                if (hazard != null && hazard.enabled)
                {
                    if (IsImmuneToHazard(hazard.HazardName)) return;
                    Kill($"Fell into {hazard.HazardName.ToLower()}");
                    return;
                }
            }

            IClimbable climbable = other.GetComponent<IClimbable>() 
                ?? other.GetComponentInParent<IClimbable>() 
                ?? other.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
                IgnoreLadderSolidColliders(climbable, true);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;

            var trans = collision.gameObject.GetComponent<Game.Gameplay.Transmutation.ITransmutable>() 
                ?? collision.gameObject.GetComponentInParent<Game.Gameplay.Transmutation.ITransmutable>();
            if (trans != null && trans.IsInverted)
            {
                // Inverted object is safe (e.g. trampoline, alibi, phase wall)
            }
            else
            {
                var hazard = collision.gameObject.GetComponent<Game.Gameplay.Combat.Hazard2D>() 
                    ?? collision.gameObject.GetComponentInParent<Game.Gameplay.Combat.Hazard2D>();
                if (hazard != null && hazard.enabled)
                {
                    if (IsImmuneToHazard(hazard.HazardName)) return;
                    Kill($"Fell into {hazard.HazardName.ToLower()}");
                    return;
                }
            }

            IClimbable climbable = collision.gameObject.GetComponent<IClimbable>() 
                ?? collision.gameObject.GetComponentInParent<IClimbable>() 
                ?? collision.gameObject.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
                IgnoreLadderSolidColliders(climbable, true);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (isDead || isClimbing) return;

            var trans = collision.gameObject.GetComponent<Game.Gameplay.Transmutation.ITransmutable>() 
                ?? collision.gameObject.GetComponentInParent<Game.Gameplay.Transmutation.ITransmutable>();
            if (trans != null && trans.IsInverted)
            {
                // Inverted object is safe (e.g. trampoline, alibi, phase wall)
            }
            else
            {
                var hazard = collision.gameObject.GetComponent<Game.Gameplay.Combat.Hazard2D>() 
                    ?? collision.gameObject.GetComponentInParent<Game.Gameplay.Combat.Hazard2D>();
                if (hazard != null && hazard.enabled)
                {
                    if (IsImmuneToHazard(hazard.HazardName)) return;
                    Kill($"Fell into {hazard.HazardName.ToLower()}");
                    return;
                }
            }

            IClimbable climbable = collision.gameObject.GetComponent<IClimbable>() 
                ?? collision.gameObject.GetComponentInParent<IClimbable>() 
                ?? collision.gameObject.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
                IgnoreLadderSolidColliders(climbable, true);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isDead || isClimbing) return;

            IClimbable climbable = other.GetComponent<IClimbable>() 
                ?? other.GetComponentInParent<IClimbable>() 
                ?? other.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
                IgnoreLadderSolidColliders(climbable, true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            IClimbable climbable = other.GetComponent<IClimbable>() 
                ?? other.GetComponentInParent<IClimbable>() 
                ?? other.GetComponentInChildren<IClimbable>();
            if (climbable != null && climbable == activeClimbable)
            {
                Bounds b = climbable.GetBounds();
                if (transform.position.y >= b.max.y - 0.35f)
                {
                    CompleteLadderDismount(b);
                }
                else
                {
                    DismountLadder(true);
                }
            }
        }

        private void IgnoreLadderSolidColliders(IClimbable climbable, bool ignore)
        {
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (bodyCollider == null || climbable == null) return;

            var mb = climbable as MonoBehaviour;
            if (mb != null)
            {
                var cols = mb.GetComponentsInParent<Collider2D>();
                for (int i = 0; i < cols.Length; i++)
                {
                    if (!cols[i].isTrigger && cols[i] != bodyCollider)
                    {
                        Physics2D.IgnoreCollision(bodyCollider, cols[i], ignore);
                    }
                }
                var childCols = mb.GetComponentsInChildren<Collider2D>();
                for (int i = 0; i < childCols.Length; i++)
                {
                    if (!childCols[i].isTrigger && childCols[i] != bodyCollider)
                    {
                        Physics2D.IgnoreCollision(bodyCollider, childCols[i], ignore);
                    }
                }
            }
        }
    }
}
