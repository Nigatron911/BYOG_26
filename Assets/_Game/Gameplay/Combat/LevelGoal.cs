using UnityEngine;
using Game.Gameplay.Player;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Goal marker component signaling the end of the puzzle level.
    /// Strictly handles arrival detection and informs the arriving entity.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        private bool hasTriggered = false;

        /// <summary>Raised when the player arrives at this goal.</summary>
        public event System.Action Reached;
        /// <summary>Raised when the goal is re-armed for a new attempt.</summary>
        public event System.Action Rearmed;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered) return;

            AutonomousPlayerController player = other.GetComponentInParent<AutonomousPlayerController>();
            if (player != null && !player.IsDead)
            {
                hasTriggered = true;
                Reached?.Invoke();
                player.ReachGoal();
            }
        }

        public void ResetGoal()
        {
            hasTriggered = false;
            Rearmed?.Invoke();
        }
    }
}
