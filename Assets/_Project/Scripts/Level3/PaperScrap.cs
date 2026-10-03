using System;
using UnityEngine;
using Project.Audio;

namespace Project.Level3
{
    [RequireComponent(typeof(Collider2D))]
    public class PaperScrap : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float rotationSpeed = 35f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem collectParticles;

        private Vector3 startPosition;
        private bool isCollected = false;

        public static event Action<PaperScrap> OnScrapCollected;

        private void Awake()
        {
            startPosition = transform.position;
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (isCollected) return;

            // Subtle bobbing and rotation
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player") || other.GetComponent<Project.Player.PlayerMaterialController>() != null)
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            ProceduralAudio.Instance?.PlayPaperPickup();

            if (collectParticles != null)
            {
                collectParticles.transform.SetParent(null);
                collectParticles.Play();
                Destroy(collectParticles.gameObject, 1.5f);
            }

            OnScrapCollected?.Invoke(this);

            var resourceController = FindFirstObjectByType<PaperResourceController>();
            if (resourceController != null)
            {
                resourceController.AddScraps(1);
            }

            gameObject.SetActive(false);
        }

        public void ResetScrap()
        {
            isCollected = false;
            transform.position = startPosition;
            gameObject.SetActive(true);
        }
    }
}
