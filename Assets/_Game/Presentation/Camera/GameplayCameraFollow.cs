using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Events;

namespace Game.Presentation.CameraSystems
{
    /// <summary>
    /// Level camera. Every level publishes its world bounds (LevelFraming); the camera never shows
    /// anything outside them, so neighbouring levels never bleed into view.
    ///  - Planning levels (FollowPlayer = false): a static view fitted to the whole level.
    ///  - Action levels (FollowPlayer = true): opens on the whole-level overview, then eases in and
    ///    follows the player with a little look-ahead, clamped to the level bounds.
    ///  - Holding Tab shows the whole-level overview at any time.
    /// Pure presentation (Section 14): reads the player's transform/velocity, never changes gameplay.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameplayCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform targetPlayer;
        [Tooltip("Orthographic size while following the player.")]
        [SerializeField] private float followOrthoSize = 16f;
        [SerializeField] private Vector2 followOffset = new Vector2(0f, 3f);
        [Tooltip("How far ahead (world units per unit of horizontal speed) the camera leads the player.")]
        [SerializeField] private float lookAheadPerSpeed = 0.6f;
        [SerializeField] private float maxLookAhead = 7f;
        [SerializeField] private float positionSmoothTime = 0.28f;
        [SerializeField] private float zoomSmoothTime = 0.45f;

        [Header("Level intro")]
        [Tooltip("Seconds the whole-level overview is shown when an action level starts.")]
        [SerializeField] private float introOverviewSeconds = 1.6f;

        private Camera cam;
        private GameEvents events;
        private Rigidbody2D targetBody;

        private bool hasFraming;
        private Rect bounds;
        private bool followPlayer;
        private float introTimer;
        private Vector3 velocity;
        private float zoomVelocity;
        private float smoothedLookAhead;
        private float lookAheadVelocity;

        public void Initialize(GameEvents gameEvents, Transform playerTransform = null)
        {
            Dispose();
            events = gameEvents;
            if (playerTransform != null)
            {
                targetPlayer = playerTransform;
                targetBody = playerTransform.GetComponent<Rigidbody2D>();
            }
            if (events != null) events.LevelFramingChanged += OnLevelFramingChanged;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnLevelFramingChanged(LevelFraming framing)
        {
            hasFraming = true;
            bounds = framing.Bounds;
            followPlayer = framing.FollowPlayer;
            introTimer = followPlayer ? introOverviewSeconds : 0f;
            velocity = Vector3.zero;
            zoomVelocity = 0f;
            smoothedLookAhead = 0f;
            lookAheadVelocity = 0f;

            // Levels change under a fade: snap straight to the overview.
            GetOverview(out var pos, out var size);
            ApplyImmediate(pos, size);
        }

        private void LateUpdate()
        {
            if (!hasFraming || cam == null) return;

            bool overviewHeld = Keyboard.current != null && Keyboard.current.tabKey.isPressed && Time.timeScale > 0f;
            if (introTimer > 0f) introTimer -= Time.deltaTime;

            Vector2 targetPos;
            float targetSize;
            GetOverview(out targetPos, out targetSize);

            if (followPlayer && !overviewHeld && introTimer <= 0f && targetPlayer != null)
            {
                targetSize = Mathf.Min(followOrthoSize, targetSize);

                float vx = targetBody != null ? targetBody.linearVelocity.x : 0f;
                float desiredLead = Mathf.Clamp(vx * lookAheadPerSpeed, -maxLookAhead, maxLookAhead);
                smoothedLookAhead = Mathf.SmoothDamp(smoothedLookAhead, desiredLead, ref lookAheadVelocity, 0.6f);

                targetPos = (Vector2)targetPlayer.position + followOffset + new Vector2(smoothedLookAhead, 0f);
                // Clamp against the size the camera is heading to, so zooming in never reveals neighbours.
                targetPos = ClampCenter(targetPos, Mathf.Max(targetSize, cam.orthographicSize));
            }

            float newSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            Vector3 goal = new Vector3(targetPos.x, targetPos.y, transform.position.z);
            Vector3 newPos = Vector3.SmoothDamp(transform.position, goal, ref velocity, positionSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            cam.orthographicSize = newSize;
            Vector2 clamped = ClampCenter(newPos, newSize);
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
        }

        /// <summary>Whole-level view: the smallest view that contains the bounds width (height is centred).</summary>
        private void GetOverview(out Vector2 center, out float orthoSize)
        {
            float aspect = cam != null && cam.aspect > 0f ? cam.aspect : 16f / 9f;
            orthoSize = Mathf.Max(bounds.height * 0.5f, bounds.width * 0.5f / aspect);
            center = bounds.center;
        }

        /// <summary>Keeps the view inside the level bounds; centres on any axis where the view is larger.</summary>
        private Vector2 ClampCenter(Vector2 center, float orthoSize)
        {
            float aspect = cam != null && cam.aspect > 0f ? cam.aspect : 16f / 9f;
            float halfH = orthoSize;
            float halfW = orthoSize * aspect;

            float x = halfW * 2f >= bounds.width ? bounds.center.x : Mathf.Clamp(center.x, bounds.xMin + halfW, bounds.xMax - halfW);
            float y = halfH * 2f >= bounds.height ? bounds.center.y : Mathf.Clamp(center.y, bounds.yMin + halfH, bounds.yMax - halfH);
            return new Vector2(x, y);
        }

        private void ApplyImmediate(Vector2 center, float size)
        {
            if (cam == null) cam = GetComponent<Camera>();
            cam.orthographicSize = size;
            transform.position = new Vector3(center.x, center.y, transform.position.z);
        }

        private void Dispose()
        {
            if (events != null) events.LevelFramingChanged -= OnLevelFramingChanged;
            events = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
