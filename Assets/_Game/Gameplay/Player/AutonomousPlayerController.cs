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
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class AutonomousPlayerController : MonoBehaviour, IDamageable
    {
        [Header("Locomotion Settings")]
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float climbSpeed = 2.2f;
        [SerializeField] private float slopeCheckDistance = 0.5f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Stuck Detection Settings")]
        [SerializeField] private float stuckTimeoutSeconds = 5.0f;
        [SerializeField] private float minimumProgressDelta = 0.05f;

        private Rigidbody2D rb;
        private Collider2D bodyCollider;
        private GameEvents events;

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

        public bool IsDead => isDead;
        public bool IsSimulating => isSimulating;
        public float StuckTimer => stuckTimer;
        public float StuckTimeoutSeconds => stuckTimeoutSeconds;
        public float StuckProgress => Mathf.Clamp01(stuckTimer / stuckTimeoutSeconds);

        public void Initialize(GameEvents gameEvents)
        {
            events = gameEvents;
            initialSpawnPosition = transform.position;
            ResetState(initialSpawnPosition);

            if (events != null)
            {
                events.SimulationStarted += OnSimulationStarted;
                events.SimulationStopped += OnSimulationStopped;
            }
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
            }
        }

        private void OnSimulationStarted()
        {
            isSimulating = true;
            lastProgressX = transform.position.x;
            stuckTimer = 0f;
            progressCheckTimer = 0f;
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
            transform.position = new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z);
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                rb.position = spawnPosition;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void Awake()
        {
            Physics2D.queriesStartInColliders = false;
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            initialSpawnPosition = transform.position;
            lastProgressX = transform.position.x;
        }

        private void FixedUpdate()
        {
            if (!isSimulating || isDead || isPaused)
            {
                if (!isSimulating && rb != null && !isDead)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                return;
            }

            if (isClimbing && activeClimbable != null)
            {
                HandleClimbing();
            }
            else
            {
                HandleWalking();
            }

            // Only monitor stuck condition while solving the puzzle, not after reaching the goal
            if (!hasReachedGoal)
            {
                CheckStuckCondition();
            }
        }

        [Header("Step & Slope Settings")]
        [SerializeField] private float maxStepHeight = 0.45f;
        [SerializeField] private float stepSearchDistance = 0.50f;

        private void Update()
        {
            if (isDead)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 90f); // Fallen pose
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

        private void HandleWalking()
        {
            // Ground & slope detection using CircleCast
            Vector2 castOrigin = (Vector2)transform.position + Vector2.down * 0.3f;
            RaycastHit2D hit = Physics2D.CircleCast(castOrigin, 0.25f, Vector2.down, slopeCheckDistance, groundLayerMask);

            Vector2 moveVelocity = new Vector2(walkSpeed, rb.linearVelocity.y);

            if (hit.collider != null && hit.normal.y > 0.1f)
            {
                // Align movement along surface slope tangent smoothly
                Vector2 slopeDirection = new Vector2(hit.normal.y, -hit.normal.x);
                if (slopeDirection.x < 0) slopeDirection = -slopeDirection;
                moveVelocity = slopeDirection.normalized * walkSpeed;
            }

            // Step assist: effortlessly step over small platform bumps and box lips
            Vector2 footPos = (Vector2)transform.position + Vector2.down * 0.6f;
            RaycastHit2D lowHit = Physics2D.Raycast(footPos + Vector2.up * 0.05f, Vector2.right, stepSearchDistance, groundLayerMask);
            if (lowHit.collider != null && !lowHit.collider.isTrigger)
            {
                RaycastHit2D highHit = Physics2D.Raycast(footPos + Vector2.up * (maxStepHeight + 0.1f), Vector2.right, stepSearchDistance, groundLayerMask);
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
            if (activeClimbable == null)
            {
                isClimbing = false;
                rb.gravityScale = 1f;
                return;
            }

            // Check if player reached near top elevation of ladder OR reached platform level on the right
            bool reachedTop = transform.position.y >= activeClimbable.TopElevation - 0.25f;
            if (!reachedTop)
            {
                Vector2 rightCheck = (Vector2)transform.position + new Vector2(0.4f, -0.2f);
                RaycastHit2D ledgeHit = Physics2D.Raycast(rightCheck, Vector2.down, 0.4f, groundLayerMask);
                if (ledgeHit.collider != null && !ledgeHit.collider.isTrigger && ledgeHit.normal.y > 0.7f && transform.position.y >= ledgeHit.point.y - 0.1f)
                {
                    reachedTop = true;
                }
            }

            if (reachedTop)
            {
                isClimbing = false;
                activeClimbable = null;
                rb.gravityScale = 1f;
                rb.linearVelocity = new Vector2(walkSpeed * 1.5f, 2.0f);
                return;
            }

            // Climb up ladder vertically with forward bias to hug ladder rungs
            rb.gravityScale = 0f;
            float climbRate = climbSpeed * activeClimbable.ClimbSpeedMultiplier;
            rb.linearVelocity = new Vector2(0.35f, climbRate);
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

        public void Kill(string cause)
        {
            if (isDead || hasReachedGoal) return;

            isDead = true;
            isSimulating = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            Debug.Log($"[AutonomousPlayerController] Player died: {cause}");

            if (events != null)
            {
                events.PublishPlayerDied(cause);
            }
        }

        public void ReachGoal()
        {
            if (hasReachedGoal || isDead) return;

            hasReachedGoal = true;
            // The player continues walking forward outside the level during the fade transition
            Debug.Log("[AutonomousPlayerController] Goal reached! Player is walking outside the level...");

            if (events != null)
            {
                events.PublishLevelCompleted();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            var hazard = other.GetComponent<Game.Gameplay.Combat.Hazard2D>() ?? other.GetComponentInParent<Game.Gameplay.Combat.Hazard2D>();
            if (hazard != null)
            {
                Kill($"Fell into {hazard.HazardName.ToLower()}");
                return;
            }

            IClimbable climbable = other.GetComponent<IClimbable>() 
                ?? other.GetComponentInParent<IClimbable>() 
                ?? other.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;

            var hazard = collision.gameObject.GetComponent<Game.Gameplay.Combat.Hazard2D>() 
                ?? collision.gameObject.GetComponentInParent<Game.Gameplay.Combat.Hazard2D>();
            if (hazard != null)
            {
                Kill($"Fell into {hazard.HazardName.ToLower()}");
                return;
            }

            IClimbable climbable = collision.gameObject.GetComponent<IClimbable>() 
                ?? collision.gameObject.GetComponentInParent<IClimbable>() 
                ?? collision.gameObject.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                if (bodyCollider != null && collision.collider != null)
                {
                    Physics2D.IgnoreCollision(bodyCollider, collision.collider, true);
                }
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (isDead || isClimbing) return;

            var hazard = collision.gameObject.GetComponent<Game.Gameplay.Combat.Hazard2D>() 
                ?? collision.gameObject.GetComponentInParent<Game.Gameplay.Combat.Hazard2D>();
            if (hazard != null)
            {
                Kill($"Fell into {hazard.HazardName.ToLower()}");
                return;
            }

            IClimbable climbable = collision.gameObject.GetComponent<IClimbable>() 
                ?? collision.gameObject.GetComponentInParent<IClimbable>() 
                ?? collision.gameObject.GetComponentInChildren<IClimbable>();
            if (climbable != null)
            {
                if (bodyCollider != null && collision.collider != null)
                {
                    Physics2D.IgnoreCollision(bodyCollider, collision.collider, true);
                }
                isClimbing = true;
                activeClimbable = climbable;
                rb.gravityScale = 0f;
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
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            IClimbable climbable = other.GetComponent<IClimbable>() 
                ?? other.GetComponentInParent<IClimbable>() 
                ?? other.GetComponentInChildren<IClimbable>();
            if (climbable != null && climbable == activeClimbable)
            {
                isClimbing = false;
                activeClimbable = null;
                rb.gravityScale = 1f;
            }
        }
    }
}
