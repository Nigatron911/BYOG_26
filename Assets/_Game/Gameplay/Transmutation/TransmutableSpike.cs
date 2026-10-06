using System;
using UnityEngine;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Transmutation
{
    /// <summary>
    /// Spike hazard that can be transmuted into a springy, bouncy trampoline.
    /// In inverted mode, lethal hazard is disabled and contacts propel the player upward.
    /// Reverts back to deadly spike after duration (default 4 seconds).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TransmutableSpike : MonoBehaviour, ITransmutable
    {
        [Header("Trampoline Settings")]
        [SerializeField] private float bounceVelocity = 19.5f;
        [SerializeField] private float defaultDuration = 3.0f;
        [SerializeField] private Color trampolineColor = new Color(0.25f, 1.0f, 0.55f, 1.0f);
        [Tooltip("Optional sprite shown while transmuted into a trampoline.")]
        [SerializeField] private Sprite trampolineSprite;

        [Header("References")]
        [SerializeField] private Spike spikeScript;
        [SerializeField] private Hazard2D hazard;
        [SerializeField] private Collider2D spikeCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Color originalColor = Color.white;
        private Sprite originalSprite;
        private Vector3 originalScale = Vector3.one;
        private bool isInverted = false;
        private float remainingTimer = 0f;
        private float bounceAnimTimer = 0f;

        public bool IsInverted => isInverted;
        public float RemainingDuration => remainingTimer;
        public float BounceVelocity => bounceVelocity;
        public string TransmutableName => gameObject.name;
        public Spike SpikeScript { get { if (spikeScript == null) spikeScript = GetComponent<Spike>(); return spikeScript; } }
        public bool IsSpikeScriptEnabled => SpikeScript != null && SpikeScript.enabled;

        public event Action<bool> OnStateChanged;
        public event Action OnPlayerBounced;

        private void Awake()
        {
            if (spikeScript == null) spikeScript = GetComponent<Spike>();
            if (hazard == null) hazard = GetComponent<Hazard2D>();
            if (spikeCollider == null) spikeCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            originalScale = transform.localScale;
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                originalSprite = spriteRenderer.sprite;
            }
        }

        public void Invert(float duration = 4.0f)
        {
            if (spikeScript == null) spikeScript = GetComponent<Spike>();
            if (hazard == null) hazard = GetComponent<Hazard2D>();
            if (spikeCollider == null) spikeCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            if (duration <= 0f) duration = defaultDuration;
            remainingTimer = duration;

            if (!isInverted)
            {
                isInverted = true;

                // Explicitly disable the Spike script
                if (spikeScript != null)
                {
                    spikeScript.enabled = false;
                }

                // Disable deadly hazard
                if (hazard != null)
                {
                    hazard.enabled = false;
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = trampolineColor;
                    if (trampolineSprite != null) spriteRenderer.sprite = trampolineSprite;
                }

                OnStateChanged?.Invoke(true);
            }
        }

        public void Revert()
        {
            if (!isInverted) return;

            if (spikeScript == null) spikeScript = GetComponent<Spike>();
            if (hazard == null) hazard = GetComponent<Hazard2D>();
            if (spikeCollider == null) spikeCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            isInverted = false;
            remainingTimer = 0f;
            transform.localScale = originalScale;

            // Explicitly re-enable the Spike script
            if (spikeScript != null)
            {
                spikeScript.enabled = true;
            }

            // Re-enable deadly hazard
            if (hazard != null)
            {
                hazard.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
                if (originalSprite != null) spriteRenderer.sprite = originalSprite;
            }

            OnStateChanged?.Invoke(false);
        }

        public void ResetToDefault()
        {
            Revert();
            if (spikeScript == null) spikeScript = GetComponent<Spike>();
            if (spikeScript != null) spikeScript.enabled = true;
            if (hazard == null) hazard = GetComponent<Hazard2D>();
            if (hazard != null) hazard.enabled = true;
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
                    float progress = 1f - Mathf.Clamp01(bounceAnimTimer / 0.25f);
                    float squashX = 1f + Mathf.Sin(progress * Mathf.PI) * 0.25f;
                    float squashY = 1f - Mathf.Sin(progress * Mathf.PI) * 0.25f;
                    transform.localScale = new Vector3(originalScale.x * squashX, originalScale.y * squashY, originalScale.z);
                }
                else
                {
                    transform.localScale = originalScale;
                }

                // Warning flicker in last 0.8 seconds
                if (remainingTimer <= 0.8f && spriteRenderer != null)
                {
                    float flash = Mathf.PingPong(Time.time * 10f, 1f);
                    spriteRenderer.color = Color.Lerp(trampolineColor, new Color(1f, 0.3f, 0.3f, 1f), flash);
                }

                if (remainingTimer <= 0f)
                {
                    Revert();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckAndBounce(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckAndBounce(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckAndBounce(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            CheckAndBounce(collision.gameObject);
        }

        private void CheckAndBounce(GameObject target)
        {
            if (!isInverted) return;

            var rb = target.GetComponent<Rigidbody2D>();
            if (rb == null) rb = target.GetComponentInParent<Rigidbody2D>();

            if (rb != null)
            {
                // Only bounce if falling or near rest to avoid runaway infinite velocity accumulation
                if (rb.linearVelocity.y <= 2.0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceVelocity);
                    bounceAnimTimer = 0.25f;
                    OnPlayerBounced?.Invoke();
                    Project.Audio.ProceduralAudio.Instance?.PlayDoubleJump();
                }
            }
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
