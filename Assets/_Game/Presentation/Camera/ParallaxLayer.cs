using UnityEngine;

namespace Game.Presentation.CameraSystems
{
    /// <summary>
    /// Presentation component providing smooth parallax displacement in the open background.
    /// Follows Section 14 (Audio/VFX/Animation/Camera are presentation systems).
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float parallaxEffectX = 0.3f;
        [SerializeField] private float parallaxEffectY = 0.1f;

        private Transform targetCamera;
        private Vector3 startPosition;
        private Vector3 lastCameraPosition;

        public void BindCamera(Camera cam)
        {
            if (cam != null)
            {
                targetCamera = cam.transform;
                lastCameraPosition = targetCamera.position;
            }
        }

        private void Start()
        {
            startPosition = transform.position;
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
                lastCameraPosition = targetCamera.position;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            Vector3 delta = targetCamera.position - lastCameraPosition;
            transform.position += new Vector3(delta.x * parallaxEffectX, delta.y * parallaxEffectY, 0f);
            lastCameraPosition = targetCamera.position;
        }
    }
}
