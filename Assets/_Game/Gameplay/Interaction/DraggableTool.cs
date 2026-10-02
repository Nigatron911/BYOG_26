using System;
using UnityEngine;
using Game.Core.Events;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Component placed on placeable tools (Ramp, Ladder, Box).
    /// Handles physical states (Preview vs Placed), transparency, and rotations.
    /// Follows SRP by handling only tool-level physical presentation and state.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class DraggableTool : MonoBehaviour
    {
        [SerializeField] private ToolType toolType;

        private Rigidbody2D rb;
        private Collider2D[] allColliders;
        private SpriteRenderer[] allRenderers;
        private Color originalColor = Color.white;
        private bool isPlaced = false;
        private bool isDragging = false;
        private Vector3 placedPosition;
        private Quaternion placedRotation;

        public ToolType Type => toolType;
        public bool IsPlaced => isPlaced;
        public bool IsDragging => isDragging;

        private bool[] originalTriggerStates;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (allColliders == null)
            {
                allColliders = GetComponentsInChildren<Collider2D>(true);
                originalTriggerStates = new bool[allColliders.Length];
                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (allColliders[i] != null)
                    {
                        originalTriggerStates[i] = allColliders[i].isTrigger;
                    }
                }
            }

            if (allRenderers == null)
            {
                allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                if (allRenderers.Length > 0 && allRenderers[0] != null)
                {
                    originalColor = allRenderers[0].color;
                }
            }

            IgnoreCollisionWithPlayerIfLadder();
        }

        private void IgnoreCollisionWithPlayerIfLadder()
        {
            if (toolType == ToolType.Ladder)
            {
                var player = FindFirstObjectByType<Game.Gameplay.Player.AutonomousPlayerController>();
                if (player != null)
                {
                    var playerCol = player.GetComponent<Collider2D>();
                    if (playerCol != null && allColliders != null)
                    {
                        foreach (var col in allColliders)
                        {
                            if (col != null && !col.isTrigger) Physics2D.IgnoreCollision(col, playerCol, true);
                        }
                    }
                }
            }
        }

        public void SetPreviewMode(bool preview)
        {
            EnsureInitialized();
            isDragging = preview;

            if (preview)
            {
                isPlaced = false;
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                // Make colliders triggers while dragging so they don't push anything
                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (allColliders[i] != null)
                    {
                        allColliders[i].isTrigger = true;
                    }
                }

                SetVisualAlpha(0.6f);
            }
            else
            {
                isPlaced = true;
                placedPosition = transform.position;
                placedRotation = transform.rotation;

                // Restore original collider trigger states accurately
                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (allColliders[i] != null)
                    {
                        allColliders[i].isTrigger = (originalTriggerStates != null && i < originalTriggerStates.Length)
                            ? originalTriggerStates[i]
                            : false;
                    }
                }

                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = 1.8f;
                    rb.WakeUp();
                }

                SetVisualAlpha(1.0f);
                IgnoreCollisionWithPlayerIfLadder();
            }
        }

        /// <summary>
        /// Drops the tool from the top spawner with dynamic 2D physics.
        /// </summary>
        public void DropWithPhysics(Vector2 initialVelocity = default)
        {
            EnsureInitialized();
            isDragging = false;
            isPlaced = true;
            placedPosition = transform.position;
            placedRotation = transform.rotation;

            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] != null)
                {
                    allColliders[i].isTrigger = (originalTriggerStates != null && i < originalTriggerStates.Length)
                        ? originalTriggerStates[i]
                        : false;
                }
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1.8f;
                rb.linearVelocity = initialVelocity;
                rb.WakeUp();
            }

            SetVisualAlpha(1.0f);
            IgnoreCollisionWithPlayerIfLadder();
        }

        /// <summary>
        /// Restores the tool to its position and orientation when confirmed,
        /// canceling any physics displacement that occurred during simulation.
        /// </summary>
        public void ResetToPlacedTransform()
        {
            if (!isPlaced) return;

            transform.position = placedPosition;
            transform.rotation = placedRotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1.8f;
                rb.WakeUp();
            }
        }

        public void Rotate(float angleDelta)
        {
            EnsureInitialized();
            transform.Rotate(0f, 0f, angleDelta);
            if (rb != null)
            {
                rb.rotation = transform.eulerAngles.z;
                rb.angularVelocity = 0f;
                rb.WakeUp();
            }
            if (isPlaced)
            {
                placedRotation = transform.rotation;
            }
        }

        public void SetVisualAlpha(float alpha)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    Color c = allRenderers[i].color;
                    c.a = alpha;
                    allRenderers[i].color = c;
                }
            }
        }

        public void SetPosition(Vector2 worldPosition)
        {
            EnsureInitialized();
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            if (rb != null)
            {
                rb.position = worldPosition;
                rb.linearVelocity = Vector2.zero;
            }
            if (isPlaced)
            {
                placedPosition = transform.position;
            }
        }

        public void RecordPlacedTransform()
        {
            placedPosition = transform.position;
            placedRotation = transform.rotation;
        }
    }
}
