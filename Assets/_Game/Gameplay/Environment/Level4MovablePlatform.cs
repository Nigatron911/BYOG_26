using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Player;

namespace Game.Gameplay.Environment
{
    /// <summary>
    /// Movable platform in Level 4.
    /// Initially hidden; appears when the player releases the ceiling switch after pressing it.
    /// Detects player collision/landing and signals the door to open for 4 seconds.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 7 (Observer Pattern).
    /// </summary>
    public class Level4MovablePlatform : MonoBehaviour
    {
        [Header("Appearance Animation Settings")]
        [SerializeField] private float appearDuration = 0.40f;
        [SerializeField] private Color platformColor = new Color(0.38f, 0.85f, 0.98f, 1f); // Glowing cyan-blue

        [Header("Transform Memory")]
        [SerializeField] private Vector3 initialScale;
        private SpriteRenderer spriteRenderer;
        private Collider2D platformCollider;
        private Color initialColor = Color.white;

        private bool isVisible = false;
        private bool hasTriggeredDoor = false;
        private Coroutine activeAppearRoutine;

        public bool IsVisible => isVisible;
        public bool HasTriggeredDoor => hasTriggeredDoor;
        public event Action PlayerLanded;

        public void RecordInitialTransform()
        {
            initialScale = transform.localScale;
        }

        private void Awake()
        {
            if (initialScale == Vector3.zero && transform.localScale != Vector3.zero)
            {
                initialScale = transform.localScale;
            }
            spriteRenderer = GetComponent<SpriteRenderer>();
            platformCollider = GetComponent<Collider2D>();
            if (spriteRenderer != null)
            {
                initialColor = spriteRenderer.color;
            }

            // Initially hidden
            SetPlatformActive(false);
        }

        public void ResetState()
        {
            if (activeAppearRoutine != null)
            {
                StopCoroutine(activeAppearRoutine);
                activeAppearRoutine = null;
            }

            isVisible = false;
            hasTriggeredDoor = false;
            if (initialScale != Vector3.zero)
            {
                transform.localScale = initialScale;
            }
            SetPlatformActive(false);
        }

        public void ShowPlatform()
        {
            if (isVisible) return;
            isVisible = true;
            Debug.Log("[Level4MovablePlatform] Switch released! Movable platform appearing...");

            if (activeAppearRoutine != null) StopCoroutine(activeAppearRoutine);
            activeAppearRoutine = StartCoroutine(AppearRoutine());
        }

        private void SetPlatformActive(bool active)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = active;
            if (platformCollider != null) platformCollider.enabled = active;
        }

        private IEnumerator AppearRoutine()
        {
            SetPlatformActive(true);
            transform.localScale = Vector3.zero;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = platformColor;
            }

            float elapsed = 0f;
            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / appearDuration);
                // Overshoot bounce pop-in
                float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.localScale = Vector3.LerpUnclamped(Vector3.zero, initialScale, scaleT);
                yield return null;
            }

            transform.localScale = initialScale;
            activeAppearRoutine = null;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerContact(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandlePlayerContact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandlePlayerContact(other);
        }

        private void HandlePlayerContact(Collider2D col)
        {
            if (!isVisible || col == null) return;
            var player = col.GetComponentInParent<AutonomousPlayerController>();
            if (player == null || player.IsDead) return;

            hasTriggeredDoor = true;
            PlayerLanded?.Invoke();
        }
    }
}
