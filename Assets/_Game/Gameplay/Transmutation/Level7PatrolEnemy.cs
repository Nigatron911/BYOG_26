using System;
using UnityEngine;
using Game.Gameplay.Player;

namespace Game.Gameplay.Transmutation
{
    /// <summary>
    /// Level 7 patrolling enemy that moves between path 1 and path 2.
    /// In Enemy mode: lethal on contact with player (triggers level fail).
    /// In Inverted mode: transforms into an "Alibi" ally platform for 4 seconds,
    /// freezing in place and becoming a solid platform the player can climb and jump from.
    /// </summary>
    public class Level7PatrolEnemy : MonoBehaviour, ITransmutable
    {
        [Header("Patrol Settings")]
        [SerializeField] private Transform path1;
        [SerializeField] private Transform path2;
        [SerializeField] private float patrolSpeed = 2.8f;
        [SerializeField] private float defaultDuration = 3.0f;

        [Header("Alibi Boost Jump")]
        [SerializeField] private float alibiBoostJumpVelocity = 19.5f;
        [SerializeField] private float bounceAnimDuration = 0.25f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color enemyColor = new Color(0.85f, 0.15f, 0.25f, 1.0f);
        [SerializeField] private Color alibiColor = new Color(0.98f, 0.85f, 0.25f, 1.0f);

        [Header("Colliders")]
        [SerializeField] private Collider2D lethalCollider;
        [SerializeField] private BoxCollider2D platformCollider;

        private Vector3 targetPos;
        private bool movingTowardsPath2 = true;
        private bool isInverted = false;
        private float remainingTimer = 0f;
        private float bounceAnimTimer = 0f;
        private Vector3 initialScale;
        private Vector3 initialSpawnPos;
        private Vector3 path1WorldPos;
        private Vector3 path2WorldPos;
        private bool hasCachedPathPositions = false;

        public bool IsInverted => isInverted;
        public float RemainingDuration => remainingTimer;
        public float BoostJumpVelocity => alibiBoostJumpVelocity;
        public string TransmutableName => "Alibi Sentinel";
        public Transform Path1 => path1;
        public Transform Path2 => path2;
        public BoxCollider2D PlatformCollider { get { if (platformCollider == null) EnsureColliders(); return platformCollider; } }
        public Collider2D LethalCollider { get { if (lethalCollider == null) EnsureColliders(); return lethalCollider; } }

        public event Action<bool> OnStateChanged;
        public event Action OnPlayerBoostJumped;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            initialScale = transform.localScale;
            initialSpawnPos = transform.position;

            FindPathMarkers();
            EnsureColliders();
            SetEnemyVisuals();
        }

        private void Start()
        {
            FindPathMarkers();
            if (path1 != null && path2 != null)
            {
                CapturePathWorldPositions();
                targetPos = path2WorldPos;
                movingTowardsPath2 = true;
            }
        }

        public void Configure(Transform p1, Transform p2)
        {
            path1 = p1;
            path2 = p2;
            if (p1 != null) path1WorldPos = p1.position;
            if (p2 != null)
            {
                path2WorldPos = p2.position;
                targetPos = path2WorldPos;
                movingTowardsPath2 = true;
                hasCachedPathPositions = true;
            }
            initialSpawnPos = transform.position;
            EnsureColliders();
            DetachChildPathsIfPlaying();
        }

        private void DetachChildPathsIfPlaying()
        {
            if (Application.isPlaying)
            {
                if (path1 != null && path1.IsChildOf(transform))
                {
                    path1.SetParent(transform.parent, true);
                }
                if (path2 != null && path2.IsChildOf(transform))
                {
                    path2.SetParent(transform.parent, true);
                }
            }
        }

        private void FindPathMarkers()
        {
            // First check children
            if (path1 == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("path 1")) { path1 = child; break; }
                }
            }
            if (path2 == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("path 2")) { path2 = child; break; }
                }
            }

            // Fallback to global find
            if (path1 == null)
            {
                var p1 = GameObject.Find("path 1 for enemy ") ?? GameObject.Find("path 1 for enemy");
                if (p1 != null) path1 = p1.transform;
            }
            if (path2 == null)
            {
                var p2 = GameObject.Find("path 2 for enemy ") ?? GameObject.Find("path 2 for enemy");
                if (p2 != null) path2 = p2.transform;
            }

            CapturePathWorldPositions();
            DetachChildPathsIfPlaying();
        }

        private void CapturePathWorldPositions()
        {
            if (path1 != null && !hasCachedPathPositions)
            {
                path1WorldPos = path1.position;
            }
            if (path2 != null && !hasCachedPathPositions)
            {
                path2WorldPos = path2.position;
                targetPos = path2WorldPos;
                hasCachedPathPositions = true;
                if (initialSpawnPos == Vector3.zero) initialSpawnPos = transform.position;
            }
        }

        private void EnsureColliders()
        {
            // 1. Lethal trigger collider
            if (lethalCollider == null)
            {
                var existingCols = GetComponents<Collider2D>();
                foreach (var c in existingCols)
                {
                    if (c.isTrigger)
                    {
                        lethalCollider = c;
                        break;
                    }
                }
                if (lethalCollider == null)
                {
                    var circle = gameObject.AddComponent<CircleCollider2D>();
                    circle.isTrigger = true;
                    circle.radius = 0.65f;
                    lethalCollider = circle;
                }
            }

            // 2. Solid top platform collider (for climbing/standing when turned into alibi)
            if (platformCollider == null)
            {
                platformCollider = gameObject.AddComponent<BoxCollider2D>();
                platformCollider.size = new Vector2(1.8f, 0.4f);
                platformCollider.offset = new Vector2(0f, 0.35f);
                platformCollider.isTrigger = false;
                platformCollider.enabled = false; // Disabled while enemy
            }
        }

        public void Invert(float duration = 4.0f)
        {
            if (duration <= 0f) duration = defaultDuration;
            remainingTimer = duration;

            if (!isInverted)
            {
                isInverted = true;

                // Disable lethal hazard
                if (lethalCollider != null)
                {
                    lethalCollider.enabled = false;
                }

                // Enable solid standable platform
                if (platformCollider != null)
                {
                    platformCollider.enabled = true;
                }

                SetAlibiVisuals();
                OnStateChanged?.Invoke(true);
            }
        }

        public void Revert()
        {
            if (!isInverted) return;

            isInverted = false;
            remainingTimer = 0f;

            // Re-enable lethal hazard
            if (lethalCollider != null)
            {
                lethalCollider.enabled = true;
            }

            // Disable solid platform
            if (platformCollider != null)
            {
                platformCollider.enabled = false;
            }

            SetEnemyVisuals();
            OnStateChanged?.Invoke(false);
        }

        private void SetEnemyVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 5;
            }
            transform.localScale = initialScale;
        }

        private void SetAlibiVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = alibiColor;
                spriteRenderer.sortingOrder = 5;
            }
        }

        private void Update()
        {
            if (isInverted)
            {
                remainingTimer -= Time.deltaTime;

                // Squash-stretch visual bounce decay
                if (bounceAnimTimer > 0f)
                {
                    bounceAnimTimer -= Time.deltaTime;
                    float progress = 1f - Mathf.Clamp01(bounceAnimTimer / bounceAnimDuration);
                    float squashX = 1f + Mathf.Sin(progress * Mathf.PI) * 0.3f;
                    float squashY = 1f - Mathf.Sin(progress * Mathf.PI) * 0.3f;
                    transform.localScale = new Vector3(initialScale.x * squashX, initialScale.y * squashY, initialScale.z);
                }
                else
                {
                    transform.localScale = initialScale;
                }

                // Subtle holy/ally pulsation
                if (spriteRenderer != null)
                {
                    float pulse = 0.85f + Mathf.Sin(Time.time * 6f) * 0.15f;
                    Color c = alibiColor;
                    c.r = Mathf.Clamp01(c.r * pulse);
                    c.g = Mathf.Clamp01(c.g * pulse);

                    // Warning flash before reverting
                    if (remainingTimer <= 0.8f)
                    {
                        float flash = Mathf.PingPong(Time.time * 12f, 1f);
                        c = Color.Lerp(alibiColor, enemyColor, flash);
                    }
                    spriteRenderer.color = c;
                }

                if (remainingTimer <= 0f)
                {
                    Revert();
                }
            }
            else
            {
                // Patrol movement between path1 and path2
                ExecutePatrol();
            }
        }

        private void ExecutePatrol()
        {
            if (path1 == null || path2 == null)
            {
                FindPathMarkers();
                if (path1 == null || path2 == null) return;
            }

            Vector3 current = transform.position;
            Vector3 dest = movingTowardsPath2 ? path2WorldPos : path1WorldPos;

            // Move only horizontally along the path
            float step = patrolSpeed * Time.deltaTime;
            float newX = Mathf.MoveTowards(current.x, dest.x, step);

            // Subtle hovering bob around the enemy's own spawn height where the user kept it
            float baseY = initialSpawnPos != Vector3.zero ? initialSpawnPos.y : current.y;
            float hoverY = baseY + Mathf.Sin(Time.time * 4f) * 0.15f;

            transform.position = new Vector3(newX, hoverY, current.z);

            // Sprite facing
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = movingTowardsPath2;
            }

            // Reached waypoint
            if (Mathf.Abs(newX - dest.x) < 0.05f)
            {
                movingTowardsPath2 = !movingTowardsPath2;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isInverted)
            {
                CheckAlibiBoostJump(other.gameObject, null);
            }
            else
            {
                CheckPlayerKill(other.gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isInverted)
            {
                CheckAlibiBoostJump(other.gameObject, null);
            }
            else
            {
                CheckPlayerKill(other.gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isInverted)
            {
                CheckAlibiBoostJump(collision.gameObject, collision);
            }
            else
            {
                CheckPlayerKill(collision.gameObject);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (isInverted)
            {
                CheckAlibiBoostJump(collision.gameObject, collision);
            }
            else
            {
                CheckPlayerKill(collision.gameObject);
            }
        }

        private void CheckAlibiBoostJump(GameObject target, Collision2D collision = null)
        {
            if (target == null) return;
            if (!isInverted) return;

            var player = target.GetComponent<AutonomousPlayerController>()
                ?? target.GetComponentInParent<AutonomousPlayerController>();
            if (player == null || player.IsDead) return;

            var rb = target.GetComponent<Rigidbody2D>()
                ?? target.GetComponentInParent<Rigidbody2D>();
            if (rb == null) return;

            bool isAbove = false;
            if (collision != null && collision.contactCount > 0)
            {
                for (int i = 0; i < collision.contactCount; i++)
                {
                    if (collision.GetContact(i).point.y >= transform.position.y - 0.1f)
                    {
                        isAbove = true;
                        break;
                    }
                }
            }
            else
            {
                isAbove = player.transform.position.y >= transform.position.y - 0.2f;
            }

            if (isAbove)
            {
                // Only bounce if falling or low upward velocity to avoid runaway infinite velocity accumulation
                if (rb.linearVelocity.y <= 3.0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, alibiBoostJumpVelocity);
                    bounceAnimTimer = bounceAnimDuration;
                    OnPlayerBoostJumped?.Invoke();
                    Project.Audio.ProceduralAudio.Instance?.PlayDoubleJump();
                }
            }
        }

        private void CheckPlayerKill(GameObject target)
        {
            if (target == null) return;
            if (isInverted) return; // In alibi mode, not lethal

            var player = target.GetComponent<AutonomousPlayerController>();
            if (player == null) player = target.GetComponentInParent<AutonomousPlayerController>();

            if (player != null && !player.IsDead)
            {
                player.Kill("Shadow Patrol Sentinel");
            }
        }

        public void ResetToInitial(Vector3 spawnPos)
        {
            Revert();
            transform.position = spawnPos;
            if (hasCachedPathPositions)
            {
                targetPos = path2WorldPos;
            }
            movingTowardsPath2 = true;
        }

        public void ResetToSpawn()
        {
            ResetToInitial(initialSpawnPos != Vector3.zero ? initialSpawnPos : transform.position);
        }

        private void OnDisable()
        {
            if (isInverted)
            {
                Revert();
            }
        }
    }
}
