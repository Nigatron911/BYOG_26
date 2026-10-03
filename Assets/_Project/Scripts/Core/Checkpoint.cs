using System;
using UnityEngine;
using Project.Audio;
using Project.Player;

namespace Project.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Config")]
        [SerializeField] private int checkpointIndex = 0;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private SpriteRenderer beaconRenderer;
        [SerializeField] private ParticleSystem activeParticles;

        private bool isActivated = false;

        public static event Action<Checkpoint> OnCheckpointActivated;
        public int CheckpointIndex => checkpointIndex;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (beaconRenderer != null)
            {
                beaconRenderer.color = new Color(0.6f, 0.65f, 0.75f, 0.8f); // Inactive dim
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerMaterialController>();
            if (player != null && !isActivated)
            {
                Activate();
            }
        }

        public void Activate()
        {
            if (isActivated) return;
            isActivated = true;

            if (beaconRenderer != null)
            {
                beaconRenderer.color = new Color(0.1f, 1.0f, 0.6f, 1.0f); // Bright active green
            }

            if (activeParticles != null)
            {
                activeParticles.Play();
            }

            ProceduralAudio.Instance?.PlayCheckpoint();
            OnCheckpointActivated?.Invoke(this);
        }

        public void SetVisualState(bool active)
        {
            isActivated = active;
            if (beaconRenderer != null)
            {
                beaconRenderer.color = active ? new Color(0.1f, 1.0f, 0.6f, 1.0f) : new Color(0.6f, 0.65f, 0.75f, 0.8f);
            }
        }
    }
}
