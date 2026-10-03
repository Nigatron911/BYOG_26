using UnityEngine;
using Project.Player;

namespace Project.Environment
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindObstacle : MonoBehaviour
    {
        [Header("Wind Settings")]
        [SerializeField] private Vector2 windForce = new Vector2(0f, 42f);
        [SerializeField] private ParticleSystem windParticles;
        [SerializeField] private SpriteRenderer windBackground;

        public ParticleSystem WindParticles => windParticles;
        public SpriteRenderer WindBackground => windBackground;
        public Vector2 WindForce => windForce;
        public void SetWindForce(Vector2 force) => windForce = force;

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;

            if (windParticles == null)
            {
                windParticles = GetComponentInChildren<ParticleSystem>();
            }

            if (windParticles != null)
            {
                var main = windParticles.main;
                main.startSpeed = 14f;
                var emission = windParticles.emission;
                emission.rateOverTime = 45f;

                if (!windParticles.isPlaying)
                {
                    windParticles.Play();
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerMaterialController>();
            if (player != null)
            {
                // Paper is buoyant all the way up; Rubber lifts moderately to editable height; Stone is immune
                player.ApplyWindForce(windForce, transform.position.y);
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
                Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)windForce.normalized * 2f);
            }
        }
    }
}
