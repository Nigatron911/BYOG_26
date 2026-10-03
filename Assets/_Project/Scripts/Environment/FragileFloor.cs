using System.Collections;
using UnityEngine;
using Project.Audio;
using Project.Player;

namespace Project.Environment
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class FragileFloor : MonoBehaviour
    {
        [Header("Break Settings")]
        [SerializeField] private float shakeDuration = 0.35f;
        [SerializeField] private float shakeMagnitude = 0.08f;
        [SerializeField] private ParticleSystem breakParticles;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D floorCollider;

        private Vector3 originalLocalPos;
        private bool isBreaking = false;
        private bool isBroken = false;

        private void Awake()
        {
            if (floorCollider == null) floorCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            originalLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (isBreaking || isBroken) return;

            // Proactively check if stone player is on top of this platform
            var hits = Physics2D.OverlapBoxAll((Vector2)transform.position + Vector2.up * 0.45f, new Vector2(3.6f, 0.4f), 0f);
            foreach (var hit in hits)
            {
                var player = hit.GetComponent<PlayerMaterialController>();
                if (player != null && player.IsStone)
                {
                    StartCoroutine(BreakSequence());
                    break;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            EvaluateCollision(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            EvaluateCollision(collision.gameObject);
        }

        private void EvaluateCollision(GameObject go)
        {
            if (isBreaking || isBroken) return;

            var player = go.GetComponent<PlayerMaterialController>();
            if (player != null && player.IsStone)
            {
                // Stone is heavy enough to break the floor!
                StartCoroutine(BreakSequence());
            }
        }

        private IEnumerator BreakSequence()
        {
            isBreaking = true;
            ProceduralAudio.Instance?.PlayFloorCrack();

            // Shake violently
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
                float yOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
                transform.localPosition = originalLocalPos + new Vector3(xOffset, yOffset, 0f);
                yield return null;
            }

            // Break!
            isBroken = true;
            isBreaking = false;
            transform.localPosition = originalLocalPos;

            if (floorCollider != null) floorCollider.enabled = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;

            if (breakParticles != null)
            {
                breakParticles.transform.position = transform.position;
                breakParticles.Play();
            }

            ProceduralAudio.Instance?.PlayFloorBreak();
        }

        public void ResetFloor()
        {
            StopAllCoroutines();
            isBreaking = false;
            isBroken = false;
            transform.localPosition = originalLocalPos;
            if (floorCollider != null) floorCollider.enabled = true;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }
    }
}
