using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.Events;
using Game.Core.Input;
using Game.Core.Interfaces;
using Game.Data.Items;
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
        [SerializeField] private RulebookUI rulebookUI;
        [SerializeField] private Game.Presentation.Audio.GameAudioPresenter audioPresenter;

        [Header("Configuration")]
        [SerializeField] private ToolDefinition[] toolDefinitions;

        // Core Infrastructure Instances
        private GameEvents gameEvents;
        private IPlacementInput placementInput;

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
                playerHUD.BindPlayer(playerController);
            }

            if (rulebookUI == null)
            {
                rulebookUI = FindFirstObjectByType<RulebookUI>(FindObjectsInactive.Include);
                if (rulebookUI == null)
                {
                    var uiDoc = FindFirstObjectByType<UnityEngine.UIElements.UIDocument>();
                    if (uiDoc != null)
                    {
                        rulebookUI = uiDoc.gameObject.AddComponent<RulebookUI>();
                    }
                }
            }
            if (rulebookUI != null)
            {
                rulebookUI.Initialize(gameEvents);
            }

            if (audioPresenter == null)
            {
                audioPresenter = FindFirstObjectByType<Game.Presentation.Audio.GameAudioPresenter>();
                if (audioPresenter == null)
                {
                    var coreObj = GameObject.Find("Core====");
                    var parentTrans = coreObj != null ? coreObj.transform : transform;
                    var audioGO = new GameObject("AudioPresenter");
                    audioGO.transform.SetParent(parentTrans, false);
                    audioPresenter = audioGO.AddComponent<Game.Presentation.Audio.GameAudioPresenter>();
                }
            }
            if (audioPresenter != null)
            {
                audioPresenter.Initialize(gameEvents);
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

            // 4. Hook Lifecycle Transitions
            gameEvents.LevelResetRequested += OnLevelResetRequested;
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
            // Fade in from black on start
            if (screenFader != null)
            {
                screenFader.FadeIn(0.6f);
            }
        }



        private void OnLevelResetRequested()
        {
            Debug.Log("[GameBootstrap] Level reset requested.");
            if (screenFader != null)
            {
                screenFader.FadeOut(0.3f, () =>
                {
                    ReloadSceneImmediate();
                });
            }
            else
            {
                ReloadSceneImmediate();
            }
        }

        private IEnumerator ReloadSceneAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReloadSceneImmediate();
        }

        private void ReloadSceneImmediate()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
