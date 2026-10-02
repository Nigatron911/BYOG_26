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
                player.ReachGoal();
            }
        }

        public void ResetGoal()
        {
            hasTriggered = false;
        }
    }
}
