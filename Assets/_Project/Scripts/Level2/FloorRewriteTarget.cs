using UnityEngine;
using Project.Audio;

namespace Project.Level2
{
    public class FloorRewriteTarget : RealityRewriteTarget
    {
        [Header("Floor Configuration")]
        [SerializeField] private Collider2D floorCollider;
        [SerializeField] private ParticleSystem dissolveParticles;

        private Color normalColor = Color.white;
        private Color intangibleColor = new Color(0.2f, 0.85f, 1f, 0.2f);

        protected override void Awake()
        {
            base.Awake();
            targetName = "Solid Floor";

            if (floorCollider == null) floorCollider = GetComponent<Collider2D>();
            if (mainRenderer != null) normalColor = mainRenderer.color;
        }

        protected override void ApplyRewriteState()
        {
            // Disable floor collision so player drops through
            if (floorCollider != null) floorCollider.enabled = false;

            // Make visually transparent / unstable
            if (mainRenderer != null)
            {
                mainRenderer.color = intangibleColor;
            }

            if (dissolveParticles != null) dissolveParticles.Play();
            ProceduralAudio.Instance?.PlayRewriteFire();
        }

        protected override void RestoreOriginalState()
        {
            // Restore solid floor collision
            if (floorCollider != null) floorCollider.enabled = true;

            // Restore solid appearance
            if (mainRenderer != null)
            {
                mainRenderer.color = normalColor;
            }

            if (dissolveParticles != null) dissolveParticles.Play();
            ProceduralAudio.Instance?.PlayRewriteRestore();
        }
    }
}
