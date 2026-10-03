using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.Events;
using Game.Gameplay.Player;
using Game.Gameplay.Interaction;
using Game.Presentation.UI;

namespace Game.Gameplay
{
    /// <summary>
    /// Coordinates progression across levels (Level 1 -> Level 2).
    /// Handles camera framing, player repositioning, and dynamic tool inventory per level.
    /// Strictly adheres to Section 3 (Composition Root wiring) and Section 4 (Single Responsibility).
    /// </summary>
    public class LevelProgressionManager : MonoBehaviour
    {
        [System.Serializable]
        public class LevelConfig
        {
            public int levelNumber = 1;
            public string levelName = "Level";
            public Vector2 spawnPosition;
            public Vector3 cameraPosition = new Vector3(0, 0, -10);
            public float cameraOrthoSize = 19.57f;
            public int plankCount = 1;
            public int ladderCount = 1;
            public int platformCount = 1;
            public int chainCount = 0;
            public float toolLifetimeSeconds = 6.5f;
            public GameObject goalShrine;
        }

        [Header("Levels Configuration")]
        [SerializeField] private List<LevelConfig> levels = new List<LevelConfig>();

        [Header("Scene References")]
        [SerializeField] private AutonomousPlayerController player;
        [SerializeField] private Camera worldCamera;
        private GameEvents events;
        private PlacementSystem placementSystem;
        private ToolSelectionBarUI toolUI;
        private ScreenFaderUI screenFader;
        private Transform toolsContainer;

        private int currentLevelIndex = 0; // 0-indexed into levels list
        private bool isTransitioning = false;

        public int CurrentLevelNumber => levels.Count > currentLevelIndex ? levels[currentLevelIndex].levelNumber : 1;
        public LevelConfig CurrentLevel => levels.Count > currentLevelIndex ? levels[currentLevelIndex] : null;
        public IReadOnlyList<LevelConfig> Levels => levels;
        public void ReinitializeLevels() => EnsureDefaultLevels();

        private void Awake()
        {
            EnsureDefaultLevels();
        }

        private void OnValidate()
        {
            EnsureDefaultLevels();
        }

        public void Initialize(
            GameEvents gameEvents,
            AutonomousPlayerController playerController,
            Camera cam,
            PlacementSystem placement = null,
            ToolSelectionBarUI ui = null,
            ScreenFaderUI fader = null,
            Transform container = null)
        {
            events = gameEvents;
            player = playerController;
            worldCamera = cam != null ? cam : Camera.main;
            placementSystem = placement;
            toolUI = ui;
            screenFader = fader;
            toolsContainer = container;

            EnsureDefaultLevels();

            if (events != null)
            {
                events.LevelCompleted -= OnLevelCompleted;
                events.LevelCompleted += OnLevelCompleted;

                events.SimulationStopped -= OnSimulationStopped;
                events.SimulationStopped += OnSimulationStopped;

                events.LevelResetRequested -= OnLevelResetRequested;
                events.LevelResetRequested += OnLevelResetRequested;

                events.SkipLevelRequested -= OnSkipLevelRequested;
                events.SkipLevelRequested += OnSkipLevelRequested;
            }

            ApplyLevelConfig(0, immediate: true);
        }

        private GameObject FindLevel3SpawnObject()
        {
            return GameObject.Find("Level 3 spawn ") 
                ?? GameObject.Find("Level 3 spawn") 
                ?? GameObject.Find("level 3 spawner") 
                ?? GameObject.Find("Level 3 spawner");
        }

        private GameObject FindLevel4SpawnObject()
        {
            return GameObject.Find("Level 4 spawn ") 
                ?? GameObject.Find("Level 4 spawn") 
                ?? GameObject.Find("level 4 spawner") 
                ?? GameObject.Find("Level 4 spawner");
        }

        private void EnsureDefaultLevels()
        {
            if (levels.Count >= 6)
            {
                if (levels.Count > 0)
                {
                    levels[0].toolLifetimeSeconds = 6.5f;
                }
                if (levels.Count > 1)
                {
                    levels[1].toolLifetimeSeconds = 8.0f;
                }

                var lvl2SpawnObj = GameObject.Find("Level 2 spawn");
                if (lvl2SpawnObj != null && levels.Count > 1)
                {
                    levels[1].spawnPosition = lvl2SpawnObj.transform.position;
                }

                var lvl3SpawnObj = FindLevel3SpawnObject();
                if (lvl3SpawnObj != null && levels.Count > 2)
                {
                    levels[2].spawnPosition = lvl3SpawnObj.transform.position;
                }

                var lvl4SpawnObj = FindLevel4SpawnObject();
                if (lvl4SpawnObj != null && levels.Count > 3)
                {
                    levels[3].spawnPosition = lvl4SpawnObj.transform.position;
                }

                var lvl5SpawnObj = GameObject.Find("Level 5 spawn");
                if (lvl5SpawnObj != null && levels.Count > 4)
                {
                    levels[4].spawnPosition = lvl5SpawnObj.transform.position;
                }

                var lvl6SpawnObj = GameObject.Find("Level 6 spawn");
                if (lvl6SpawnObj != null && levels.Count > 5)
                {
                    levels[5].spawnPosition = lvl6SpawnObj.transform.position;
                }

                if (levels.Count > 2)
                {
                    levels[2].plankCount = 0;
                    levels[2].ladderCount = 0;
                    levels[2].platformCount = 0;
                    levels[2].chainCount = 0;
                }

                if (levels.Count > 3)
                {
                    levels[3].plankCount = 0;
                    levels[3].ladderCount = 0;
                    levels[3].platformCount = 0;
                    levels[3].chainCount = 0;
                }

                if (levels.Count > 4)
                {
                    levels[4].plankCount = 0;
                    levels[4].ladderCount = 0;
                    levels[4].platformCount = 0;
                    levels[4].chainCount = 0;
                    if (levels[4].goalShrine == null)
                    {
                        levels[4].goalShrine = GameObject.Find("Goal_Shrine (3)");
                    }
                }

                if (levels.Count > 5)
                {
                    levels[5].plankCount = 0;
                    levels[5].ladderCount = 0;
                    levels[5].platformCount = 0;
                    levels[5].chainCount = 0;
                    if (levels[5].goalShrine == null)
                    {
                        levels[5].goalShrine = GameObject.Find("Goal_Shrine (4)");
                    }
                }
                return;
            }

            if (levels.Count == 4)
            {
                Vector2 lvl5SpawnPoint = new Vector2(297.21f, -0.70f);
                var lvl5Obj = GameObject.Find("Level 5 spawn");
                if (lvl5Obj != null) lvl5SpawnPoint = lvl5Obj.transform.position;

                levels.Add(new LevelConfig
                {
                    levelNumber = 5,
                    levelName = "Level 5 - Material Alchemy",
                    spawnPosition = lvl5SpawnPoint,
                    cameraPosition = new Vector3(343.5f, 7.5f, -10f),
                    cameraOrthoSize = 31.0f,
                    plankCount = 0,
                    ladderCount = 0,
                    platformCount = 0,
                    chainCount = 0,
                    toolLifetimeSeconds = 8.0f,
                    goalShrine = GameObject.Find("Goal_Shrine (3)")
                });
            }

            if (levels.Count == 5)
            {
                Vector2 lvl6SpawnPoint = new Vector2(400.60f, 0.30f);
                var lvl6Obj = GameObject.Find("Level 6 spawn");
                if (lvl6Obj != null) lvl6SpawnPoint = lvl6Obj.transform.position;

                levels.Add(new LevelConfig
                {
                    levelNumber = 6,
                    levelName = "Level 6 - Aerial Material Traverse",
                    spawnPosition = lvl6SpawnPoint,
                    cameraPosition = new Vector3(466.0f, 9.6f, -10f),
                    cameraOrthoSize = 46.0f,
                    plankCount = 0,
                    ladderCount = 0,
                    platformCount = 0,
                    chainCount = 0,
                    toolLifetimeSeconds = 8.0f,
                    goalShrine = GameObject.Find("Goal_Shrine (4)")
                });
                return;
            }

            levels.Clear();

            // Level 1: Left canyon to Shrine
            levels.Add(new LevelConfig
            {
                levelNumber = 1,
                levelName = "Level 1 - The Ascent",
                spawnPosition = new Vector2(-49.0f, -1.8f),
                cameraPosition = new Vector3(-19.3f, 6.7f, -10f),
                cameraOrthoSize = 19.57f,
                plankCount = 1,
                ladderCount = 1,
                platformCount = 1,
                chainCount = 0,
                toolLifetimeSeconds = 6.5f, // Level 1: placed tools disappear in 6.5 seconds
                goalShrine = GameObject.Find("Goal_Shrine")
            });

            // Level 2: Plateau over Spikes to Shrine 2
            Vector2 lvl2Spawn = new Vector2(18.20f, -1.40f);
            var lvl2GO = GameObject.Find("Level 2 spawn");
            if (lvl2GO != null)
            {
                lvl2Spawn = lvl2GO.transform.position;
            }

            levels.Add(new LevelConfig
            {
                levelNumber = 2,
                levelName = "Level 2 - Suspension Chasm",
                spawnPosition = lvl2Spawn,
                cameraPosition = new Vector3(49.4f, 3.5f, -10f),
                cameraOrthoSize = 22.0f,
                plankCount = 2,
                ladderCount = 2,
                platformCount = 2,
                chainCount = 1,
                toolLifetimeSeconds = 8.0f,
                goalShrine = GameObject.Find("Goal_Shrine (1)")
            });

            // Level 3: Gravity Inversion Chasm (Manual Input & Dual Gravity Modes)
            Vector2 lvl3Spawn = new Vector2(91.40f, -2.38f);
            var lvl3GO = FindLevel3SpawnObject();
            if (lvl3GO != null)
            {
                lvl3Spawn = lvl3GO.transform.position;
            }

            levels.Add(new LevelConfig
            {
                levelNumber = 3,
                levelName = "Level 3 - Gravity Inversion Chasm",
                spawnPosition = lvl3Spawn,
                cameraPosition = new Vector3(136.5f, 10.0f, -10f),
                cameraOrthoSize = 31.0f,
                plankCount = 0,
                ladderCount = 0,
                platformCount = 0,
                chainCount = 0,
                toolLifetimeSeconds = 8.0f,
                goalShrine = GameObject.Find("Goal_Shrine (2)")
            });

            // Level 4: Random Gravity Puzzle Chamber (Switch -> Spike -> Platform -> Timed Door)
            Vector2 lvl4Spawn = new Vector2(189.80f, -1.60f);
            var lvl4GO = FindLevel4SpawnObject();
            if (lvl4GO != null)
            {
                lvl4Spawn = lvl4GO.transform.position;
            }

            levels.Add(new LevelConfig
            {
                levelNumber = 4,
                levelName = "Level 4 - Chamber of Inversion",
                spawnPosition = lvl4Spawn,
                cameraPosition = new Vector3(240.0f, 10.0f, -10f),
                cameraOrthoSize = 31.0f,
                plankCount = 0,
                ladderCount = 0,
                platformCount = 0,
                chainCount = 0,
                toolLifetimeSeconds = 8.0f,
                goalShrine = null // Handled by Level 4 Timed Door!
            });

            // Level 5: Material Alchemy (Paper, Stone, Rubber)
            Vector2 lvl5Spawn = new Vector2(297.21f, -0.70f);
            var lvl5GO = GameObject.Find("Level 5 spawn");
            if (lvl5GO != null)
            {
                lvl5Spawn = lvl5GO.transform.position;
            }

            levels.Add(new LevelConfig
            {
                levelNumber = 5,
                levelName = "Level 5 - Material Alchemy",
                spawnPosition = lvl5Spawn,
                cameraPosition = new Vector3(343.5f, 7.5f, -10f),
                cameraOrthoSize = 31.0f,
                plankCount = 0,
                ladderCount = 0,
                platformCount = 0,
                chainCount = 0,
                toolLifetimeSeconds = 8.0f,
                goalShrine = GameObject.Find("Goal_Shrine (3)")
            });

            // Level 6: Aerial Material Traverse (Paper, Stone, Rubber)
            Vector2 lvl6Spawn = new Vector2(400.60f, 0.30f);
            var lvl6GO = GameObject.Find("Level 6 spawn");
            if (lvl6GO != null)
            {
                lvl6Spawn = lvl6GO.transform.position;
            }

            levels.Add(new LevelConfig
            {
                levelNumber = 6,
                levelName = "Level 6 - Aerial Material Traverse",
                spawnPosition = lvl6Spawn,
                cameraPosition = new Vector3(466.0f, 9.6f, -10f),
                cameraOrthoSize = 46.0f,
                plankCount = 0,
                ladderCount = 0,
                platformCount = 0,
                chainCount = 0,
                toolLifetimeSeconds = 8.0f,
                goalShrine = GameObject.Find("Goal_Shrine (4)")
            });
        }

        private void OnLevelCompleted()
        {
            if (isTransitioning) return;

            if (currentLevelIndex < levels.Count - 1)
            {
                StartCoroutine(TransitionToNextLevelRoutine(currentLevelIndex + 1));
            }
            else
            {
                Debug.Log("[LevelProgressionManager] All levels completed! Final victory!");
                // Final level victory handling: restart or loop
                StartCoroutine(FinalVictoryRoutine());
            }
        }

        private IEnumerator TransitionToNextLevelRoutine(int targetIndex, float initialDelay = 0.6f)
        {
            isTransitioning = true;
            Debug.Log($"[LevelProgressionManager] Level {CurrentLevelNumber} -> Preparing Level {targetIndex + 1} transition...");

            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            if (screenFader != null)
            {
                bool fadeDone = false;
                screenFader.FadeOut(0.4f, () => fadeDone = true);
                while (!fadeDone) yield return null;
            }

            ApplyLevelConfig(targetIndex, immediate: false);

            yield return new WaitForSeconds(0.15f);

            if (screenFader != null)
            {
                screenFader.FadeIn(0.4f);
            }

            isTransitioning = false;
            Debug.Log($"[LevelProgressionManager] Successfully entered Level {CurrentLevelNumber}!");
        }

        public void SkipToNextLevel(bool immediate = false)
        {
            if (!immediate && isTransitioning) return;
            if (levels.Count == 0) return;

            int nextIndex = (currentLevelIndex + 1) % levels.Count;
            Debug.Log($"[LevelProgressionManager] Skip requested! Advancing from Level {CurrentLevelNumber} to Level {nextIndex + 1}...");

            if (immediate || !Application.isPlaying)
            {
                ApplyLevelConfig(nextIndex, immediate: true);
                isTransitioning = false;
            }
            else
            {
                StartCoroutine(TransitionToNextLevelRoutine(nextIndex, initialDelay: 0f));
            }
        }

        private void OnSkipLevelRequested()
        {
            SkipToNextLevel();
        }

        private IEnumerator FinalVictoryRoutine()
        {
            if (screenFader != null)
            {
                bool fadeDone = false;
                screenFader.FadeOut(1.0f, () => fadeDone = true);
                while (!fadeDone) yield return null;
            }

            Debug.Log("[LevelProgressionManager] All levels completed! Final Level Complete on black screen.");
        }

        public void ApplyLevelConfig(int index, bool immediate)
        {
            if (index < 0 || index >= levels.Count) return;
            currentLevelIndex = index;
            LevelConfig config = levels[index];

            if (index == 0)
            {
                config.toolLifetimeSeconds = 6.5f;
            }
            else if (index == 1)
            {
                config.toolLifetimeSeconds = 8.0f;
                var lvl2GO = GameObject.Find("Level 2 spawn");
                if (lvl2GO != null)
                {
                    config.spawnPosition = lvl2GO.transform.position;
                }
            }
            else if (index == 2)
            {
                var lvl3GO = FindLevel3SpawnObject();
                if (lvl3GO != null)
                {
                    config.spawnPosition = lvl3GO.transform.position;
                }
            }
            else if (index == 3)
            {
                var lvl4GO = FindLevel4SpawnObject();
                if (lvl4GO != null)
                {
                    config.spawnPosition = lvl4GO.transform.position;
                }
            }
            else if (index == 4)
            {
                var lvl5GO = GameObject.Find("Level 5 spawn");
                if (lvl5GO != null)
                {
                    config.spawnPosition = lvl5GO.transform.position;
                }
            }
            else if (index == 5)
            {
                var lvl6GO = GameObject.Find("Level 6 spawn");
                if (lvl6GO != null)
                {
                    config.spawnPosition = lvl6GO.transform.position;
                }
            }

            // 1. Stop simulation & clear placed tools
            events?.PublishSimulationStopped();
            ClearPlacedTools();

            // 2. Reposition camera
            if ((worldCamera as UnityEngine.Object) == null) worldCamera = Camera.main;
            if ((worldCamera as UnityEngine.Object) != null)
            {
                worldCamera.transform.position = config.cameraPosition;
                worldCamera.orthographicSize = config.cameraOrthoSize;
            }

            // 3. Move and configure player
            if ((player as UnityEngine.Object) == null) player = FindFirstObjectByType<AutonomousPlayerController>();
            if ((player as UnityEngine.Object) != null)
            {
                player.SetSpawnPosition(config.spawnPosition);

                var gravCtrl = player.GetComponent<PlayerGravityController>();
                var matCtrl = player.GetComponent<Project.Player.PlayerMaterialController>();

                if (index == 2 || index == 3)
                {
                    player.SetLocomotionMode(LocomotionMode.Manual);
                    if (gravCtrl == null)
                    {
                        gravCtrl = player.gameObject.AddComponent<PlayerGravityController>();
                    }
                    gravCtrl.Initialize(player.GetComponent<Rigidbody2D>(), player.GetComponentInChildren<SpriteRenderer>(), events);
                    gravCtrl.enabled = true;
                    // Level 3: ONLY Earth and Moon, rapid 3-second cycle!
                    // Level 4: Random cycle with all modes including InvertedRoof!
                    gravCtrl.ConfigureCycle(
                        includeInverted: index == 3,
                        intervalSeconds: 3.0f,
                        random: index == 3
                    );
                    gravCtrl.ResetCycle();
                    player.SetGravityController(gravCtrl);

                    if (matCtrl != null)
                    {
                        matCtrl.enabled = false;
                        player.ResetVisuals();
                    }
                }
                else if (index == 4 || index == 5) // Level 5 & Level 6: Material Alchemy (Paper, Stone, Rubber)
                {
                    player.SetLocomotionMode(LocomotionMode.Material);
                    if (gravCtrl != null)
                    {
                        gravCtrl.enabled = false;
                    }
                    player.transform.localEulerAngles = Vector3.zero;
                    player.SetGravityController(null);

                    if (matCtrl == null)
                    {
                        matCtrl = player.gameObject.AddComponent<Project.Player.PlayerMaterialController>();
                    }
                    matCtrl.enabled = true;
                    matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);
                }
                else
                {
                    player.SetLocomotionMode(LocomotionMode.Autonomous);
                    if (gravCtrl != null)
                    {
                        gravCtrl.enabled = false;
                    }
                    if (matCtrl != null)
                    {
                        matCtrl.enabled = false;
                        player.ResetVisuals();
                    }
                    var rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.gravityScale = 1.0f;
                        rb.mass = 1.0f;
                    }
                    player.transform.localEulerAngles = Vector3.zero;
                    player.SetGravityController(null);
                }
            }

            // 4. Update tool limits in PlacementSystem & ToolSelectionBarUI
            if ((placementSystem as UnityEngine.Object) == null) placementSystem = FindFirstObjectByType<PlacementSystem>();
            if (placementSystem != null)
            {
                placementSystem.SetToolLimit(ToolType.Plank, config.plankCount);
                placementSystem.SetToolLimit(ToolType.Ladder, config.ladderCount);
                placementSystem.SetToolLimit(ToolType.Platform, config.platformCount);
                placementSystem.SetToolLimit(ToolType.Chain, config.chainCount);
                placementSystem.SetToolLifetime(config.toolLifetimeSeconds);
            }

            if (toolUI == null) toolUI = FindFirstObjectByType<ToolSelectionBarUI>(FindObjectsInactive.Include);
            if (toolUI != null)
            {
                toolUI.SetToolLimits(config.plankCount, config.ladderCount, config.platformCount, config.chainCount);
                // Levels 3 & 4 have no tools or building dock - hide toolbar
                toolUI.SetToolbarVisible(index < 2);
            }

            // 5. Reset goals & Level 4 puzzle components
            ResetLevelGoals();

            // Level 4 puzzle container must strictly only be active in Level 4 (index 3)
            var puzzleRoot = GameObject.Find("Level_04_Puzzle");
            if (puzzleRoot == null)
            {
                var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = 0; i < allGos.Length; i++)
                {
                    if (allGos[i].name == "Level_04_Puzzle" && allGos[i].scene.isLoaded)
                    {
                        puzzleRoot = allGos[i];
                        break;
                    }
                }
            }
            if (puzzleRoot != null)
            {
                puzzleRoot.SetActive(index == 3);
            }

            var movableSpikeObj = GameObject.Find("Movable spike");
            if (movableSpikeObj == null)
            {
                var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = 0; i < allGos.Length; i++)
                {
                    if (allGos[i].name == "Movable spike" && allGos[i].scene.isLoaded)
                    {
                        movableSpikeObj = allGos[i];
                        break;
                    }
                }
            }
            if (movableSpikeObj != null)
            {
                movableSpikeObj.SetActive(index == 3);
            }

            if (index == 3)
            {
                var puzzleCoordinator = FindFirstObjectByType<Game.Gameplay.Environment.Level4PuzzleCoordinator>();
                if (puzzleCoordinator != null)
                {
                    if (events != null) puzzleCoordinator.Initialize(events);
                    puzzleCoordinator.ResetAll();
                }
            }

            // 6. Notify systems of new level
            events?.PublishLevelLoaded(config.levelNumber);

            // In Levels 3, 4, 5, & 6: auto-start simulation so the player can immediately use controls!
            if ((index == 2 || index == 3 || index == 4 || index == 5) && Application.isPlaying)
            {
                StartCoroutine(AutoStartSimulationRoutine());
            }
        }

        private void ClearPlacedTools()
        {
            if (toolsContainer == null)
            {
                var go = GameObject.Find("Placed_Tools");
                if (go != null) toolsContainer = go.transform;
            }

            if (toolsContainer != null)
            {
                var tools = toolsContainer.GetComponentsInChildren<DraggableTool>(true);
                for (int i = 0; i < tools.Length; i++)
                {
                    if (tools[i] != null)
                    {
                        if (Application.isPlaying) Destroy(tools[i].gameObject);
                        else DestroyImmediate(tools[i].gameObject);
                    }
                }
            }
        }

        private void ResetLevelGoals()
        {
            var goals = FindObjectsByType<Combat.LevelGoal>(FindObjectsSortMode.None);
            foreach (var g in goals)
            {
                if (g != null) g.ResetGoal();
            }
        }

        private void OnSimulationStopped()
        {
            // Reset player to current level's spawn position
            if (player != null && CurrentLevel != null)
            {
                Vector2 spawnPos = CurrentLevel.spawnPosition;
                if (currentLevelIndex == 1)
                {
                    var lvl2GO = GameObject.Find("Level 2 spawn");
                    if (lvl2GO != null) spawnPos = lvl2GO.transform.position;
                }
                else if (currentLevelIndex == 2)
                {
                    var lvl3GO = FindLevel3SpawnObject();
                    if (lvl3GO != null) spawnPos = lvl3GO.transform.position;
                }
                else if (currentLevelIndex == 3)
                {
                    var lvl4GO = FindLevel4SpawnObject();
                    if (lvl4GO != null) spawnPos = lvl4GO.transform.position;
                }
                else if (currentLevelIndex == 4)
                {
                    var lvl5GO = GameObject.Find("Level 5 spawn");
                    if (lvl5GO != null) spawnPos = lvl5GO.transform.position;
                }
                else if (currentLevelIndex == 5)
                {
                    var lvl6GO = GameObject.Find("Level 6 spawn");
                    if (lvl6GO != null) spawnPos = lvl6GO.transform.position;
                }
                player.ResetState(spawnPos);

                var gravCtrl = player.GetComponent<PlayerGravityController>();
                var matCtrl = player.GetComponent<Project.Player.PlayerMaterialController>();

                if (currentLevelIndex == 2 || currentLevelIndex == 3)
                {
                    if (gravCtrl != null)
                    {
                        gravCtrl.enabled = true;
                        gravCtrl.SetRandomCycle(currentLevelIndex == 3);
                        gravCtrl.ResetCycle();
                    }
                    if (matCtrl != null)
                    {
                        matCtrl.enabled = false;
                        player.ResetVisuals();
                    }
                    if (Application.isPlaying)
                    {
                        StartCoroutine(AutoStartSimulationRoutine());
                    }
                }
                else if (currentLevelIndex == 4 || currentLevelIndex == 5)
                {
                    player.SetLocomotionMode(LocomotionMode.Material);
                    if (gravCtrl != null) gravCtrl.enabled = false;
                    player.transform.localEulerAngles = Vector3.zero;
                    if (matCtrl == null) matCtrl = player.gameObject.AddComponent<Project.Player.PlayerMaterialController>();
                    matCtrl.enabled = true;
                    matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);
                    if (Application.isPlaying)
                    {
                        StartCoroutine(AutoStartSimulationRoutine());
                    }
                }
                else
                {
                    if (gravCtrl != null)
                    {
                        gravCtrl.enabled = false;
                    }
                    if (matCtrl != null)
                    {
                        matCtrl.enabled = false;
                        player.ResetVisuals();
                    }
                    var rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.gravityScale = 1.0f;
                        rb.mass = 1.0f;
                    }
                    player.transform.localEulerAngles = Vector3.zero;
                }

                var puzzleCoordinator = FindFirstObjectByType<Game.Gameplay.Environment.Level4PuzzleCoordinator>();
                if (puzzleCoordinator != null)
                {
                    puzzleCoordinator.ResetAll();
                }
            }
        }

        private IEnumerator AutoStartSimulationRoutine()
        {
            yield return null;
            if ((currentLevelIndex == 2 || currentLevelIndex == 3 || currentLevelIndex == 4 || currentLevelIndex == 5) && player != null && !player.IsDead)
            {
                events?.PublishSimulationStarted();
            }
        }

        private void Update()
        {
            // Allow testing shortcut 'N' to skip level at any time
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.nKey.wasPressedThisFrame)
            {
                SkipToNextLevel();
            }
        }

        private void OnLevelResetRequested()
        {
            ApplyLevelConfig(currentLevelIndex, immediate: true);
        }

        public void Dispose()
        {
            if (events != null)
            {
                events.LevelCompleted -= OnLevelCompleted;
                events.SimulationStopped -= OnSimulationStopped;
                events.LevelResetRequested -= OnLevelResetRequested;
                events.SkipLevelRequested -= OnSkipLevelRequested;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
