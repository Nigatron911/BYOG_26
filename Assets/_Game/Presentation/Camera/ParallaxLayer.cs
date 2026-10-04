using UnityEngine;

namespace Game.Presentation.CameraSystems
{
    /// <summary>
    /// Presentation component providing multi-layered parallax displacement and optional continuous atmospheric drift.
    /// Follows Section 14 (Presentation systems) and Section 17 (Performance: zero allocations in Update).
    /// Uses deterministic camera-anchor tracking to completely eliminate drift on level resets and transitions.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Rates")]
        [Tooltip("Horizontal parallax factor (0 = stationary in world, 1 = locked to camera).")]
        [SerializeField] private float parallaxEffectX = 0.3f;
        [Tooltip("Vertical parallax factor.")]
        [SerializeField] private float parallaxEffectY = 0.1f;

        [Header("Atmospheric Drift")]
        [Tooltip("Constant horizontal drift speed in world units/sec (e.g. for floating clouds).")]
        [SerializeField] private float driftSpeedX = 0f;

        [Header("Tiling / Wrapping (Optional)")]
        [Tooltip("Enable horizontal wrapping if layer drifts or moves beyond bounds.")]
        [SerializeField] private bool enableWrapping = false;
        [SerializeField] private float wrapWidth = 850f;
        [SerializeField] private float wrapMinX = -80f;

        private Transform targetCamera;
        private Vector3 startLayerPosition;
        private Vector3 startCameraPosition;
        private float accumulatedDrift = 0f;
        private bool isAnchored = false;

        public float ParallaxEffectX { get => parallaxEffectX; set => parallaxEffectX = value; }
        public float ParallaxEffectY { get => parallaxEffectY; set => parallaxEffectY = value; }
        public float DriftSpeedX { get => driftSpeedX; set => driftSpeedX = value; }

        public void BindCamera(Camera cam)
        {
            if (cam != null)
            {
                targetCamera = cam.transform;
                SetAnchor(targetCamera.position);
            }
        }

        public void SetAnchor(Vector3 cameraPos)
        {
            startCameraPosition = cameraPos;
            startLayerPosition = transform.position;
            accumulatedDrift = 0f;
            isAnchored = true;
        }

        private void Awake()
        {
            startLayerPosition = transform.position;
        }

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            if (targetCamera != null && !isAnchored)
            {
                startCameraPosition = targetCamera.position;
                isAnchored = true;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                if (Camera.main != null)
                {
                    targetCamera = Camera.main.transform;
                    if (!isAnchored)
                    {
                        startCameraPosition = targetCamera.position;
                        isAnchored = true;
                    }
                }
                else
                {
                    return;
                }
            }

            if (driftSpeedX != 0f)
            {
                accumulatedDrift += driftSpeedX * Time.deltaTime;
            }

            float camDeltaX = targetCamera.position.x - startCameraPosition.x;
            float camDeltaY = targetCamera.position.y - startCameraPosition.y;

            float newX = startLayerPosition.x + (camDeltaX * parallaxEffectX) + accumulatedDrift;
            float newY = startLayerPosition.y + (camDeltaY * parallaxEffectY);

            if (enableWrapping && wrapWidth > 0f)
            {
                float relativeX = newX - wrapMinX;
                relativeX = Mathf.Repeat(relativeX, wrapWidth);
                newX = wrapMinX + relativeX;
            }

            transform.position = new Vector3(newX, newY, startLayerPosition.z);
        }
    }
}
