using UnityEngine;
using UnityEngine.UIElements;
using Game.Core.Events;
using Game.Gameplay.Interaction;

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
        private Button btnChain;
        private Label countPlank;
        private Label countLadder;
        private Label countPlatform;
        private Label countChain;
        private Button btnRotate;
        private Button btnSimulate;
        private Label labelSimulate;
        private Transform toolsContainer;
        private VisualElement toolbarContainer;
        private VisualElement toolbarDock;
        private int maxPlanks = 1;
        private int maxLadders = 1;
        private int maxPlatforms = 1;
        private int maxChains = 0;

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

            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
            {
                OnNextLevelClicked();
            }

            RefreshCounts();
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
            btnChain = root.Q<Button>("Btn_Chain");
            countPlank = root.Q<Label>("Count_Plank");
            countLadder = root.Q<Label>("Count_Ladder");
            countPlatform = root.Q<Label>("Count_Platform");
            countChain = root.Q<Label>("Count_Chain");
            btnRotate = root.Q<Button>("Btn_Rotate");
            btnSimulate = root.Q<Button>("Btn_Simulate");
            labelSimulate = root.Q<Label>("Label_Simulate");
            toolbarContainer = root.Q<VisualElement>("BottomToolbarContainer");
            toolbarDock = root.Q<VisualElement>("ToolbarDock");

            if (btnPlank != null) btnPlank.clicked += OnPlankClicked;
            if (btnLadder != null) btnLadder.clicked += OnLadderClicked;
            if (btnPlatform != null) btnPlatform.clicked += OnPlatformClicked;
            if (btnChain != null) btnChain.clicked += OnChainClicked;
            if (btnRotate != null) btnRotate.clicked += OnRotateClicked;
            if (btnSimulate != null) btnSimulate.clicked += OnSimulateClicked;
            root.Query<Button>("Btn_NextLevel").ForEach(btn =>
            {
                btn.clicked -= OnNextLevelClicked;
                btn.clicked += OnNextLevelClicked;
            });

            UpdateChainVisibility();
            SetSimulatingState(isSimulating);
            RefreshCounts();
        }

        public void SetToolbarVisible(bool visible)
        {
            if (toolbarContainer == null || toolbarDock == null)
            {
                BindUI();
            }

            if (toolbarContainer != null)
            {
                toolbarContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (toolbarDock != null)
            {
                toolbarDock.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetToolLimits(int planks, int ladders, int platforms, int chains)
        {
            maxPlanks = planks;
            maxLadders = ladders;
            maxPlatforms = platforms;
            maxChains = chains;

            int totalTools = planks + ladders + platforms + chains;
            if (totalTools == 0)
            {
                SetToolbarVisible(false);
            }
            else
            {
                SetToolbarVisible(true);
            }

            UpdateChainVisibility();
            RefreshCounts();
        }

        private void UpdateChainVisibility()
        {
            if (btnChain != null)
            {
                btnChain.style.display = maxChains > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void RefreshCounts()
        {
            if (toolsContainer == null)
            {
                var go = GameObject.Find("Placed_Tools");
                if (go != null) toolsContainer = go.transform;
            }

            int planks = 0, ladders = 0, platforms = 0, chains = 0;
            if (toolsContainer != null)
            {
                var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
                foreach (var t in tools)
                {
                    if (t == null) continue;
                    if (t.Type == ToolType.Plank) planks++;
                    else if (t.Type == ToolType.Ladder) ladders++;
                    else if (t.Type == ToolType.Platform) platforms++;
                    else if (t.Type == ToolType.Chain) chains++;
                }
            }

            if (countPlank != null) countPlank.text = $"x{Mathf.Max(0, maxPlanks - planks)}";
            if (countLadder != null) countLadder.text = $"x{Mathf.Max(0, maxLadders - ladders)}";
            if (countPlatform != null) countPlatform.text = $"x{Mathf.Max(0, maxPlatforms - platforms)}";
            if (countChain != null) countChain.text = $"x{Mathf.Max(0, maxChains - chains)}";
        }

        private void UnbindButtons()
        {
            if (btnPlank != null) btnPlank.clicked -= OnPlankClicked;
            if (btnLadder != null) btnLadder.clicked -= OnLadderClicked;
            if (btnPlatform != null) btnPlatform.clicked -= OnPlatformClicked;
            if (btnChain != null) btnChain.clicked -= OnChainClicked;
            if (btnRotate != null) btnRotate.clicked -= OnRotateClicked;
            if (btnSimulate != null) btnSimulate.clicked -= OnSimulateClicked;
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.Query<Button>("Btn_NextLevel").ForEach(btn =>
                {
                    btn.clicked -= OnNextLevelClicked;
                });
            }
        }

        private void OnPlankClicked() => OnButtonClicked(ToolType.Plank);
        private void OnLadderClicked() => OnButtonClicked(ToolType.Ladder);
        private void OnPlatformClicked() => OnButtonClicked(ToolType.Platform);
        private void OnChainClicked() => OnButtonClicked(ToolType.Chain);

        private void OnNextLevelClicked()
        {
            events?.PublishSkipLevelRequested();
        }

        private void OnButtonClicked(ToolType toolType)
        {
            if (isSimulating) return;
            events?.PublishToolSelected(toolType);
        }

        private void OnRotateClicked()
        {
            if (isSimulating) return;
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

            // Disable tool buttons during simulation so player cannot place objects while simulating
            btnPlank?.SetEnabled(!simulating);
            btnLadder?.SetEnabled(!simulating);
            btnPlatform?.SetEnabled(!simulating);
            btnChain?.SetEnabled(!simulating);
            btnRotate?.SetEnabled(!simulating);
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.Query<Button>("Btn_NextLevel").ForEach(btn => btn?.SetEnabled(true));
            }
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
