using UnityEngine;
using Project.Audio;

namespace Project.Level2
{
    public class WallRewriteTarget : RealityRewriteTarget
    {
        [Header("Wall Configuration")]
        [SerializeField] private Collider2D wallCollider;
        [SerializeField] private ParticleSystem dissolveParticles;

        private Color normalColor = Color.white;
        private Color intangibleColor = new Color(0.2f, 0.85f, 1f, 0.25f);

        protected override void Awake()
        {
            base.Awake();
            targetName = "Solid Wall";

            if (wallCollider == null) wallCollider = GetComponent<Collider2D>();
            if (mainRenderer != null) normalColor = mainRenderer.color;
        }

        protected override void ApplyRewriteState()
        {
            // Disable blocking collision
            if (wallCollider != null) wallCollider.isTrigger = true;

            // Make visually ghost-like / transparent
            if (mainRenderer != null)
            {
                mainRenderer.color = intangibleColor;
            }

            if (dissolveParticles != null) dissolveParticles.Play();
            ProceduralAudio.Instance?.PlayRewriteFire();
        }

        protected override void RestoreOriginalState()
        {
            // Check if player is inside wall upon restoration, safely push out to right or left
            var player = FindFirstObjectByType<Project.Player.PlayerMaterialController>();
            if (player != null && wallCollider != null)
            {
                var playerCol = player.GetComponent<Collider2D>();
                if (playerCol != null && wallCollider.bounds.Intersects(playerCol.bounds))
                {
                    float wallCenterX = wallCollider.bounds.center.x;
                    float pushX = player.transform.position.x >= wallCenterX
                        ? wallCollider.bounds.max.x + 0.6f
                        : wallCollider.bounds.min.x - 0.6f;
                    player.transform.position = new Vector3(pushX, player.transform.position.y, player.transform.position.z);
                }
            }

            // Restore blocking collision
            if (wallCollider != null) wallCollider.isTrigger = false;

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
