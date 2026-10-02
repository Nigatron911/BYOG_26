using System.Collections.Generic;
using UnityEngine;
using Game.Core.Events;
using Game.Core.Interfaces;
using Game.Data.Items;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Coordinates mouse-driven drag-and-drop tool placement in the world.
    /// Strictly limits tool count to max 1 per type (Plank, Ladder, Platform).
    /// Supports direct rotation (R/E/Q/Wheel/RightClick/UI Button) and effortless repositioning.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 2 (No Singletons).
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float rotationStepDegrees = 15f;
        [SerializeField] private LayerMask toolLayerMask = ~0;
        [SerializeField] private UnityEngine.UIElements.UIDocument uiDocument;

        private IPlacementInput input;
        private GameEvents events;
        private Camera worldCamera;
        private Transform toolsContainer;
        private Dictionary<ToolType, ToolDefinition> definitionsLookup = new Dictionary<ToolType, ToolDefinition>();

        private DraggableTool activeTool;
        private DraggableTool lastManipulatedTool;
        private bool isSpawningNewTool = false;
        private bool isSimulating = false;
        private int framesSincePickup = 0;
        private bool isHoldingDrag = false;

        public bool IsDraggingTool => activeTool != null;
        public bool IsSimulating => isSimulating;
        public DraggableTool ActiveTool => activeTool;

        public void Initialize(
            IPlacementInput inputProvider,
            GameEvents gameEvents,
            Camera cam,
            Transform container,
            ToolDefinition[] definitions,
            ToolSpawner spawner = null)
        {
            input = inputProvider;
            events = gameEvents;
            worldCamera = cam != null ? cam : Camera.main;
            toolsContainer = container;

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
                events.ToolRotateRequested += OnToolRotateRequested;
            }
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
                events.ToolRotateRequested -= OnToolRotateRequested;
            }
        }

        private void OnToolRotateRequested()
        {
            RotateCurrentOrHoveredTool(rotationStepDegrees);
        }

        private void OnSimulationStarted()
        {
            isSimulating = true;
            if (activeTool != null)
            {
                ConfirmPlacement();
            }
        }

        private void OnSimulationStopped()
        {
            isSimulating = false;
            ResetAllPlacedTools();
        }

        private void OnPlayerDied(string reason)
        {
            isSimulating = false;
        }

        private void OnLevelCompleted()
        {
            isSimulating = false;
        }

        public void ResetAllPlacedTools()
        {
            if (toolsContainer != null)
            {
                var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
                foreach (var tool in tools)
                {
                    if (tool != null)
                    {
                        tool.ResetToPlacedTransform();
                    }
                }
            }
        }

        private void Update()
        {
            if (worldCamera == null) worldCamera = Camera.main;

            // Keyboard hotkeys for simulation toggle
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    if (events != null)
                    {
                        if (isSimulating) events.PublishSimulationStopped();
                        else events.PublishSimulationStarted();
                    }
                    return;
                }
            }

            // During simulation, tool placement and dragging is locked to keep physics clean
            if (isSimulating)
            {
                return;
            }

            // Keyboard hotkeys for tool selection / spawning in planning mode
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SelectOrSpawnTool(ToolType.Plank);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SelectOrSpawnTool(ToolType.Ladder);
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SelectOrSpawnTool(ToolType.Platform);

                if (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
                {
                    DeleteHoveredOrActiveTool();
                }

                // Dedicated rotation hotkeys: R or E (clockwise), Q (counter-clockwise)
                if (kb.rKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                {
                    RotateCurrentOrHoveredTool(rotationStepDegrees);
                }
                if (kb.qKey.wasPressedThisFrame)
                {
                    RotateCurrentOrHoveredTool(-rotationStepDegrees);
                }
            }

            if (input == null || worldCamera == null)
            {
                return;
            }

            Vector2 cursorWorld = input.GetCursorWorldPosition(worldCamera);
            bool isOverUI = IsPointerOverUI();

            // Right-click or mouse scroll rotation
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    RotateCurrentOrHoveredTool(rotationStepDegrees);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f) RotateCurrentOrHoveredTool(rotationStepDegrees);
                else if (scroll < -0.1f) RotateCurrentOrHoveredTool(-rotationStepDegrees);
            }

            // Handle active dragging with mouse
            if (activeTool != null)
            {
                framesSincePickup++;
                activeTool.SetPosition(cursorWorld);

                if (input.IsCancelRequested())
                {
                    CancelPlacement();
                    return;
                }

                // Place on click when not over UI and debounce passed
                if (framesSincePickup > 2 && !isOverUI)
                {
                    // Click-and-drag release (mouse up) OR click-to-place (mouse down)
                    if (isHoldingDrag && input.IsPointerUp())
                    {
                        ConfirmPlacement();
                        isHoldingDrag = false;
                    }
                    else if (!isHoldingDrag && input.IsPointerDown())
                    {
                        ConfirmPlacement();
                    }
                }
            }
            else
            {
                // Check if user clicked an existing placed tool in the world to pick it up and reposition
                if (input.IsPointerDown() && !isOverUI)
                {
                    DraggableTool toolUnderCursor = GetToolUnderCursor(cursorWorld);
                    if (toolUnderCursor != null)
                    {
                        PickUpTool(toolUnderCursor, isDragHold: true);
                    }
                }
            }
        }

        private void OnToolSelected(ToolType type)
        {
            SelectOrSpawnTool(type);
        }

        /// <summary>
        /// Selects or spawns a tool ensuring strictly max 1 instance per tool type.
        /// If already exists, picks up the existing one for repositioning.
        /// </summary>
        public void SelectOrSpawnTool(ToolType type)
        {
            if (isSimulating) return;

            // If we are currently holding a tool of the same type, drop it
            if (activeTool != null)
            {
                if (activeTool.Type == type)
                {
                    ConfirmPlacement();
                    return;
                }
                else
                {
                    ConfirmPlacement();
                }
            }

            int maxAllowed = definitionsLookup.TryGetValue(type, out var def) && def != null ? def.MaxCount : 1;
            int currentCount = CountExistingTools(type);

            if (currentCount >= maxAllowed)
            {
                // Check if tool already exists in toolsContainer to reposition
                DraggableTool existing = FindExistingTool(type);
                if (existing != null)
                {
                    PickUpTool(existing, isDragHold: false);
                    Debug.Log($"[PlacementSystem] Repositioning existing {type} (limit of {maxAllowed} reached)");
                    return;
                }
            }

            if (def == null || def.Prefab == null)
            {
                Debug.LogWarning($"[PlacementSystem] No prefab found for ToolType: {type}");
                return;
            }

            Vector2 cursorWorld = worldCamera != null 
                ? (input != null ? input.GetCursorWorldPosition(worldCamera) : (Vector2)worldCamera.transform.position)
                : Vector2.zero;

            GameObject instance = Instantiate(def.Prefab, new Vector3(cursorWorld.x, cursorWorld.y, 0f), Quaternion.identity, toolsContainer);
            instance.name = $"{def.DisplayName}_{currentCount + 1}";

            var tool = instance.GetComponent<DraggableTool>();
            if (tool == null)
            {
                tool = instance.AddComponent<DraggableTool>();
            }

            activeTool = tool;
            lastManipulatedTool = tool;
            activeTool.SetPreviewMode(true);
            isSpawningNewTool = true;
            framesSincePickup = 0;
            isHoldingDrag = false;

            Debug.Log($"[PlacementSystem] Spawned {type} #{currentCount + 1} attached to mouse cursor");
        }

        private void PickUpTool(DraggableTool tool, bool isDragHold)
        {
            if (tool == null) return;
            activeTool = tool;
            lastManipulatedTool = tool;
            activeTool.SetPreviewMode(true);
            isSpawningNewTool = false;
            framesSincePickup = 0;
            isHoldingDrag = isDragHold;
        }

        public void RotateCurrentOrHoveredTool(float deltaAngle)
        {
            if (isSimulating) return;

            if (activeTool != null)
            {
                activeTool.Rotate(deltaAngle);
                return;
            }

            // Check hovered tool under cursor
            if (worldCamera != null && input != null)
            {
                Vector2 cursorWorld = input.GetCursorWorldPosition(worldCamera);
                DraggableTool hovered = GetToolUnderCursor(cursorWorld);
                if (hovered != null)
                {
                    hovered.Rotate(deltaAngle);
                    lastManipulatedTool = hovered;
                    return;
                }
            }

            // Fallback: rotate the last manipulated tool if none under cursor
            if (lastManipulatedTool != null)
            {
                lastManipulatedTool.Rotate(deltaAngle);
            }
        }

        public DraggableTool GetToolUnderCursor(Vector2 worldPosition)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition, toolLayerMask);
            foreach (var hit in hits)
            {
                if (hit != null && !hit.isTrigger)
                {
                    DraggableTool tool = hit.GetComponentInParent<DraggableTool>();
                    if (tool != null) return tool;
                }
            }
            // Check triggers (ladder climb zone)
            foreach (var hit in hits)
            {
                if (hit != null)
                {
                    DraggableTool tool = hit.GetComponentInParent<DraggableTool>();
                    if (tool != null) return tool;
                }
            }
            return null;
        }

        public int CountExistingTools(ToolType type)
        {
            if (toolsContainer == null)
            {
                var go = GameObject.Find("Placed_Tools");
                if (go != null) toolsContainer = go.transform;
            }
            if (toolsContainer == null) return 0;

            int count = 0;
            var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
            foreach (var t in tools)
            {
                if (t != null && t.Type == type) count++;
            }
            return count;
        }

        private DraggableTool FindExistingTool(ToolType type)
        {
            if (toolsContainer == null)
            {
                var go = GameObject.Find("Placed_Tools");
                if (go != null) toolsContainer = go.transform;
            }
            if (toolsContainer == null) return null;

            var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
            foreach (var t in tools)
            {
                if (t != null && t.Type == type)
                {
                    return t;
                }
            }
            return null;
        }

        private void ConfirmPlacement()
        {
            if (activeTool == null) return;

            // Release with dynamic 2D physics
            activeTool.DropWithPhysics();

            if (events != null)
            {
                events.PublishToolPlaced(activeTool.Type, activeTool.transform.position);
            }

            lastManipulatedTool = activeTool;
            activeTool = null;
            isSpawningNewTool = false;
            framesSincePickup = 0;
            isHoldingDrag = false;
        }

        private void CancelPlacement()
        {
            if (activeTool == null) return;

            if (isSpawningNewTool)
            {
                Destroy(activeTool.gameObject);
            }
            else
            {
                activeTool.DropWithPhysics();
            }

            activeTool = null;
            isSpawningNewTool = false;
            framesSincePickup = 0;
            isHoldingDrag = false;
        }

        private void DeleteHoveredOrActiveTool()
        {
            if (activeTool != null)
            {
                Destroy(activeTool.gameObject);
                activeTool = null;
                isSpawningNewTool = false;
                return;
            }

            if (worldCamera != null && input != null)
            {
                Vector2 cursor = input.GetCursorWorldPosition(worldCamera);
                DraggableTool hovered = GetToolUnderCursor(cursor);
                if (hovered != null)
                {
                    Destroy(hovered.gameObject);
                }
            }
        }

        private bool IsPointerOverUI()
        {
            if (uiDocument == null)
            {
                uiDocument = FindFirstObjectByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Include);
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null && uiDocument.rootVisualElement.panel != null)
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    Vector2 mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    Vector2 panelPos = UnityEngine.UIElements.RuntimePanelUtils.ScreenToPanel(
                        uiDocument.rootVisualElement.panel,
                        new Vector2(mouseScreen.x, Screen.height - mouseScreen.y)
                    );
                    var picked = uiDocument.rootVisualElement.panel.Pick(panelPos);
                    if (picked != null && picked != uiDocument.rootVisualElement && picked.pickingMode == UnityEngine.UIElements.PickingMode.Position)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
