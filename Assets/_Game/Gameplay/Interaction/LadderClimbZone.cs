using UnityEngine;
using Game.Core.Interfaces;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Component placed on ladders that provides elevation bounds and climbing metadata.
    /// Implements IClimbable to decouple player climbing mechanics from ladder prefabs.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LadderClimbZone : MonoBehaviour, IClimbable
    {
        [SerializeField] private float climbSpeedMultiplier = 1.0f;
        [SerializeField] private float topOffset = 0.2f;

        private Collider2D zoneCollider;

        public float ClimbSpeedMultiplier => climbSpeedMultiplier;

        public Bounds GetBounds()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider2D>();
            }
            return zoneCollider != null ? zoneCollider.bounds : new Bounds(transform.position, Vector3.one);
        }

        public float TopElevation
        {
            get
            {
                Bounds b = GetBounds();
                return b.max.y + topOffset;
            }
        }

        private void Awake()
        {
            zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }
    }
}
