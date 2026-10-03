using UnityEngine;
using Game.Core.Interfaces;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Hazard component (Spikes, Death Pit, Acid).
    /// Triggers instant elimination when contacting an IDamageable entity.
    /// Strictly adheres to Section 6 (Interfaces Define Boundaries) - no direct player dependency.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard2D : MonoBehaviour
    {
        [SerializeField] protected string hazardName = "Spike Hazard";
        public string HazardName => hazardName;

        public void SetHazardName(string name)
        {
            hazardName = name;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryEliminate(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryEliminate(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryEliminate(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryEliminate(collision.gameObject);
        }

        private void TryEliminate(GameObject target)
        {
            if (target == null) return;

            var immunity = target.GetComponent<IHazardImmunity>() ?? target.GetComponentInParent<IHazardImmunity>();
            if (immunity != null && immunity.IsImmuneToHazard(hazardName))
            {
                // Target is immune to this hazard (e.g. Stone deflecting Spikes)
                return;
            }

            IDamageable damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                Debug.Log($"[Hazard2D] {hazardName} eliminated {target.name}!");
                damageable.Kill($"Eliminated by {hazardName}");
            }
        }
    }
}
