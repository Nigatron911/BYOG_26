using System;
using UnityEngine;
using Project.Audio;
using Project.Managers;
using Project.Player;

namespace Project.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class FinishTrigger : MonoBehaviour
    {
        [SerializeField] private ParticleSystem confettiParticles;
        private bool hasTriggered = false;

        public static event Action<int> OnLevelCompleted;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered) return;

            var player = other.GetComponent<PlayerMaterialController>();
            if (player != null)
            {
                hasTriggered = true;
                if (confettiParticles != null) confettiParticles.Play();

                ObjectivesManager.Instance?.SetStep(ObjectivesManager.ObjectiveStep.LevelCompleted);
                ProceduralAudio.Instance?.PlayVictory();

                OnLevelCompleted?.Invoke(player.RemainingTransformations);
            }
        }
    }
}
