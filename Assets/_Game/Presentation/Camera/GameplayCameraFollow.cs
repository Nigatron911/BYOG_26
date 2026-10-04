using UnityEngine;
using Game.Core.Events;
using Game.Gameplay.Player;

namespace Game.Presentation.CameraSystems
{
    /// <summary>
    /// Presentation camera controller that smoothly frames and tracks the player during gameplay and simulation.
    /// In Planning phase (Levels 1-2 before simulate), keeps the camera framed on the puzzle overview.
    /// In Simulation & Manual locomotion (Levels 3-8), smoothly tracks the player with smooth damping,
    /// clamped within level bounds so the player remains clearly visible and parallax scrolling comes alive.
    /// Follows Section 14 (Presentation systems) and Section 15 (Fault isolation).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameplayCameraFollow : MonoBehaviour
    {
        [Header("Target & Tracking")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private Vector3 followOffset = new Vector3(3.0f, 2.5f, -10f);
        [SerializeField] private float smoothTime = 0.28f;

        [Header("Zoom Framing")]
        [Tooltip("Target orthographic size during active gameplay/simulation. Keeps the player well-proportioned.")]
        [SerializeField] private float gameplayOrthoSize = 16.0f;
        [SerializeField] private float zoomSmoothTime = 0.40f;

        [Header("Current Level State")]
        [SerializeField] private bool isFollowActive = false;
        [SerializeField] private Vector3 overviewPosition;
        [SerializeField] private float overviewOrthoSize = 19.57f;
        [SerializeField] private float levelMinX = -50f;
        [SerializeField] private float levelMaxX = 20f;
        [SerializeField] private float levelMinY = -5f;
        [SerializeField] private float levelMaxY = 30f;

        private Camera cam;
        private Vector3 currentVelocity = Vector3.zero;
        private float orthoVelocity = 0f;
        private GameEvents events;
        private AutonomousPlayerController playerController;

        public void Initialize(GameEvents gameEvents, Transform playerTransform = null)
        {
            events = gameEvents;
            if (playerTransform != null)
            {
                targetPlayer = playerTransform;
            }

            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStarted += OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
                events.SimulationStopped += OnSimulationStopped;
                events.LevelLoaded -= OnLevelLoaded;
                events.LevelLoaded += OnLevelLoaded;
            }
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            overviewPosition = transform.position;
            if (cam != null) overviewOrthoSize = cam.orthographicSize;
        }

        private void Start()
        {
            if (targetPlayer == null)
            {
                var player = FindFirstObjectByType<AutonomousPlayerController>();
                if (player != null)
                {
                    targetPlayer = player.transform;
                    playerController = player;
                }
            }
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
                events.LevelLoaded -= OnLevelLoaded;
            }
        }

        public void ConfigureLevel(int levelNumber, Vector3 camOverviewPos, float camOverviewOrtho, float minX, float maxX, float minY = -5f, float maxY = 30f)
        {
            overviewPosition = camOverviewPos;
            overviewOrthoSize = camOverviewOrtho;
            levelMinX = minX;
            levelMaxX = maxX;
            levelMinY = minY;
            levelMaxY = maxY;

            // In Levels 3 to 8 (manual/material locomotion), follow is active by default
            isFollowActive = levelNumber >= 3;
            currentVelocity = Vector3.zero;
            orthoVelocity = 0f;
        }

        public void SnapToOverview()
        {
            transform.position = overviewPosition;
            if (cam != null) cam.orthographicSize = overviewOrthoSize;
            currentVelocity = Vector3.zero;
            orthoVelocity = 0f;
        }

        private void OnSimulationStarted()
        {
            isFollowActive = true;
        }

        private void OnSimulationStopped()
        {
            // If in planning levels (1 or 2), return to overview
            if (targetPlayer != null && playerController != null && playerController.CurrentLocomotionMode == LocomotionMode.Autonomous)
            {
                isFollowActive = false;
            }
        }

        private void OnLevelLoaded(int levelNumber)
        {
            // Auto configure bounds based on level number
            float minX = -50f, maxX = 20f;
            switch (levelNumber)
            {
                case 1: minX = -45f; maxX = 16f; break;
                case 2: minX = 22f;  maxX = 82f; break;
                case 3: minX = 95f;  maxX = 180f; break;
                case 4: minX = 195f; maxX = 285f; break;
                case 5: minX = 302f; maxX = 385f; break;
                case 6: minX = 405f; maxX = 530f; break;
                case 7: minX = 548f; maxX = 650f; break;
                case 8: minX = 665f; maxX = 765f; break;
            }

            if (cam != null)
            {
                ConfigureLevel(levelNumber, transform.position, cam.orthographicSize, minX, maxX);
            }
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            if (targetPlayer == null)
            {
                var player = FindFirstObjectByType<AutonomousPlayerController>();
                if (player != null)
                {
                    targetPlayer = player.transform;
                    playerController = player;
                }
            }

            Vector3 targetPosition;
            float targetOrtho;

            if (isFollowActive && targetPlayer != null)
            {
                // Dynamic tracking
                float desiredX = targetPlayer.position.x + followOffset.x;
                float desiredY = targetPlayer.position.y + followOffset.y;

                float clampedX = Mathf.Clamp(desiredX, levelMinX, levelMaxX);
                float clampedY = Mathf.Clamp(desiredY, levelMinY, levelMaxY);

                targetPosition = new Vector3(clampedX, clampedY, overviewPosition.z);
                // In manual levels, use comfortable gameplay framing
                targetOrtho = Mathf.Min(overviewOrthoSize, gameplayOrthoSize);
            }
            else
            {
                // Overview framing
                targetPosition = overviewPosition;
                targetOrtho = overviewOrthoSize;
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrtho, ref orthoVelocity, zoomSmoothTime);
        }
    }
}
