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
        private Dictionary<ToolType, int> customToolLimits = new Dictionary<ToolType, int>();

        private DraggableTool activeTool;
        private DraggableTool lastManipulatedTool;
        private bool isSpawningNewTool = false;
        private bool isSimulating = false;
        private int framesSincePickup = 0;
        private bool isHoldingDrag = false;

        private bool isPlacingChain = false;
        private Vector2? chainFirstPoint = null;
        private GameObject chainPreviewGO;
        private LineRenderer chainPreviewLine;
        private GameObject anchorPreviewA;
        private GameObject anchorPreviewB;

        [Header("Lifetime Settings")]
        [SerializeField] private float toolLifetimeSeconds = 0f;

        public bool IsDraggingTool => activeTool != null || isPlacingChain;
        public bool IsSimulating => isSimulating;
        public DraggableTool ActiveTool => activeTool;
        public float ToolLifetimeSeconds => toolLifetimeSeconds;

        public void SetToolLifetime(float seconds)
        {
            toolLifetimeSeconds = seconds;
        }

        public void SetToolLimit(ToolType type, int count)
        {
            customToolLimits[type] = count;
        }

        public int GetMaxAllowed(ToolType type)
        {
            if (customToolLimits.TryGetValue(type, out int limit)) return limit;
            return definitionsLookup.TryGetValue(type, out var def) && def != null ? def.MaxCount : 1;
        }

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

            if (!definitionsLookup.ContainsKey(ToolType.Chain))
            {
                var chainDef = UnityEditor.AssetDatabase.LoadAssetAtPath<ToolDefinition>("Assets/_Game/Data/Items/Tool_Chain.asset");
                if (chainDef != null) definitionsLookup[ToolType.Chain] = chainDef;
            }

            if (events != null)
            {
                events.ToolSelected += OnToolSelected;
                events.SimulationStarted += OnSimulationStarted;
                events.SimulationStopped += OnSimulationStopped;
                events.PlayerDied += OnPlayerDied;
                events.LevelCompleted += OnLevelCompleted;
                events.LevelResetRequested += OnLevelResetRequested;
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
                events.LevelResetRequested -= OnLevelResetRequested;
                events.ToolRotateRequested -= OnToolRotateRequested;
            }
        }

        private void OnToolRotateRequested()
        {
            if (isSimulating) return;
            RotateCurrentOrHoveredTool(rotationStepDegrees);
        }

        private void OnSimulationStarted()
        {
            isSimulating = true;
            if (activeTool != null)
            {
                ConfirmPlacement();
            }
            CancelChainPlacement();
            SetAllToolsSimulating(true);
        }

        private void OnSimulationStopped()
        {
            isSimulating = false;
            SetAllToolsSimulating(false);
            CancelChainPlacement();
            ResetAllPlacedTools();
        }

        private void OnPlayerDied(string reason)
        {
            isSimulating = false;
            SetAllToolsSimulating(false);
            CancelChainPlacement();
        }

        private void OnLevelCompleted()
        {
            isSimulating = false;
            SetAllToolsSimulating(false);
            CancelChainPlacement();
        }

        private void OnLevelResetRequested()
        {
            isSimulating = false;
            SetAllToolsSimulating(false);
            CancelChainPlacement();
            CancelPlacement();
            ResetAllPlacedTools();
        }

        private void SetAllToolsSimulating(bool simulating)
        {
            if (toolsContainer == null)
            {
                var go = GameObject.Find("Placed_Tools");
                if (go != null) toolsContainer = go.transform;
            }
            if (toolsContainer == null) return;

            var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
            foreach (var tool in tools)
            {
                if (tool != null) tool.SetSimulating(simulating);
            }
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
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    if (events != null)
                    {
                        if (isSimulating) events.PublishSimulationStopped();
                        else events.PublishSimulationStarted();
                    }
                    return;
                }

                // Testing hotkey: N to skip level
                if (kb.nKey.wasPressedThisFrame)
                {
                    events?.PublishSkipLevelRequested();
                    return;
                }
            }

            // Lock all tool placement and manipulation during simulation
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
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SelectOrSpawnTool(ToolType.Chain);

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

            if (isPlacingChain)
            {
                HandleChainPlacementUpdate(cursorWorld, isOverUI);
                return;
            }

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
            if (isSimulating)
            {
                Debug.Log("[PlacementSystem] Tool selection blocked: Simulation is active.");
                return;
            }

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

            definitionsLookup.TryGetValue(type, out var def);
            int maxAllowed = GetMaxAllowed(type);
            int currentCount = CountExistingTools(type);

            if (type == ToolType.Chain)
            {
                if (currentCount >= maxAllowed)
                {
                    Debug.Log($"[PlacementSystem] Chain limit of {maxAllowed} reached.");
                    return;
                }
                StartChainPlacement(def);
                return;
            }

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

            tool.SetLifetime(toolLifetimeSeconds);
            activeTool = tool;
            lastManipulatedTool = tool;
            activeTool.SetPreviewMode(true);
            isSpawningNewTool = true;
            framesSincePickup = 0;
            isHoldingDrag = false;

            Debug.Log($"[PlacementSystem] Spawned {type} #{currentCount + 1} attached to mouse cursor (lifetime {toolLifetimeSeconds}s)");
        }

        private void PickUpTool(DraggableTool tool, bool isDragHold)
        {
            if (isSimulating || tool == null) return;
            tool.SetLifetime(toolLifetimeSeconds);
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

        public void StartChainPlacement(ToolDefinition def)
        {
            if (isSimulating) return;
            if (activeTool != null) ConfirmPlacement();
            CancelChainPlacement();

            isPlacingChain = true;
            chainFirstPoint = null;
            EnsureChainPreview();
            Debug.Log("[PlacementSystem] Started Chain Placement. Click/attach first spot.");
        }

        private void EnsureChainPreview()
        {
            if (chainPreviewGO == null)
            {
                chainPreviewGO = new GameObject("_ChainPreview");
                chainPreviewLine = chainPreviewGO.AddComponent<LineRenderer>();
                chainPreviewLine.material = new Material(Shader.Find("Sprites/Default"));
                chainPreviewLine.startColor = new Color(0.95f, 0.80f, 0.25f, 0.90f);
                chainPreviewLine.endColor = new Color(0.95f, 0.80f, 0.25f, 0.90f);
                chainPreviewLine.startWidth = 0.15f;
                chainPreviewLine.endWidth = 0.15f;
                chainPreviewLine.positionCount = 0;

                Sprite anchorSprite = null;
                if (definitionsLookup.TryGetValue(ToolType.Chain, out var def) && def != null && def.Prefab != null)
                {
                    var ct = def.Prefab.GetComponent<ChainTool>();
                    if (ct != null) anchorSprite = ct.AnchorSprite;
                }
                if (anchorSprite == null)
                {
                    anchorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_AnchorPin.png");
                }

                anchorPreviewA = new GameObject("AnchorA");
                anchorPreviewA.transform.SetParent(chainPreviewGO.transform);
                var srA = anchorPreviewA.AddComponent<SpriteRenderer>();
                srA.sprite = anchorSprite;
                srA.sortingOrder = 15;
                anchorPreviewA.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                anchorPreviewB = new GameObject("AnchorB");
                anchorPreviewB.transform.SetParent(chainPreviewGO.transform);
                var srB = anchorPreviewB.AddComponent<SpriteRenderer>();
                srB.sprite = anchorSprite;
                srB.sortingOrder = 15;
                anchorPreviewB.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                anchorPreviewB.SetActive(false);
            }
            chainPreviewGO.SetActive(true);
        }

        private void HandleChainPlacementUpdate(Vector2 cursorWorld, bool isOverUI)
        {
            if (input == null) return;

            if (input.IsCancelRequested() || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame))
            {
                CancelChainPlacement();
                return;
            }

            EnsureChainPreview();

            if (!chainFirstPoint.HasValue)
            {
                if (anchorPreviewA != null)
                {
                    anchorPreviewA.SetActive(true);
                    anchorPreviewA.transform.position = new Vector3(cursorWorld.x, cursorWorld.y, -0.05f);
                }
                if (anchorPreviewB != null) anchorPreviewB.SetActive(false);
                if (chainPreviewLine != null) chainPreviewLine.positionCount = 0;

                if (!isOverUI && input.IsPointerDown())
                {
                    chainFirstPoint = cursorWorld;
                    if (anchorPreviewA != null)
                    {
                        anchorPreviewA.transform.position = new Vector3(cursorWorld.x, cursorWorld.y, -0.05f);
                    }
                    if (anchorPreviewB != null) anchorPreviewB.SetActive(true);
                    Debug.Log($"[PlacementSystem] Chain Anchor 1 attached at {chainFirstPoint.Value}");
                }
            }
            else
            {
                Vector2 start = chainFirstPoint.Value;
                Vector2 end = cursorWorld;

                if (anchorPreviewB != null)
                {
                    anchorPreviewB.SetActive(true);
                    anchorPreviewB.transform.position = new Vector3(end.x, end.y, -0.05f);
                }

                float span = Vector2.Distance(start, end);
                float sag = Mathf.Clamp(span * 0.04f, 0.05f, 0.15f);
                int segs = Mathf.Max(4, Mathf.RoundToInt(span / 0.35f));

                if (chainPreviewLine != null)
                {
                    chainPreviewLine.positionCount = segs + 1;
                    for (int i = 0; i <= segs; i++)
                    {
                        float t = (float)i / segs;
                        Vector2 pt = Vector2.Lerp(start, end, t) + Vector2.down * (4f * t * (1f - t) * sag);
                        chainPreviewLine.SetPosition(i, new Vector3(pt.x, pt.y, -0.02f));
                    }
                }

                bool shouldConfirm = false;
                if (!isOverUI)
                {
                    if (input.IsPointerDown() && span >= 0.4f) shouldConfirm = true;
                    else if (input.IsPointerUp() && span >= 0.8f) shouldConfirm = true;
                }

                if (shouldConfirm)
                {
                    SpawnAndPlaceChain(start, end);
                    CancelChainPlacement();
                }
            }
        }

        private void SpawnAndPlaceChain(Vector2 start, Vector2 end)
        {
            if (start.x > end.x)
            {
                var tmp = start;
                start = end;
                end = tmp;
            }

            GameObject prefab = null;
            if (definitionsLookup.TryGetValue(ToolType.Chain, out var def) && def != null)
            {
                prefab = def.Prefab;
            }
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Data/Prefabs/ChainPrefab.prefab");
            }

            if (prefab == null)
            {
                Debug.LogError("[PlacementSystem] Could not load ChainPrefab!");
                return;
            }

            int count = CountExistingTools(ToolType.Chain);
            var go = Instantiate(prefab, toolsContainer);
            go.name = $"Chain_{count + 1}";

            var chainTool = go.GetComponent<ChainTool>();
            if (chainTool != null)
            {
                chainTool.Initialize(start, end, lifetime: toolLifetimeSeconds);
            }

            var drag = go.GetComponent<DraggableTool>();
            if (drag != null)
            {
                lastManipulatedTool = drag;
            }

            events?.PublishToolPlaced(ToolType.Chain, end);
            Debug.Log($"[PlacementSystem] Chain attached between {start} and {end} (lifetime {toolLifetimeSeconds}s)!");
        }

        public void CancelChainPlacement()
        {
            isPlacingChain = false;
            chainFirstPoint = null;
            if (chainPreviewGO != null)
            {
                chainPreviewGO.SetActive(false);
            }
        }

        private void ConfirmPlacement()
        {
            if (activeTool == null) return;

            // Apply level-specific lifetime and release with dynamic 2D physics
            activeTool.SetLifetime(toolLifetimeSeconds);
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
            CancelChainPlacement();

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
