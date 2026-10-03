using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Spike hazard component that eliminates damageable entities on contact, triggering Game Over.
    /// Inherits from Hazard2D to preserve interface-driven architecture (Section 6, Section 23 of Architecture Contract).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Spike : Hazard2D
    {
        private void Awake()
        {
            if (string.IsNullOrEmpty(hazardName) || hazardName == "Spike Hazard")
            {
                hazardName = "Spike";
            }
        }
    }
}
