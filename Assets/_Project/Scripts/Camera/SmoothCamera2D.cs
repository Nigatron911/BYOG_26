using UnityEngine;

namespace Project.CameraControl
{
    public class SmoothCamera2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private Vector3 offset = new Vector3(2f, 1.5f, -10f);
        [SerializeField] private Vector2 minBounds = new Vector2(-5f, -3f);
        [SerializeField] private Vector2 maxBounds = new Vector2(80f, 25f);

        private Vector3 currentVelocity = Vector3.zero;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Start()
        {
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 targetPosition = target.position + offset;
            float clampedX = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            float clampedY = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            transform.position = new Vector3(clampedX, clampedY, offset.z);
            currentVelocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + offset;
            float clampedX = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            float clampedY = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);

            Vector3 destination = new Vector3(clampedX, clampedY, offset.z);
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref currentVelocity, smoothTime);
        }
    }
}
