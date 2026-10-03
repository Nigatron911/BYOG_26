using UnityEngine;
using Project.Audio;
using Project.Player;

namespace Project.Level3
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RubberCreation : TemporaryCreation
    {
        [Header("Bounce Settings")]
        [SerializeField] private float bounceVelocity = 17.5f;
        [SerializeField] private ParticleSystem bounceParticles;

        public override CreationMaterial MaterialType => CreationMaterial.Rubber;

        protected override void Awake()
        {
            base.Awake();
            gameObject.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleBounce(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleBounce(other);
        }

        private void HandleBounce(Collider2D collider)
        {
            var player = collider.GetComponent<PlayerMaterialController>();
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceVelocity);
                    ProceduralAudio.Instance?.PlayRubberBounce();
                    if (bounceParticles != null) bounceParticles.Play();
                }
            }
        }
    }
}
