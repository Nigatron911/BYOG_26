using System.Collections;
using UnityEngine;

namespace Game.Gameplay.Environment
{
    /// <summary>
    /// Movable spike hazard in Level 4.
    /// Initially submerged beneath the floor.
    /// Animates rising up when the switch is pressed.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 14 (VFX/Animation).
    /// </summary>
    public class Level4MovableSpike : MonoBehaviour
    {
        [Header("Rise Animation Settings")]
        [Tooltip("Target elevation when fully raised.")]
        [SerializeField] private float raisedPositionY = -3.60f;
        [Tooltip("Distance submerged below floor when reset/hidden.")]
        [SerializeField] private float loweredOffsetY = 3.0f;
        [SerializeField] private float riseDuration = 0.50f;

        [Header("Transform Memory")]
        [SerializeField] private Vector3 initialPosition;
        private Collider2D spikeCollider;
        private Coroutine activeRiseRoutine;
        private bool isRaised = false;

        public bool IsRaised => isRaised;

        public void RecordInitialTransform()
        {
            raisedPositionY = transform.position.y;
            initialPosition = new Vector3(transform.position.x, raisedPositionY - loweredOffsetY, transform.position.z);
        }

        private void Awake()
        {
            spikeCollider = GetComponent<Collider2D>();
            if (Mathf.Abs(raisedPositionY) < 0.001f && transform.position.y != 0f)
            {
                raisedPositionY = transform.position.y;
            }
            if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
            {
                initialPosition = new Vector3(transform.position.x, raisedPositionY - loweredOffsetY, transform.position.z);
            }
        }

        public void ResetState()
        {
            if (activeRiseRoutine != null)
            {
                StopCoroutine(activeRiseRoutine);
                activeRiseRoutine = null;
            }

            isRaised = false;
            Vector3 loweredPos = new Vector3(transform.position.x, raisedPositionY - loweredOffsetY, transform.position.z);
            transform.position = loweredPos;

            if (spikeCollider != null)
            {
                spikeCollider.enabled = false;
            }
        }

        public void RaiseSpikes()
        {
            if (isRaised) return;
            isRaised = true;
            Debug.Log("[Level4MovableSpike] Switch pressed! Animating movable spikes rising up...");

            if (activeRiseRoutine != null) StopCoroutine(activeRiseRoutine);
            activeRiseRoutine = StartCoroutine(RiseRoutine());
        }

        private IEnumerator RiseRoutine()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = new Vector3(startPos.x, raisedPositionY, startPos.z);

            if (spikeCollider != null)
            {
                spikeCollider.enabled = true;
            }

            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                // Punchy smooth rise
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(startPos, targetPos, easeOut);
                yield return null;
            }

            transform.position = targetPos;
            activeRiseRoutine = null;
        }
    }
}
