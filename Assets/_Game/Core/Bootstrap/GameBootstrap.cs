using System.Linq;
using UnityEngine;
using Game.Core.Events;
using Game.Core.Input;
using Game.Core.Interfaces;
using Game.Core.Flow;
using Game.Core.Save;
using Game.Data.Audio;
using Game.Data.Items;
using Game.Gameplay.Environment;
using Game.Gameplay.Transmutation;
using Game.Presentation.Audio;
using Game.Presentation.Environment;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Presentation.UI;

namespace Game.Core.Bootstrap
{
    /// <summary>
    /// Composition Root of the scene.
    /// Strictly adheres to Section 3 of the Architecture Contract:
    /// Responsibilities are strictly: CREATE, CONFIGURE, CONNECT, INITIALIZE.
    /// Contains NO gameplay logic or Singletons.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private AutonomousPlayerController playerController;
        [SerializeField] private PlacementSystem placementSystem;
        [SerializeField] private ToolSpawner toolSpawner;
        [SerializeField] private Transform toolsContainer;
        [SerializeField] private Game.Gameplay.LevelProgressionManager levelManager;

        [Header("Presentation References")]
        [SerializeField] private ScreenFaderUI screenFader;
        [SerializeField] private ToolSelectionBarUI toolSelectionBar;
        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private PlayerHUDUI playerHUD;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private PauseMenuUI pauseMenuUI;
        [SerializeField] private VictoryUI victoryUI;
        [SerializeField] private LevelBriefingUI briefingUI;
        [SerializeField] private CreditsUI creditsUI;
        [SerializeField] private GameAudioController audioController;
        [SerializeField] private PaperBackdrop backdrop;

        [Header("Configuration")]
        [SerializeField] private ToolDefinition[] toolDefinitions;
        [SerializeField] private GameAudioLibrary audioLibrary;
        [Tooltip("Show the title screen when the scene starts. Disable to jump straight into Level 1 while iterating.")]
        [SerializeField] private bool showMainMenuOnStart = true;

        // Core Infrastructure Instances
        private GameEvents gameEvents;
        private IPlacementInput placementInput;
        private ProgressStore progressStore;
        private GameFlowController flowController;

        private void Awake()
        {
            Application.runInBackground = true;
            AssembleDependencies();
        }

        private void OnEnable()
        {
            if (gameEvents == null)
            {
                AssembleDependencies();
            }
        }

        private void Start()
        {
            InitializeScene();
        }

        private void AssembleDependencies()
        {
            // 1. Create Core Services
            gameEvents = new GameEvents();
            placementInput = new NewInputSystemPlacementReader();
            progressStore = new ProgressStore();

            if (mainCamera == null) mainCamera = Camera.main;
            if (playerController == null) playerController = FindFirstObjectByType<Game.Gameplay.Player.AutonomousPlayerController>();
            if (placementSystem == null) placementSystem = GetComponent<Game.Gameplay.Interaction.PlacementSystem>() ?? FindFirstObjectByType<Game.Gameplay.Interaction.PlacementSystem>();
            if (toolsContainer == null)
            {
                var toolsGO = GameObject.Find("Placed_Tools");
                if (toolsGO != null) toolsContainer = toolsGO.transform;
            }
            if (toolSelectionBar == null) toolSelectionBar = FindFirstObjectByType<Game.Presentation.UI.ToolSelectionBarUI>(FindObjectsInactive.Include);
            if (gameOverUI == null) gameOverUI = FindFirstObjectByType<Game.Presentation.UI.GameOverUI>(FindObjectsInactive.Include);
            if (playerHUD == null) playerHUD = FindFirstObjectByType<Game.Presentation.UI.PlayerHUDUI>(FindObjectsInactive.Include);
            if (screenFader == null) screenFader = FindFirstObjectByType<Game.Presentation.UI.ScreenFaderUI>(FindObjectsInactive.Include);

            // 2. Wire Gameplay Systems
            if (playerController != null)
            {
                playerController.Initialize(gameEvents);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] PlayerController reference is missing.", this);
            }

            if (toolSpawner != null)
            {
                var spawnPoint = toolSpawner.TopSpawnPoint != null ? toolSpawner.TopSpawnPoint : toolSpawner.transform;
                toolSpawner.Initialize(gameEvents, placementInput, mainCamera, toolsContainer, spawnPoint, toolDefinitions);
            }

            if (placementSystem != null)
            {
                placementSystem.Initialize(placementInput, gameEvents, mainCamera, toolsContainer, toolDefinitions, toolSpawner);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] PlacementSystem reference is missing.", this);
            }

            // 3. Wire Presentation Systems
            if (toolSelectionBar != null)
            {
                toolSelectionBar.Initialize(gameEvents, container: toolsContainer);
            }

            if (gameOverUI != null)
            {
                gameOverUI.Initialize(gameEvents);
            }

            if (playerHUD != null && playerController != null)
            {
                playerHUD.BindPlayer(playerController, null, gameEvents);
            }

            // Camera listens before the level manager publishes the first level's framing.
            if (mainCamera != null)
            {
                var cameraFollow = mainCamera.GetComponent<Game.Presentation.CameraSystems.GameplayCameraFollow>();
                if (cameraFollow == null) cameraFollow = mainCamera.gameObject.AddComponent<Game.Presentation.CameraSystems.GameplayCameraFollow>();
                cameraFollow.Initialize(gameEvents, playerController != null ? playerController.transform : null);
            }

            if (levelManager == null)
            {
                levelManager = GetComponent<Game.Gameplay.LevelProgressionManager>() 
                    ?? FindFirstObjectByType<Game.Gameplay.LevelProgressionManager>() 
                    ?? gameObject.AddComponent<Game.Gameplay.LevelProgressionManager>();
            }

            if (levelManager != null)
            {
                levelManager.Initialize(gameEvents, playerController, mainCamera, placementSystem, toolSelectionBar, screenFader, toolsContainer);
            }


            // 4. Game flow, menus and audio
            WireGameFlow();

            // 5. Hook Lifecycle Transitions
            gameEvents.LevelResetRequested += OnLevelResetRequested;
        }

        private void WireGameFlow()
        {
            UnityEngine.UIElements.UIDocument uiDoc = playerHUD != null ? playerHUD.GetComponent<UnityEngine.UIElements.UIDocument>() : null;
            if (uiDoc == null) uiDoc = FindFirstObjectByType<UnityEngine.UIElements.UIDocument>();
            GameObject uiHost = uiDoc != null ? uiDoc.gameObject : gameObject;

            if (mainMenuUI == null) mainMenuUI = GetOrAdd<MainMenuUI>(uiHost);
            if (pauseMenuUI == null) pauseMenuUI = GetOrAdd<PauseMenuUI>(uiHost);
            if (victoryUI == null) victoryUI = GetOrAdd<VictoryUI>(uiHost);
            if (briefingUI == null) briefingUI = GetOrAdd<LevelBriefingUI>(uiHost);
            if (creditsUI == null) creditsUI = GetOrAdd<CreditsUI>(uiHost);
            if (audioController == null) audioController = GetOrAdd<GameAudioController>(gameObject);
            if (backdrop == null) backdrop = FindFirstObjectByType<PaperBackdrop>();

            var levelNames = new System.Collections.Generic.List<string>();
            if (levelManager != null)
            {
                foreach (var lvl in levelManager.Levels) levelNames.Add(lvl != null ? lvl.levelName : string.Empty);
            }

            LevelBriefing BriefingForLevel(int levelNumber)
            {
                int i = levelNumber - 1;
                var cfg = levelManager != null && i >= 0 && i < levelManager.Levels.Count ? levelManager.Levels[i] : null;
                if (cfg == null) return new LevelBriefing { Code = MainMenuUI.LevelCode(i) };
                return new LevelBriefing
                {
                    Code = MainMenuUI.LevelCode(i),
                    Title = string.IsNullOrEmpty(cfg.briefingTitle) ? MainMenuUI.ShortName(cfg.levelName).ToUpperInvariant() : cfg.briefingTitle,
                    Goal = cfg.briefingGoal,
                    HowToPlay = cfg.briefingHowToPlay,
                    Catch = cfg.briefingCatch
                };
            }

            string CurrentLevelName()
            {
                return levelManager != null && levelManager.CurrentLevel != null ? levelManager.CurrentLevel.levelName : string.Empty;
            }

            // Audio first so it hears the initial LevelLoaded / FlowStateChanged events.
            audioController.Initialize(gameEvents, audioLibrary, progressStore);
            audioController.BindPlayer(playerController);
            audioController.BindWorld(
                FindFirstObjectByType<Level7TransmutationController>(FindObjectsInactive.Include),
                FindObjectsByType<TransmutableWall>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                FindObjectsByType<TransmutableSpike>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                FindObjectsByType<Level7PatrolEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                FindObjectsByType<Level4TimedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            creditsUI.Initialize(uiDoc);
            mainMenuUI.Initialize(gameEvents, progressStore, levelNames, creditsUI, uiDoc);
            pauseMenuUI.Initialize(gameEvents, progressStore, audioController.ApplyVolumes, CurrentLevelName, uiDoc);
            victoryUI.Initialize(gameEvents, creditsUI, uiDoc);
            briefingUI.Initialize(gameEvents, BriefingForLevel, uiDoc);

            if (backdrop != null)
            {
                backdrop.BindCamera(mainCamera);
                backdrop.BindFocus(playerController != null ? playerController.transform : null);
            }

            // Exit doors hint (open slightly) when the player approaches.
            foreach (var door in FindObjectsByType<GoalDoorAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                door.BindPlayer(playerController != null ? playerController.transform : null);
            }

            // World-space feedback: material form countdown and transmutable highlight.
            var materialController = playerController != null ? playerController.GetComponent<Project.Player.PlayerMaterialController>() : null;
            new GameObject("MaterialFormTimer").AddComponent<MaterialFormTimerVisual>().Bind(materialController);
            var transmutables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m is ITransmutable);
            new GameObject("TransmutableHighlighter").AddComponent<TransmutableHighlighter>().Bind(
                FindFirstObjectByType<Level7TransmutationController>(FindObjectsInactive.Include),
                transmutables,
                playerController != null ? playerController.transform : null);

            if (uiDoc != null) UiFocusGuard.MakeButtonsMouseOnly(uiDoc.rootVisualElement);

            if (flowController == null) flowController = GetOrAdd<GameFlowController>(gameObject);
        }

        private static T GetOrAdd<T>(GameObject host) where T : Component
        {
            var c = host.GetComponent<T>();
            return c != null ? c : host.AddComponent<T>();
        }

        private void OnDestroy()
        {
            if (gameEvents != null)
            {
                gameEvents.LevelResetRequested -= OnLevelResetRequested;
            }
        }

        private void InitializeScene()
        {
            // Flow starts after every system (including the level manager) has applied its initial state,
            // so the title screen's time-scale freeze is not overridden.
            if (flowController != null)
            {
                flowController.Initialize(gameEvents, progressStore, showMainMenuOnStart);
            }

            // Fade in from black on start
            if (screenFader != null)
            {
                screenFader.FadeIn(0.6f);
            }
        }



        private void OnLevelResetRequested()
        {
            // The LevelProgressionManager re-applies the current level; just cover the jump with a quick fade.
            if (screenFader != null)
            {
                screenFader.FadeIn(0.35f);
            }
        }
    }
}
