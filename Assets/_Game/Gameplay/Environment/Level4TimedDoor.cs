using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Player;
using Game.Presentation.UI;

namespace Game.Gameplay.Environment
{
    /// <summary>
    /// Timed exit door for Level 4.
    /// Opens for exactly 4 seconds when the player touches the movable platform.
    /// Closes automatically when the timer expires.
    /// If the player walks through the open door, triggers level complete and black screen.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 13/14 (Presentation separation).
    /// </summary>
    public class Level4TimedDoor : MonoBehaviour
    {
        [Header("Door Settings")]
        [Tooltip("Seconds the door stays open.")]
        [SerializeField] private float openDuration = 4.0f;
        [Tooltip("Vertical slide distance when opening.")]
        [SerializeField] private float slideDistance = 6.2f;
        [SerializeField] private float slideSpeed = 12.0f;

        [Header("Visual Feedback")]
        [SerializeField] private Color closedColor = new Color(0.9f, 0.4f, 0.4f, 1f); // Reddish-tint closed
        [SerializeField] private Color openColor = new Color(0.3f, 0.9f, 0.4f, 1f);   // Green-tint open

        [Header("Transform Memory")]
        [SerializeField] private Vector3 closedPosition;
        [SerializeField] private Vector3 openPosition;
        private Collider2D doorCollider;
        private SpriteRenderer spriteRenderer;
        private ScreenFaderUI screenFader;

        private bool isOpen = false;
        private bool isLevelCompleted = false;
        private float remainingOpenTime = 0f;
        private Coroutine activeDoorRoutine;

        public bool IsOpen => isOpen;
        public float RemainingOpenTime => remainingOpenTime;

        public event Action LevelCompleted;

        public void RecordInitialTransform()
        {
            closedPosition = transform.position;
            openPosition = closedPosition + new Vector3(0f, slideDistance, 0f);
        }

        private void Awake()
        {
            if (closedPosition == Vector3.zero && transform.position != Vector3.zero)
            {
                closedPosition = transform.position;
                openPosition = closedPosition + new Vector3(0f, slideDistance, 0f);
            }
            doorCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            screenFader = FindFirstObjectByType<ScreenFaderUI>(FindObjectsInactive.Include);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = closedColor;
            }
        }

        public void ResetState()
        {
            if (activeDoorRoutine != null)
            {
                StopCoroutine(activeDoorRoutine);
                activeDoorRoutine = null;
            }

            isOpen = false;
            isLevelCompleted = false;
            remainingOpenTime = 0f;

            if (closedPosition != Vector3.zero)
            {
                transform.position = closedPosition;
            }
            if (doorCollider != null)
            {
                doorCollider.isTrigger = false;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = closedColor;
            }
        }

        public void OpenDoor()
        {
            if (isLevelCompleted) return;

            // Reset/refresh timer to 4.0 seconds
            remainingOpenTime = openDuration;

            if (!isOpen)
            {
                Debug.Log($"[Level4TimedDoor] Opening door for {openDuration} seconds!");
                if (activeDoorRoutine != null) StopCoroutine(activeDoorRoutine);
                activeDoorRoutine = StartCoroutine(DoorLifecycleRoutine());
            }
        }

        private IEnumerator DoorLifecycleRoutine()
        {
            isOpen = true;
            if (doorCollider != null)
            {
                doorCollider.isTrigger = true; // Allow passage while open
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = openColor;
            }

            // Slide Up
            while (Vector3.Distance(transform.position, openPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, openPosition, slideSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = openPosition;

            // Countdown timer for 4 seconds
            while (remainingOpenTime > 0f)
            {
                remainingOpenTime -= Time.deltaTime;
                yield return null;
            }

            // Time expired without passing through - slide closed
            if (!isLevelCompleted)
            {
                Debug.Log("[Level4TimedDoor] 4 seconds elapsed! Closing door...");
                isOpen = false;

                while (Vector3.Distance(transform.position, closedPosition) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, closedPosition, slideSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = closedPosition;

                if (doorCollider != null)
                {
                    doorCollider.isTrigger = false;
                }
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = closedColor;
                }
            }

            activeDoorRoutine = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckPassage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckPassage(other);
        }

        private void CheckPassage(Collider2D other)
        {
            if (!isOpen || isLevelCompleted || other == null) return;

            var player = other.GetComponentInParent<AutonomousPlayerController>();
            if (player != null && !player.IsDead)
            {
                TriggerLevelComplete(player);
            }
        }

        private void TriggerLevelComplete(AutonomousPlayerController player)
        {
            if (isLevelCompleted) return;
            isLevelCompleted = true;
            Debug.Log("[Level4TimedDoor] Player passed through open door! Level 4 Complete! Fading to black screen...");

            // Level complete event
            LevelCompleted?.Invoke();
            player.ReachGoal();

            // Trigger black screen fade
            if (screenFader == null) screenFader = FindFirstObjectByType<ScreenFaderUI>(FindObjectsInactive.Include);
            if (screenFader != null)
            {
                screenFader.FadeOut(0.8f);
            }
        }
    }
}
