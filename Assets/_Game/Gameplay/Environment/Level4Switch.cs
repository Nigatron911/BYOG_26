using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Player;

namespace Game.Gameplay.Environment
{
    /// <summary>
    /// Interactive ceiling switch for Level 4.
    /// Animates a press squash/sink when touched by the player.
    /// Fires SwitchPressed on contact, and SwitchReleased when contact ends.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 7 (Observer Pattern).
    /// </summary>
    public class Level4Switch : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Vector3 pressedOffset = new Vector3(0f, 0.25f, 0f);
        [SerializeField] private float pressScaleYMultiplier = 0.65f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(0.22f, 0.74f, 0.97f, 1f); // Vibrant Cyan
        [SerializeField] private float animDuration = 0.15f;

        [Header("Transform Memory")]
        [SerializeField] private Vector3 initialPosition;
        [SerializeField] private Vector3 initialScale;
        private SpriteRenderer spriteRenderer;
        private Collider2D switchCollider;
        private Collider2D activePlayerCollider;

        private bool isPressed = false;
        private bool hasReleasedAfterPress = false;
        private int touchingPlayerColliders = 0;
        private Coroutine activeAnimRoutine;

        public bool IsPressed => isPressed;
        public bool HasReleasedAfterPress => hasReleasedAfterPress;

        public event Action SwitchPressed;
        public event Action SwitchReleased;

        public void RecordInitialTransform()
        {
            initialPosition = transform.position;
            initialScale = transform.localScale;
        }

        private void Awake()
        {
            if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
            {
                initialPosition = transform.position;
            }
            if (initialScale == Vector3.zero && transform.localScale != Vector3.zero)
            {
                initialScale = transform.localScale;
            }
            spriteRenderer = GetComponent<SpriteRenderer>();
            switchCollider = GetComponent<Collider2D>();
            if (spriteRenderer != null)
            {
                normalColor = spriteRenderer.color;
            }
        }

        private void Update()
        {
            // Safety verification: if pressed but not released, and player is no longer touching
            if (isPressed && !hasReleasedAfterPress && touchingPlayerColliders > 0 && switchCollider != null)
            {
                if (activePlayerCollider != null && !switchCollider.IsTouching(activePlayerCollider))
                {
                    touchingPlayerColliders = 0;
                    hasReleasedAfterPress = true;
                    Debug.Log("[Level4Switch] Verified player exited switch! Notifying release...");
                    SwitchReleased?.Invoke();
                }
            }
        }

        public void ResetState()
        {
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                activeAnimRoutine = null;
            }

            isPressed = false;
            hasReleasedAfterPress = false;
            touchingPlayerColliders = 0;
            activePlayerCollider = null;

            if (initialPosition != Vector3.zero)
            {
                transform.position = initialPosition;
            }
            if (initialScale != Vector3.zero)
            {
                transform.localScale = initialScale;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerEnter(collision.collider);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            HandlePlayerExit(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerEnter(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            HandlePlayerExit(other);
        }

        private void HandlePlayerEnter(Collider2D col)
        {
            if (col == null) return;
            var player = col.GetComponentInParent<AutonomousPlayerController>();
            if (player == null || player.IsDead) return;

            activePlayerCollider = col;
            touchingPlayerColliders++;

            if (!isPressed)
            {
                isPressed = true;
                hasReleasedAfterPress = false;
                Debug.Log("[Level4Switch] Player pressed the switch! Playing press animation...");

                if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);
                activeAnimRoutine = StartCoroutine(AnimatePressRoutine(true));

                SwitchPressed?.Invoke();
            }
        }

        private void HandlePlayerExit(Collider2D col)
        {
            if (col == null) return;
            var player = col.GetComponentInParent<AutonomousPlayerController>();
            if (player == null) return;

            touchingPlayerColliders = Mathf.Max(0, touchingPlayerColliders - 1);

            if (touchingPlayerColliders == 0 && isPressed && !hasReleasedAfterPress)
            {
                hasReleasedAfterPress = true;
                Debug.Log("[Level4Switch] Player collision not detected with switch after once pressed! Notifying release...");
                SwitchReleased?.Invoke();
            }
        }

        private IEnumerator AnimatePressRoutine(bool pressed)
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : normalColor;

            Vector3 targetPos = pressed ? initialPosition + pressedOffset : initialPosition;
            Vector3 targetScale = pressed 
                ? new Vector3(initialScale.x * 1.12f, initialScale.y * pressScaleYMultiplier, initialScale.z) 
                : initialScale;
            Color targetColor = pressed ? pressedColor : normalColor;

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
                }
                yield return null;
            }

            transform.position = targetPos;
            transform.localScale = targetScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = targetColor;
            }
            activeAnimRoutine = null;
        }
    }
}
