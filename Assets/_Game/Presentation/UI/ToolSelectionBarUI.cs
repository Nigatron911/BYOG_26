using UnityEngine;
using UnityEngine.UIElements;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Displays the tool selection options at the bottom of the screen (Plank, Ladder, Platform).
    /// Built on UI Toolkit (UnityEngine.UIElements).
    /// Publishes ToolSelected events without knowing about placement internals.
    /// Strictly adheres to Section 13 (UI must not own gameplay logic).
    /// </summary>
    public class ToolSelectionBarUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        private GameEvents events;
        private bool isSimulating = false;

        private Button btnPlank;
        private Button btnLadder;
        private Button btnPlatform;
        private Label countPlank;
        private Label countLadder;
        private Label countPlatform;
        private Button btnRotate;
        private Button btnSimulate;
        private Label labelSimulate;
        private Button btnReset;
        private Transform toolsContainer;
        private int maxPerTool = 2;

        private void Awake()
        {
            EnsureDocument();
        }

        private void OnEnable()
        {
            BindUI();
        }

        private void EnsureDocument()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>() ?? GetComponentInParent<UIDocument>();
            }
        }

        public void Initialize(GameEvents gameEvents, UIDocument document = null, Transform container = null)
        {
            if (document != null) uiDocument = document;
            if (container != null) toolsContainer = container;
            events = gameEvents;

            EnsureDocument();
            BindUI();

            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStarted += OnSimulationStarted;

                events.SimulationStopped -= OnSimulationStopped;
                events.SimulationStopped += OnSimulationStopped;

                events.PlayerDied -= OnPlayerDied;
                events.PlayerDied += OnPlayerDied;

                events.LevelCompleted -= OnLevelCompleted;
                events.LevelCompleted += OnLevelCompleted;

                events.LevelResetRequested -= OnLevelResetRequested;
                events.LevelResetRequested += OnLevelResetRequested;

                events.ToolPlaced -= OnToolPlaced;
                events.ToolPlaced += OnToolPlaced;
            }

            SetSimulatingState(false);
            RefreshCounts();
        }

        private void OnToolPlaced(ToolType type, Vector2 pos)
        {
            RefreshCounts();
        }

        private void Update()
        {
            if (btnPlank == null)
            {
                BindUI();
            }
        }

        private void BindUI()
        {
            EnsureDocument();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;

            UnbindButtons();

            btnPlank = root.Q<Button>("Btn_Plank");
            btnLadder = root.Q<Button>("Btn_Ladder");
            btnPlatform = root.Q<Button>("Btn_Platform");
            countPlank = root.Q<Label>("Count_Plank");
            countLadder = root.Q<Label>("Count_Ladder");
            countPlatform = root.Q<Label>("Count_Platform");
            btnRotate = root.Q<Button>("Btn_Rotate");
            btnSimulate = root.Q<Button>("Btn_Simulate");
            labelSimulate = root.Q<Label>("Label_Simulate");
            btnReset = root.Q<Button>("Btn_Reset");

            if (btnPlank != null) btnPlank.clicked += OnPlankClicked;
            if (btnLadder != null) btnLadder.clicked += OnLadderClicked;
            if (btnPlatform != null) btnPlatform.clicked += OnPlatformClicked;
            if (btnRotate != null) btnRotate.clicked += OnRotateClicked;
            if (btnSimulate != null) btnSimulate.clicked += OnSimulateClicked;
            if (btnReset != null) btnReset.clicked += OnResetClicked;

            SetSimulatingState(isSimulating);
            RefreshCounts();
        }

        public void RefreshCounts()
        {
            if (toolsContainer == null)
            {
                var go = GameObject.Find("Placed_Tools");
                if (go != null) toolsContainer = go.transform;
            }

            int planks = 0, ladders = 0, platforms = 0;
            if (toolsContainer != null)
            {
                var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
                foreach (var t in tools)
                {
                    if (t == null) continue;
                    if (t.Type == ToolType.Plank) planks++;
                    else if (t.Type == ToolType.Ladder) ladders++;
                    else if (t.Type == ToolType.Platform) platforms++;
                }
            }

            if (countPlank != null) countPlank.text = $"x{Mathf.Max(0, maxPerTool - planks)}";
            if (countLadder != null) countLadder.text = $"x{Mathf.Max(0, maxPerTool - ladders)}";
            if (countPlatform != null) countPlatform.text = $"x{Mathf.Max(0, maxPerTool - platforms)}";
        }

        private void UnbindButtons()
        {
            if (btnPlank != null) btnPlank.clicked -= OnPlankClicked;
            if (btnLadder != null) btnLadder.clicked -= OnLadderClicked;
            if (btnPlatform != null) btnPlatform.clicked -= OnPlatformClicked;
            if (btnRotate != null) btnRotate.clicked -= OnRotateClicked;
            if (btnSimulate != null) btnSimulate.clicked -= OnSimulateClicked;
            if (btnReset != null) btnReset.clicked -= OnResetClicked;
        }

        private void OnPlankClicked() => OnButtonClicked(ToolType.Plank);
        private void OnLadderClicked() => OnButtonClicked(ToolType.Ladder);
        private void OnPlatformClicked() => OnButtonClicked(ToolType.Platform);

        private void OnButtonClicked(ToolType toolType)
        {
            events?.PublishToolSelected(toolType);
        }

        private void OnRotateClicked()
        {
            events?.PublishToolRotateRequested();
        }

        private void OnSimulateClicked()
        {
            if (events == null) return;

            if (!isSimulating)
            {
                events.PublishSimulationStarted();
            }
            else
            {
                events.PublishSimulationStopped();
            }
        }

        private void OnResetClicked()
        {
            events?.PublishLevelResetRequested();
        }

        private void OnSimulationStarted() => SetSimulatingState(true);
        private void OnSimulationStopped()
        {
            SetSimulatingState(false);
            RefreshCounts();
        }
        private void OnPlayerDied(string reason) => SetSimulatingState(false);
        private void OnLevelCompleted() => SetSimulatingState(false);
        private void OnLevelResetRequested()
        {
            SetSimulatingState(false);
            RefreshCounts();
        }

        private void SetSimulatingState(bool simulating)
        {
            isSimulating = simulating;

            if (labelSimulate != null)
            {
                labelSimulate.text = simulating ? "■ STOP" : "▶ SIMULATE";
            }

            if (btnSimulate != null)
            {
                if (simulating)
                {
                    btnSimulate.AddToClassList("btn-simulate-active");
                }
                else
                {
                    btnSimulate.RemoveFromClassList("btn-simulate-active");
                }
            }

            // Disable tool spawning and rotation buttons while simulating
            btnPlank?.SetEnabled(!simulating);
            btnLadder?.SetEnabled(!simulating);
            btnPlatform?.SetEnabled(!simulating);
            btnRotate?.SetEnabled(!simulating);
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
                events.PlayerDied -= OnPlayerDied;
                events.LevelCompleted -= OnLevelCompleted;
                events.LevelResetRequested -= OnLevelResetRequested;
                events.ToolPlaced -= OnToolPlaced;
            }

            UnbindButtons();
        }
    }
}
