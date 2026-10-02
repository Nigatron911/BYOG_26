using System.Collections.Generic;
using UnityEngine;
using Game.Core.Events;
using Game.Core.Interfaces;
using Game.Data.Items;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Spawner that drops physics-based tools from a top empty GameObject.
    /// Follows Section 4 (Single Responsibility) and Section 7 (Observer Pattern).
    /// </summary>
    public class ToolSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private Transform topSpawnPoint;
        [SerializeField] private float topElevationY = 3.6f;
        [SerializeField] private float minX = -7.5f;
        [SerializeField] private float maxX = 9.5f;
        [SerializeField] private bool followCursorX = true;

        [Header("Drop Indicator")]
        [SerializeField] private GameObject indicatorRoot;
        [SerializeField] private LineRenderer dropGuideLine;

        private GameEvents events;
        private IPlacementInput input;
        private Camera worldCamera;
        private Transform toolsContainer;
        private Dictionary<ToolType, ToolDefinition> definitionsLookup = new Dictionary<ToolType, ToolDefinition>();
        private bool isSimulating = false;

        public Transform TopSpawnPoint => topSpawnPoint;
        public Vector2 CurrentSpawnPosition => topSpawnPoint != null ? (Vector2)topSpawnPoint.position : new Vector2(0f, topElevationY);

        public void Initialize(
            GameEvents gameEvents,
            IPlacementInput inputProvider,
            Camera cam,
            Transform container,
            Transform spawnPoint,
            ToolDefinition[] definitions)
        {
            events = gameEvents;
            input = inputProvider;
            worldCamera = cam != null ? cam : Camera.main;
            toolsContainer = container;
            topSpawnPoint = spawnPoint;

            definitionsLookup.Clear();
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    if (def != null && !definitionsLookup.ContainsKey(def.ToolType))
                    {
                        definitionsLookup.Add(def.ToolType, def);
                    }
                }
            }

            if (events != null)
            {
                events.ToolSelected += OnToolSelected;
                events.SimulationStarted += OnSimulationStarted;
                events.SimulationStopped += OnSimulationStopped;
                events.PlayerDied += OnPlayerDied;
                events.LevelCompleted += OnLevelCompleted;
            }

            SetupGuideLine();
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.ToolSelected -= OnToolSelected;
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
                events.PlayerDied -= OnPlayerDied;
                events.LevelCompleted -= OnLevelCompleted;
            }
        }

        private void SetupGuideLine()
        {
            if (dropGuideLine == null && indicatorRoot != null)
            {
                dropGuideLine = indicatorRoot.GetComponent<LineRenderer>();
            }

            if (dropGuideLine != null)
            {
                dropGuideLine.positionCount = 2;
                dropGuideLine.startWidth = 0.05f;
                dropGuideLine.endWidth = 0.05f;
                dropGuideLine.useWorldSpace = true;
            }
        }

        private void Update()
        {
            if (worldCamera == null) worldCamera = Camera.main;

            // Track mouse X position along top ceiling during planning mode
            if (!isSimulating && followCursorX && input != null && worldCamera != null)
            {
                Vector2 cursorWorld = input.GetCursorWorldPosition(worldCamera);
                float clampedX = Mathf.Clamp(cursorWorld.x, minX, maxX);
                Vector3 newPos = new Vector3(clampedX, topElevationY, 0f);

                if (topSpawnPoint != null)
                {
                    topSpawnPoint.position = newPos;
                }
                else
                {
                    transform.position = newPos;
                }

                UpdateGuideLine(newPos);
            }
        }

        private void UpdateGuideLine(Vector3 spawnerPos)
        {
            if (dropGuideLine != null && dropGuideLine.enabled)
            {
                // Raycast straight down to ground
                RaycastHit2D hit = Physics2D.Raycast(spawnerPos, Vector2.down, 10f, ~0);
                float groundY = hit.collider != null ? hit.point.y : -2.5f;

                dropGuideLine.SetPosition(0, spawnerPos);
                dropGuideLine.SetPosition(1, new Vector3(spawnerPos.x, groundY, 0f));
            }
        }

        private void OnToolSelected(ToolType type)
        {
            if (isSimulating) return;
            SpawnAndDropTool(type);
        }

        public GameObject SpawnAndDropTool(ToolType type, Vector2? customDropPos = null)
        {
            if (!definitionsLookup.TryGetValue(type, out var def) || def == null || def.Prefab == null)
            {
                Debug.LogWarning($"[ToolSpawner] No prefab registered for ToolType: {type}");
                return null;
            }

            Vector3 spawnPos = customDropPos.HasValue 
                ? new Vector3(customDropPos.Value.x, topElevationY, 0f)
                : (topSpawnPoint != null ? topSpawnPoint.position : new Vector3(0f, topElevationY, 0f));

            // Instantiate tool under toolsContainer
            GameObject toolGO = Instantiate(def.Prefab, spawnPos, Quaternion.identity, toolsContainer);
            toolGO.name = $"{def.DisplayName}_{Time.frameCount}";

            var draggable = toolGO.GetComponent<DraggableTool>();
            if (draggable == null)
            {
                draggable = toolGO.AddComponent<DraggableTool>();
            }

            // Drop with initial downward velocity so it falls physically
            draggable.DropWithPhysics(new Vector2(0f, -0.5f));

            Debug.Log($"[ToolSpawner] Spawned {type} at {spawnPos} with physics drop!");

            if (events != null)
            {
                events.PublishToolPlaced(type, spawnPos);
            }

            return toolGO;
        }

        private void OnSimulationStarted()
        {
            isSimulating = true;
            if (indicatorRoot != null) indicatorRoot.SetActive(false);
            if (dropGuideLine != null) dropGuideLine.enabled = false;
        }

        private void OnSimulationStopped()
        {
            isSimulating = false;
            if (indicatorRoot != null) indicatorRoot.SetActive(true);
            if (dropGuideLine != null) dropGuideLine.enabled = true;
        }

        private void OnPlayerDied(string reason)
        {
            isSimulating = false;
        }

        private void OnLevelCompleted()
        {
            isSimulating = false;
        }
    }
}
