using UnityEngine;
using Game.Gameplay.Player;

namespace Game.Presentation.Animation
{
    /// <summary>
    /// Presentation component that drives player character sprite animations and facing direction.
    /// Strictly adheres to Architecture Contract Section 1 (Presentation) and Section 14:
    /// Gameplay does not depend on animation. If this component or Animator is disabled,
    /// gameplay and physics continue uninterrupted.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerVisualAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private AutonomousPlayerController playerController;
        [SerializeField] private PlayerGravityController gravityController;

        [Header("Animation Tuning")]
        [SerializeField] private float speedThreshold = 0.15f;
        [SerializeField] private float airThreshold = 0.2f;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalVelParam = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsClimbingParam = Animator.StringToHash("IsClimbing");
        private static readonly int IsZeroGParam = Animator.StringToHash("IsZeroG");
        private static readonly int IsDeadParam = Animator.StringToHash("IsDead");

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (playerController == null) playerController = GetComponent<AutonomousPlayerController>();
            if (gravityController == null) gravityController = GetComponent<PlayerGravityController>();
        }

        private void Update()
        {
            EnsureReferences();
            if (spriteRenderer == null) return;

            // 1. Calculate movement velocity
            float vx = rb != null ? rb.linearVelocity.x : 0f;
            float vy = rb != null ? rb.linearVelocity.y : 0f;
            float speed = Mathf.Abs(vx);

            // 2. Query states from player controller
            bool isDead = playerController != null && playerController.IsDead;
            bool isClimbing = playerController != null && playerController.IsClimbing;
            bool isGrounded = playerController != null ? playerController.CheckSurfaceGrounded(Vector2.down) : (Mathf.Abs(vy) < airThreshold);
            bool isZeroG = gravityController != null && gravityController.enabled && gravityController.CurrentMode == GravityMode.Moon;

            // 3. Facing direction (flips sprite around its centered pivot with zero horizontal displacement)
            if (speed > speedThreshold && !isClimbing)
            {
                spriteRenderer.flipX = vx < -0.05f;
            }

            // 4. Update Animator parameters if present
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(SpeedParam, isClimbing ? Mathf.Abs(vy) : speed);
                animator.SetFloat(VerticalVelParam, vy);
                animator.SetBool(IsGroundedParam, isGrounded);
                animator.SetBool(IsClimbingParam, isClimbing);
                animator.SetBool(IsZeroGParam, isZeroG);
                animator.SetBool(IsDeadParam, isDead);
            }
        }
    }
}
