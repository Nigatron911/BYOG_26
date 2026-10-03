using UnityEngine;
using Project.Audio;

namespace Project.Level3
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class AnvilCreation : TemporaryCreation
    {
        [Header("Physics Settings")]
        [SerializeField] private float heavyMass = 60f;
        [SerializeField] private float gravityScale = 3.5f;

        private Rigidbody2D rb;
        private bool hasImpacted = false;

        public override CreationMaterial MaterialType => CreationMaterial.Anvil;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.mass = heavyMass;
                rb.gravityScale = gravityScale;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.freezeRotation = true;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!hasImpacted && collision.relativeVelocity.magnitude > 2.0f)
            {
                hasImpacted = true;
                ProceduralAudio.Instance?.PlayAnvilImpact();
            }
        }
    }
}
