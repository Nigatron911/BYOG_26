using System;
using System.Collections;
using UnityEngine;

namespace Game.Gameplay.Transmutation
{
    /// <summary>
    /// Wall or barrier that can be transmuted from solid obstacle into a pass-through phase trigger.
    /// Reverts to solid after duration (default 4 seconds).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TransmutableWall : MonoBehaviour, ITransmutable
    {
        [Header("Phase Settings")]
        [SerializeField] private float defaultDuration = 3.0f;
        [SerializeField] private Color phaseColor = new Color(0.35f, 0.85f, 1.0f, 0.25f);
        [SerializeField] private float pulseFrequency = 4.0f;

        [Header("References")]
        [SerializeField] private Collider2D wallCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Color originalColor = Color.white;
        private bool isInverted = false;
        private float remainingTimer = 0f;
        private bool isWaitingForPlayerExit = false;

        public bool IsInverted => isInverted;
        public float RemainingDuration => remainingTimer;
        public string TransmutableName => gameObject.name;

        public event Action<bool> OnStateChanged;

        private void Awake()
        {
            if (wallCollider == null) wallCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public void Invert(float duration = 4.0f)
        {
            if (wallCollider == null) wallCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            if (duration <= 0f) duration = defaultDuration;
            remainingTimer = duration;

            if (!isInverted)
            {
                isInverted = true;
                isWaitingForPlayerExit = false;

                if (wallCollider != null)
                {
                    wallCollider.isTrigger = true;
                }

                OnStateChanged?.Invoke(true);
            }
        }

        public void Revert()
        {
            if (!isInverted && !isWaitingForPlayerExit) return;

            // Check if player is currently overlapping before solidifying
            if (IsPlayerOverlapping())
            {
                isWaitingForPlayerExit = true;
                return;
            }

            FinalizeRevert();
        }

        private void FinalizeRevert()
        {
            isInverted = false;
            isWaitingForPlayerExit = false;
            remainingTimer = 0f;

            if (wallCollider == null) wallCollider = GetComponent<Collider2D>();
            if (wallCollider != null)
            {
                wallCollider.isTrigger = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            OnStateChanged?.Invoke(false);
        }

        private void Update()
        {
            if (isInverted)
            {
                remainingTimer -= Time.deltaTime;

                // Visual pulse while phased
                if (spriteRenderer != null)
                {
                    float pulse = 0.75f + Mathf.Sin(Time.time * pulseFrequency) * 0.25f;
                    Color c = phaseColor;
                    c.a *= pulse;

                    // Warning flash in last 0.8s
                    if (remainingTimer <= 0.8f)
                    {
                        float flash = Mathf.PingPong(Time.time * 12f, 1f);
                        c = Color.Lerp(phaseColor, new Color(1f, 0.4f, 0.2f, 0.4f), flash);
                    }
                    spriteRenderer.color = c;
                }

                if (remainingTimer <= 0f)
                {
                    Revert();
                }
            }
            else if (isWaitingForPlayerExit)
            {
                // Warning visual while waiting for player to step out
                if (spriteRenderer != null)
                {
                    float flash = Mathf.PingPong(Time.time * 8f, 1f);
                    spriteRenderer.color = Color.Lerp(new Color(1f, 0.3f, 0.2f, 0.35f), new Color(1f, 0.8f, 0.2f, 0.5f), flash);
                }

                if (!IsPlayerOverlapping())
                {
                    FinalizeRevert();
                }
            }
        }

        private bool IsPlayerOverlapping()
        {
            if (wallCollider == null) return false;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            Collider2D[] results = new Collider2D[8];
            int count = wallCollider.Overlap(filter, results);

            for (int i = 0; i < count; i++)
            {
                if (results[i] != null && results[i].CompareTag("Player"))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnDisable()
        {
            if (isInverted || isWaitingForPlayerExit)
            {
                FinalizeRevert();
            }
        }
    }
}
