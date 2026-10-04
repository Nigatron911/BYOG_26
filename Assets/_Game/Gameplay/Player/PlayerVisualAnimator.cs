using System;
using UnityEngine;
using Game.Gameplay.Player;
using Project.Player;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Presentation-layer component that animates the character using sliced spritesheets.
    /// Handles Idle, Run, Jump, Ladder Climbing, and Zero-G Floating animations.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 14 (Animation is Presentation).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerVisualAnimator : MonoBehaviour
    {
        public enum AnimationState
        {
            Idle,
            IdleFront,
            Run,
            Jump,
            Climb,
            ZeroG
        }

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] idleSprites = new Sprite[5];
        [SerializeField] private Sprite[] idleFrontSprites = new Sprite[5];
        [SerializeField] private Sprite[] runSprites = new Sprite[5];
        [SerializeField] private Sprite[] jumpSprites = new Sprite[5];
        [SerializeField] private Sprite[] climbSprites = new Sprite[5];
        [SerializeField] private Sprite[] zeroGSprites = new Sprite[5];

        [Header("Frame Rates (FPS)")]
        [SerializeField] private float idleFps = 6f;
        [SerializeField] private float runFps = 10f;
        [SerializeField] private float jumpFps = 8f;
        [SerializeField] private float climbFps = 8f;
        [SerializeField] private float zeroGFps = 6f;

        [Header("Dependencies")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private AutonomousPlayerController playerController;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerMaterialController materialController;

        private AnimationState currentState = AnimationState.Idle;
        private float frameTimer = 0f;
        private int currentFrameIndex = 0;
        private bool facingRight = true;

        public AnimationState CurrentState => currentState;
        public int CurrentFrameIndex => currentFrameIndex;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (playerController == null) playerController = GetComponent<AutonomousPlayerController>() ?? GetComponentInParent<AutonomousPlayerController>();
            if (rb == null) rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();
            if (materialController == null) materialController = GetComponent<PlayerMaterialController>() ?? GetComponentInParent<PlayerMaterialController>();

            LoadSpritesIfMissing();
        }

        public void LoadSpritesIfMissing()
        {
#if UNITY_EDITOR
            if (idleSprites == null || idleSprites.Length == 0 || idleSprites[0] == null)
            {
                idleSprites = LoadStripSprites("Assets/assets/SpriteSheet/character_idle_breath_strip.png");
            }
            if (idleFrontSprites == null || idleFrontSprites.Length == 0 || idleFrontSprites[0] == null)
            {
                idleFrontSprites = LoadStripSprites("Assets/assets/SpriteSheet/character_idle_front_noblink_strip.png");
            }
            if (runSprites == null || runSprites.Length == 0 || runSprites[0] == null)
            {
                runSprites = LoadStripSprites("Assets/assets/SpriteSheet/character_run_strip.png");
            }
            if (jumpSprites == null || jumpSprites.Length == 0 || jumpSprites[0] == null)
            {
                jumpSprites = LoadStripSprites("Assets/assets/SpriteSheet/character_jump_strip.png");
            }
            if (climbSprites == null || climbSprites.Length == 0 || climbSprites[0] == null)
            {
                climbSprites = LoadStripSprites("Assets/assets/SpriteSheet/character_climb_hires_strip.png");
            }
            if (zeroGSprites == null || zeroGSprites.Length == 0 || zeroGSprites[0] == null)
            {
                zeroGSprites = LoadStripSprites("Assets/assets/SpriteSheet/character_zerog_strip.png");
            }
#endif
        }

#if UNITY_EDITOR
        private Sprite[] LoadStripSprites(string assetPath)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (var a in assets)
            {
                if (a is Sprite s) list.Add(s);
            }
            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return list.ToArray();
        }
#endif

        public void SetSpriteArrays(Sprite[] idle, Sprite[] run, Sprite[] jump, Sprite[] climb, Sprite[] zeroG)
        {
            if (idle != null && idle.Length > 0) idleSprites = idle;
            if (run != null && run.Length > 0) runSprites = run;
            if (jump != null && jump.Length > 0) jumpSprites = jump;
            if (climb != null && climb.Length > 0) climbSprites = climb;
            if (zeroG != null && zeroG.Length > 0) zeroGSprites = zeroG;
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null) return;

            // In Material mode, if transformed into Stone or Rubber, allow materialController to show material form
            if (materialController != null && materialController.enabled && materialController.CurrentMaterial != MaterialType.Paper)
            {
                return;
            }

            AnimationState targetState = EvaluateTargetState();

            if (targetState != currentState)
            {
                currentState = targetState;
                currentFrameIndex = 0;
                frameTimer = 0f;
            }

            UpdateFacingDirection();
            UpdateFrameAnimation();
        }

        private AnimationState EvaluateTargetState()
        {
            if (playerController != null && playerController.IsDead)
            {
                return AnimationState.Idle;
            }

            // 1. Ladder climbing
            if (playerController != null && playerController.IsClimbing)
            {
                return AnimationState.Climb;
            }

            // 2. Moon gravity / Zero-G floating
            if (playerController != null && playerController.GravityController != null
                && playerController.GravityController.enabled
                && playerController.GravityController.CurrentMode == GravityMode.Moon)
            {
                return AnimationState.ZeroG;
            }

            // 3. In-air Jump / Fall
            bool isGrounded = IsGroundedCheck();
            if (!isGrounded)
            {
                return AnimationState.Jump;
            }

            // 4. Grounded movement (Run / Walk)
            float vx = rb != null ? rb.linearVelocity.x : 0f;
            if (Mathf.Abs(vx) > 0.15f)
            {
                return AnimationState.Run;
            }

            // 5. Default grounded Idle
            return AnimationState.Idle;
        }

        private bool IsGroundedCheck()
        {
            if (playerController != null)
            {
                // In inverted roof mode, ground is above
                if (playerController.GravityController != null && playerController.GravityController.enabled
                    && playerController.GravityController.CurrentMode == GravityMode.InvertedRoof)
                {
                    return playerController.CheckSurfaceGrounded(Vector2.up);
                }
                return playerController.CheckSurfaceGrounded(Vector2.down);
            }

            if (rb != null)
            {
                return Mathf.Abs(rb.linearVelocity.y) < 0.05f;
            }

            return true;
        }

        private void UpdateFacingDirection()
        {
            if (rb == null) return;

            float vx = rb.linearVelocity.x;
            if (vx > 0.1f)
            {
                facingRight = true;
            }
            else if (vx < -0.1f)
            {
                facingRight = false;
            }

            bool invertedRoof = playerController != null && playerController.GravityController != null
                && playerController.GravityController.enabled
                && playerController.GravityController.CurrentMode == GravityMode.InvertedRoof;

            if (!invertedRoof)
            {
                spriteRenderer.flipX = !facingRight;
            }
            else
            {
                // When upside down (rotation 180 or -1 scale), invert flip logic
                spriteRenderer.flipX = facingRight;
            }
        }

        private void UpdateFrameAnimation()
        {
            Sprite[] activeArray = GetActiveSpriteArray();
            if (activeArray == null || activeArray.Length == 0) return;

            float fps = GetFpsForState(currentState);
            float frameDuration = fps > 0f ? (1f / fps) : 0.2f;

            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % activeArray.Length;
            }

            if (currentFrameIndex >= activeArray.Length) currentFrameIndex = 0;

            Sprite s = activeArray[currentFrameIndex];
            if (s != null)
            {
                spriteRenderer.sprite = s;
            }
        }

        private Sprite[] GetActiveSpriteArray()
        {
            switch (currentState)
            {
                case AnimationState.Climb:
                    return (climbSprites != null && climbSprites.Length > 0) ? climbSprites : runSprites;
                case AnimationState.ZeroG:
                    return (zeroGSprites != null && zeroGSprites.Length > 0) ? zeroGSprites : jumpSprites;
                case AnimationState.Jump:
                    return (jumpSprites != null && jumpSprites.Length > 0) ? jumpSprites : idleSprites;
                case AnimationState.Run:
                    return (runSprites != null && runSprites.Length > 0) ? runSprites : idleSprites;
                case AnimationState.IdleFront:
                    return (idleFrontSprites != null && idleFrontSprites.Length > 0) ? idleFrontSprites : idleSprites;
                case AnimationState.Idle:
                default:
                    return idleSprites;
            }
        }

        private float GetFpsForState(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Run: return runFps;
                case AnimationState.Jump: return jumpFps;
                case AnimationState.Climb: return climbFps;
                case AnimationState.ZeroG: return zeroGFps;
                case AnimationState.IdleFront:
                case AnimationState.Idle:
                default:
                    return idleFps;
            }
        }
    }
}
