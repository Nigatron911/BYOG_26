using UnityEngine;
using Project.Audio;
using Project.Core;

namespace Project.Level2
{
    public class SpikeRewriteTarget : RealityRewriteTarget
    {
        [Header("Spike Configuration")]
        [SerializeField] private Collider2D deadlyCollider;
        [SerializeField] private Collider2D springCollider;
        [SerializeField] private KillZone killZoneComponent;
        [SerializeField] private float bounceVelocity = 17.5f;

        [Header("Visual Elements")]
        [SerializeField] private GameObject spikeVisual;
        [SerializeField] private GameObject springVisual;
        [SerializeField] private ParticleSystem rewriteParticles;
        [SerializeField] private ParticleSystem bounceParticles;

        protected override void Awake()
        {
            base.Awake();
            targetName = "Deadly Spikes";

            if (deadlyCollider == null) deadlyCollider = GetComponent<Collider2D>();
            if (killZoneComponent == null) killZoneComponent = GetComponent<KillZone>();
            if (springCollider != null) springCollider.enabled = false;
            if (spikeVisual != null) spikeVisual.SetActive(true);
            if (springVisual != null) springVisual.SetActive(false);
        }

        protected override void ApplyRewriteState()
        {
            // Transition: Spikes -> Bouncy Spring
            if (killZoneComponent != null) killZoneComponent.enabled = false;
            if (deadlyCollider != null) deadlyCollider.enabled = false;
            if (springCollider != null) springCollider.enabled = true;

            if (spikeVisual != null) spikeVisual.SetActive(false);
            if (springVisual != null) springVisual.SetActive(true);

            if (rewriteParticles != null) rewriteParticles.Play();
            ProceduralAudio.Instance?.PlayRewriteFire();
        }

        protected override void RestoreOriginalState()
        {
            // Transition: Bouncy Spring -> Deadly Spikes
            if (killZoneComponent != null) killZoneComponent.enabled = true;
            if (deadlyCollider != null) deadlyCollider.enabled = true;
            if (springCollider != null) springCollider.enabled = false;

            if (spikeVisual != null) spikeVisual.SetActive(true);
            if (springVisual != null) springVisual.SetActive(false);

            if (rewriteParticles != null) rewriteParticles.Play();
            ProceduralAudio.Instance?.PlayRewriteRestore();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isRewritten) return; // If normal, deadlyCollider handles kill

            if (other.CompareTag("Player"))
            {
                var rb = other.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    // Launch player upward!
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceVelocity);
                    ProceduralAudio.Instance?.PlaySpringBounce();
                    if (bounceParticles != null) bounceParticles.Play();
                }
            }
        }
    }
}
