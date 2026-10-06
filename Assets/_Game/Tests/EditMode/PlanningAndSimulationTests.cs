#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.Core.Events;
using Game.Gameplay;
using Game.Gameplay.Player;
using Game.Gameplay.Interaction;
using Game.Gameplay.Transmutation;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class PlanningAndSimulationTests
    {
        private GameObject playerGO;
        private AutonomousPlayerController player;
        private GameEvents events;
        private readonly System.Collections.Generic.List<GameObject> spawnedObjects = new System.Collections.Generic.List<GameObject>();

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            spawnedObjects.Add(go);
            return go;
        }

        [SetUp]
        public void SetUp()
        {
            events = new GameEvents();
            playerGO = CreateTestGameObject("TestPlayer");
            playerGO.AddComponent<Rigidbody2D>();
            playerGO.AddComponent<BoxCollider2D>();
            player = playerGO.AddComponent<AutonomousPlayerController>();
            playerGO.transform.position = new Vector3(-7.2f, -1.8f, 0f);
            player.Initialize(events);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null) Object.DestroyImmediate(spawnedObjects[i]);
            }
            spawnedObjects.Clear();
        }

        [Test]
        public void Player_StartsInPlanningMode_NotSimulating()
        {
            Assert.IsFalse(player.IsSimulating, "Player should start in planning phase (not simulating).");
            Assert.IsFalse(player.IsDead, "Player should be alive.");
            Assert.AreEqual(0f, player.StuckTimer, "Stuck timer should be zero in planning phase.");
            Assert.AreEqual(new Vector3(-7.2f, -1.8f, 0f), player.transform.position, "Player should remain at spawn.");
        }

        [Test]
        public void SimulationStarted_EnablesPlayerLocomotion()
        {
            events.PublishSimulationStarted();
            Assert.IsTrue(player.IsSimulating, "Publishing SimulationStarted should activate locomotion.");
        }

        [Test]
        public void SimulationStopped_ResetsPlayerToInitialSpawn()
        {
            events.PublishSimulationStarted();
            player.transform.position = new Vector3(2.5f, 0.8f, 0f);

            events.PublishSimulationStopped();

            Assert.IsFalse(player.IsSimulating, "Simulation should be inactive after stop.");
            Assert.AreEqual(-7.2f, player.transform.position.x, 0.01f, "Player X should reset to spawn.");
            Assert.AreEqual(-1.8f, player.transform.position.y, 0.01f, "Player Y should reset to spawn.");
        }

        [Test]
        public void DraggableTool_ResetToPlacedTransform_RestoresConfirmedPosition()
        {
            var toolGO = CreateTestGameObject("TestTool");
            toolGO.AddComponent<Rigidbody2D>();
            toolGO.AddComponent<BoxCollider2D>();
            var tool = toolGO.AddComponent<DraggableTool>();

            toolGO.transform.position = new Vector3(-3.5f, -2.0f, 0f);
            toolGO.transform.rotation = Quaternion.Euler(0, 0, 24.5f);
            tool.SetPreviewMode(false); // Confirm placement

            // Simulate physics perturbation during simulation
            toolGO.transform.position = new Vector3(0f, -5f, 0f);
            toolGO.transform.rotation = Quaternion.Euler(0, 0, 90f);

            tool.ResetToPlacedTransform();

            Assert.AreEqual(-3.5f, toolGO.transform.position.x, 0.01f, "Tool position X should restore to placed position.");
            Assert.AreEqual(-2.0f, toolGO.transform.position.y, 0.01f, "Tool position Y should restore to placed position.");
            Assert.AreEqual(24.5f, toolGO.transform.eulerAngles.z, 0.1f, "Tool angle should restore to placed angle.");

            Object.DestroyImmediate(toolGO);
        }

        [Test]
        public void Spike_ContactWithPlayer_EliminatesPlayerAndPublishesPlayerDied()
        {
            string receivedCause = null;
            events.PlayerDied += (cause) => { receivedCause = cause; };

            var spikeGO = CreateTestGameObject("TestSpike");
            var spikeCol = spikeGO.AddComponent<BoxCollider2D>();
            spikeCol.isTrigger = true;
            var spike = spikeGO.AddComponent<Game.Gameplay.Combat.Spike>();

            var method = typeof(Game.Gameplay.Combat.Hazard2D).GetMethod("OnTriggerEnter2D", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(spike, new object[] { playerGO.GetComponent<Collider2D>() });

            Assert.IsTrue(player.IsDead, "Player should be dead after contacting Spike.");
            Assert.IsFalse(player.IsSimulating, "Simulation should stop on player death.");
            Assert.IsNotNull(receivedCause, "PlayerDied event should be fired.");
            StringAssert.Contains("Spike", receivedCause, "Death cause should mention Spike.");

            Object.DestroyImmediate(spikeGO);
        }

        [Test]
        public void DraggableTool_StartsLifetime_WhenPlaced()
        {
            var toolGO = CreateTestGameObject("TestTool");
            toolGO.AddComponent<Rigidbody2D>();
            var tool = toolGO.AddComponent<DraggableTool>();

            Assert.IsFalse(tool.IsPlaced);
            tool.SetPreviewMode(false); // Confirm placement

            Assert.IsTrue(tool.IsPlaced);
            Assert.AreEqual(6.5f, tool.LifetimeSeconds, 0.01f);
            Assert.AreEqual(6.5f, tool.LifetimeRemaining, 0.01f);

            Object.DestroyImmediate(toolGO);
        }

        [Test]
        public void DraggableTool_Disappears_WhenDisappearCalled()
        {
            var toolGO = CreateTestGameObject("TestTool");
            toolGO.AddComponent<Rigidbody2D>();
            var tool = toolGO.AddComponent<DraggableTool>();

            tool.SetPreviewMode(false);
            tool.Disappear();

            Assert.IsFalse(tool.IsPlaced);
            Object.DestroyImmediate(toolGO);
        }

        [Test]
        public void ChainTool_BuildsWalkableEdgeCollider_BetweenTwoPoints()
        {
            var chainGO = CreateTestGameObject("TestChain");
            chainGO.AddComponent<Rigidbody2D>();
            var edge = chainGO.AddComponent<EdgeCollider2D>();
            var drag = chainGO.AddComponent<DraggableTool>();
            var chain = chainGO.AddComponent<ChainTool>();

            chain.Initialize(new Vector2(10f, 2f), new Vector2(15f, 2f), 6.5f);

            Assert.AreEqual(new Vector3(12.5f, 2f, 0f), chainGO.transform.position, "Chain root should be at midpoint.");
            Assert.IsTrue(edge.points.Length >= 4, "EdgeCollider should have curve points.");
            Assert.AreEqual(ToolType.Chain, drag.Type, "Tool type should be Chain.");
            Assert.IsTrue(drag.IsPlaced, "Chain should be placed.");
            Assert.AreEqual(6.5f, drag.LifetimeSeconds, 0.01f);

            Object.DestroyImmediate(chainGO);
        }

        [Test]
        public void AutonomousPlayerController_SetSpawnPosition_UpdatesSpawnAndResets()
        {
            Vector2 lvl2Spawn = new Vector2(13.31f, -3.80f);
            player.SetSpawnPosition(lvl2Spawn);

            Assert.AreEqual(lvl2Spawn.x, player.transform.position.x, 0.01f);
            Assert.AreEqual(lvl2Spawn.y, player.transform.position.y, 0.01f);

            // Start simulation and move player
            events.PublishSimulationStarted();
            player.transform.position = new Vector3(30f, 5f, 0f);

            // Stop simulation -> should reset to Level 2 spawn!
            events.PublishSimulationStopped();
            Assert.AreEqual(lvl2Spawn.x, player.transform.position.x, 0.01f);
            Assert.AreEqual(lvl2Spawn.y, player.transform.position.y, 0.01f);
        }

        [Test]
        public void LevelProgressionManager_TransitionToLevel2_ConfiguresPlayerAndTools()
        {
            var managerGO = CreateTestGameObject("TestLPM");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();

            manager.Initialize(events, player, cam, null, null, null, null);

            Assert.AreEqual(1, manager.CurrentLevelNumber, "Should start at Level 1.");
            Assert.AreEqual(0, manager.CurrentLevel.chainCount, "Level 1 should have 0 chains.");

            manager.ApplyLevelConfig(1, immediate: true);

            Assert.AreEqual(2, manager.CurrentLevelNumber, "Should switch to Level 2.");
            Assert.AreEqual(2, manager.CurrentLevel.plankCount, "Level 2 should have 2 planks.");
            Assert.AreEqual(2, manager.CurrentLevel.ladderCount, "Level 2 should have 2 ladders.");
            Assert.AreEqual(2, manager.CurrentLevel.platformCount, "Level 2 should have 2 platforms.");
            Assert.AreEqual(1, manager.CurrentLevel.chainCount, "Level 2 should have 1 chain.");
            Assert.AreEqual(manager.CurrentLevel.spawnPosition.x, player.transform.position.x, 0.05f, "Player should be moved to Level 2 spawn.");

            manager.Dispose();
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(camGO);
        }

        [Test]
        public void LevelProgressionManager_LevelConfigs_HaveCorrectLifetimeFlags()
        {
            var managerGO = CreateTestGameObject("TestLPM");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();

            manager.Initialize(events, player, cam, null, null, null, null);

            // Level 1: lifetime 6.5s
            manager.ApplyLevelConfig(0, immediate: true);
            Assert.AreEqual(1, manager.CurrentLevelNumber);
            Assert.AreEqual(6.5f, manager.CurrentLevel.toolLifetimeSeconds, 0.01f, "Level 1 tool lifetime should be 6.5s.");

            // Level 2: lifetime 8.0s
            manager.ApplyLevelConfig(1, immediate: true);
            Assert.AreEqual(2, manager.CurrentLevelNumber);
            Assert.AreEqual(8.0f, manager.CurrentLevel.toolLifetimeSeconds, 0.01f, "Level 2 tool lifetime should be 8.0s.");

            manager.Dispose();
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(camGO);
        }

        [Test]
        public void LevelProgressionManager_SkipToNextLevel_CyclesLevels()
        {
            var managerGO = CreateTestGameObject("TestLPM");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                manager.Initialize(events, player, cam, null, null, null, null);
                Assert.AreEqual(1, manager.CurrentLevelNumber);

                // Skip to Level 2
                manager.SkipToNextLevel();
                Assert.AreEqual(2, manager.CurrentLevelNumber);

                // Skip to Level 3
                manager.SkipToNextLevel();
                Assert.AreEqual(3, manager.CurrentLevelNumber);

                // Skip to Level 4
                manager.SkipToNextLevel();
                Assert.AreEqual(4, manager.CurrentLevelNumber);

                // Skip to Level 5
                manager.SkipToNextLevel();
                Assert.AreEqual(5, manager.CurrentLevelNumber);

                // Skip to Level 6
                manager.SkipToNextLevel();
                Assert.AreEqual(6, manager.CurrentLevelNumber);

                // Skip to Level 7
                manager.SkipToNextLevel();
                Assert.AreEqual(7, manager.CurrentLevelNumber);

                // Skip to Level 8
                manager.SkipToNextLevel();
                Assert.AreEqual(8, manager.CurrentLevelNumber);

                // Skip again cycles back to Level 1
                manager.SkipToNextLevel();
                Assert.AreEqual(1, manager.CurrentLevelNumber);
            }
            finally
            {
                manager.Dispose();
                Object.DestroyImmediate(managerGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void PlacementSystem_ToolLifetime_UpdatesWithLevelConfiguration()
        {
            var psGO = CreateTestGameObject("TestPS");
            var ps = psGO.AddComponent<PlacementSystem>();

            Assert.AreEqual(0.0f, ps.ToolLifetimeSeconds, 0.01f, "Default tool lifetime should be 0.0s (permanent).");

            ps.SetToolLifetime(8.0f);
            Assert.AreEqual(8.0f, ps.ToolLifetimeSeconds, 0.01f, "Level 2 tool lifetime should be 8.0s.");

            var toolGO = CreateTestGameObject("TestTool");
            toolGO.AddComponent<Rigidbody2D>();
            var tool = toolGO.AddComponent<DraggableTool>();
            tool.SetLifetime(ps.ToolLifetimeSeconds);

            tool.SetPreviewMode(false);
            Assert.AreEqual(8.0f, tool.LifetimeSeconds, 0.01f, "Placed tool should have 8.0s lifetime.");
            Assert.AreEqual(8.0f, tool.LifetimeRemaining, 0.01f, "Placed tool remaining lifetime should be 8.0s.");

            Object.DestroyImmediate(toolGO);
            Object.DestroyImmediate(psGO);
        }

        [Test]
        public void PlacementSystem_BlocksToolPlacement_DuringSimulation()
        {
            var psGO = CreateTestGameObject("TestPS");
            var ps = psGO.AddComponent<PlacementSystem>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();
            var containerGO = CreateTestGameObject("TestContainer");
            var plankDef = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Data.Items.ToolDefinition>("Assets/_Game/Data/Items/PlankToolData.asset");

            ps.Initialize(null, events, cam, containerGO.transform, new[] { plankDef });

            // Start simulation
            events.PublishSimulationStarted();
            Assert.IsTrue(ps.IsSimulating, "PlacementSystem should be in simulation mode.");

            // Attempting to select/spawn a tool during simulation must be blocked
            ps.SelectOrSpawnTool(ToolType.Plank);
            Assert.AreEqual(0, containerGO.transform.childCount, "No tool should be spawned during simulation.");

            // Stop simulation
            events.PublishSimulationStopped();
            Assert.IsFalse(ps.IsSimulating, "PlacementSystem should return to planning mode.");

            // In planning mode, tool can be spawned
            ps.SelectOrSpawnTool(ToolType.Plank);
            Assert.AreEqual(1, containerGO.transform.childCount, "Tool should spawn in planning mode.");

            Object.DestroyImmediate(psGO);
            Object.DestroyImmediate(camGO);
            Object.DestroyImmediate(containerGO);
        }

        [Test]
        public void PlayerGravityController_TogglesModesAndOrientation()
        {
            var gc = playerGO.AddComponent<PlayerGravityController>();
            gc.Initialize(playerGO.GetComponent<Rigidbody2D>(), playerGO.GetComponentInChildren<SpriteRenderer>(), events);

            // Initially Earth mode
            Assert.AreEqual(GravityMode.Earth, gc.CurrentMode);
            Assert.AreEqual(1.0f, gc.EarthGravityScale, 0.01f);
            Assert.AreEqual(1.0f, playerGO.GetComponent<Rigidbody2D>().gravityScale, 0.01f);
            Assert.AreEqual(0f, playerGO.transform.localEulerAngles.z, 0.01f);

            // In 2-mode (Level 3) configuration: toggle strictly alternates Earth <-> Moon
            gc.ConfigureCycle(includeInverted: false, intervalSeconds: 3.0f);
            Assert.AreEqual(3.0f, gc.SwitchIntervalSeconds, 0.01f);

            gc.ToggleGravityMode();
            Assert.AreEqual(GravityMode.Moon, gc.CurrentMode);
            Assert.AreEqual(0.25f, gc.MoonGravityScale, 0.01f);

            gc.ToggleGravityMode();
            Assert.AreEqual(GravityMode.Earth, gc.CurrentMode);

            // With includeInverted enabled: cycles Earth -> Moon -> InvertedRoof -> Earth
            gc.ConfigureCycle(includeInverted: true, intervalSeconds: 3.0f);
            gc.ToggleGravityMode(); // Earth -> Moon
            Assert.AreEqual(GravityMode.Moon, gc.CurrentMode);
            gc.ToggleGravityMode(); // Moon -> InvertedRoof
            Assert.AreEqual(GravityMode.InvertedRoof, gc.CurrentMode);
            Assert.AreEqual(-0.85f, playerGO.GetComponent<Rigidbody2D>().gravityScale, 0.01f);
            Assert.AreEqual(180f, playerGO.transform.localEulerAngles.z, 0.01f);

            gc.ToggleGravityMode(); // InvertedRoof -> Earth
            Assert.AreEqual(GravityMode.Earth, gc.CurrentMode);
            Assert.AreEqual(1.0f, playerGO.GetComponent<Rigidbody2D>().gravityScale, 0.01f);
            Assert.AreEqual(0f, playerGO.transform.localEulerAngles.z, 0.01f);

            gc.Dispose();
            Object.DestroyImmediate(gc);
        }

        [Test]
        public void LevelProgressionManager_Level3_ConfiguresManualLocomotionAndGravity()
        {
            var lpmGO = CreateTestGameObject("TestLPM");
            var lpm = lpmGO.AddComponent<LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                lpm.Initialize(events, player, cam);

                // Advance to Level 3 (index 2)
                lpm.ApplyLevelConfig(2, immediate: true);

                Assert.AreEqual(3, lpm.CurrentLevelNumber);
                Assert.AreEqual(LocomotionMode.Manual, player.CurrentLocomotionMode);
                Assert.IsNotNull(player.GravityController);
                Assert.IsTrue(player.GravityController.enabled);
                Assert.AreEqual(GravityMode.Earth, player.GravityController.CurrentMode);
                Assert.IsFalse(player.GravityController.IncludeInvertedMode, "Level 3 must only have 2 modes: Earth and Moon.");
                Assert.AreEqual(3.0f, player.GravityController.SwitchIntervalSeconds, 0.01f, "Level 3 must shift rapidly in 3 seconds.");

                // Verify Level 3 toggle only alternates Earth <-> Moon
                player.GravityController.ToggleGravityMode();
                Assert.AreEqual(GravityMode.Moon, player.GravityController.CurrentMode);
                player.GravityController.ToggleGravityMode();
                Assert.AreEqual(GravityMode.Earth, player.GravityController.CurrentMode);

                // Switching back to Level 1 restores autonomous mode and disables gravity controller
                lpm.ApplyLevelConfig(0, immediate: true);
                Assert.AreEqual(1, lpm.CurrentLevelNumber);
                Assert.AreEqual(LocomotionMode.Autonomous, player.CurrentLocomotionMode);
                Assert.IsTrue(player.GravityController == null || !player.GravityController.enabled);
                Assert.AreEqual(1.0f, playerGO.GetComponent<Rigidbody2D>().gravityScale, 0.01f);
            }
            finally
            {
                lpm.Dispose();
                Object.DestroyImmediate(lpmGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void PlayerGravityController_RandomCycle_PicksAlternateMode()
        {
            var gc = playerGO.AddComponent<PlayerGravityController>();
            gc.Initialize(playerGO.GetComponent<Rigidbody2D>(), playerGO.GetComponentInChildren<SpriteRenderer>(), events);

            gc.SetRandomCycle(true);
            Assert.IsTrue(gc.IsRandomCycleEnabled, "Random cycle should be enabled.");

            GravityMode initial = gc.CurrentMode;
            gc.SwitchToRandomMode();
            Assert.AreNotEqual(initial, gc.CurrentMode, "SwitchToRandomMode must select a different gravity mode.");

            gc.Dispose();
            Object.DestroyImmediate(gc);
        }

        [Test]
        public void LevelProgressionManager_Level4_ConfiguresManualAndRandomGravity()
        {
            var lpmGO = CreateTestGameObject("TestLPM");
            var lpm = lpmGO.AddComponent<LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                lpm.Initialize(events, player, cam);

                // Advance to Level 4 (index 3)
                lpm.ApplyLevelConfig(3, immediate: true);

                Assert.AreEqual(4, lpm.CurrentLevelNumber);
                Assert.AreEqual(LocomotionMode.Manual, player.CurrentLocomotionMode);
                Assert.IsNotNull(player.GravityController);
                Assert.IsTrue(player.GravityController.enabled);
                Assert.IsTrue(player.GravityController.IsRandomCycleEnabled, "Level 4 should have random gravity modes enabled.");
            }
            finally
            {
                lpm.Dispose();
                Object.DestroyImmediate(lpmGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void Level4Puzzle_Switch_Spike_Platform_Door_Flow()
        {
            // Setup Puzzle Components
            var swGO = CreateTestGameObject("TestSwitch");
            swGO.AddComponent<SpriteRenderer>();
            swGO.AddComponent<BoxCollider2D>();
            var puzzleSwitch = swGO.AddComponent<Game.Gameplay.Environment.Level4Switch>();

            var spGO = CreateTestGameObject("TestSpike");
            spGO.transform.position = new Vector3(0f, -5.3f, 0f);
            spGO.AddComponent<SpriteRenderer>();
            spGO.AddComponent<BoxCollider2D>();
            var movableSpike = spGO.AddComponent<Game.Gameplay.Environment.Level4MovableSpike>();

            var pltGO = CreateTestGameObject("TestPlatform");
            pltGO.AddComponent<SpriteRenderer>();
            pltGO.AddComponent<BoxCollider2D>();
            var movablePlatform = pltGO.AddComponent<Game.Gameplay.Environment.Level4MovablePlatform>();

            var doorGO = CreateTestGameObject("TestDoor");
            doorGO.transform.position = new Vector3(10f, 0f, 0f);
            doorGO.AddComponent<SpriteRenderer>();
            doorGO.AddComponent<BoxCollider2D>();
            var timedDoor = doorGO.AddComponent<Game.Gameplay.Environment.Level4TimedDoor>();

            var coordGO = CreateTestGameObject("TestCoord");
            var coord = coordGO.AddComponent<Game.Gameplay.Environment.Level4PuzzleCoordinator>();
            coord.Initialize(events, puzzleSwitch, movableSpike, movablePlatform, timedDoor);

            // 1. Initial State
            Assert.IsFalse(puzzleSwitch.IsPressed);
            Assert.IsFalse(movableSpike.IsRaised);
            Assert.IsFalse(movablePlatform.IsVisible, "Platform should initially be hidden.");
            Assert.IsFalse(timedDoor.IsOpen, "Door should initially be closed.");

            // 2. Switch Pressed -> Spike Raises
            bool spikeRaised = false;
            puzzleSwitch.SwitchPressed += () => spikeRaised = true;

            var enterMethod = typeof(Game.Gameplay.Environment.Level4Switch).GetMethod("HandlePlayerEnter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enterMethod.Invoke(puzzleSwitch, new object[] { playerGO.GetComponent<Collider2D>() });

            Assert.IsTrue(spikeRaised, "SwitchPressed event should have set spikeRaised.");
            Assert.IsTrue(puzzleSwitch.IsPressed, "Switch should be pressed.");
            Assert.IsTrue(movableSpike.IsRaised, "Movable spike should be raised upon switch press.");

            // 3. Switch Released (player collision not detected) -> Platform Appears
            var exitMethod = typeof(Game.Gameplay.Environment.Level4Switch).GetMethod("HandlePlayerExit", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            exitMethod.Invoke(puzzleSwitch, new object[] { playerGO.GetComponent<Collider2D>() });

            Assert.IsTrue(puzzleSwitch.HasReleasedAfterPress, "Switch should record release.");
            Assert.IsTrue(movablePlatform.IsVisible, "Movable platform should appear after switch is released.");

            // 4. Platform Touched -> Door Opens for 4 seconds
            var platContactMethod = typeof(Game.Gameplay.Environment.Level4MovablePlatform).GetMethod("HandlePlayerContact",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            platContactMethod.Invoke(movablePlatform, new object[] { playerGO.GetComponent<Collider2D>() });

            Assert.IsTrue(timedDoor.RemainingOpenTime > 3.0f && timedDoor.RemainingOpenTime <= 4.05f, "Door should open for 4 seconds.");

            // Cleanup
            Object.DestroyImmediate(swGO);
            Object.DestroyImmediate(spGO);
            Object.DestroyImmediate(pltGO);
            Object.DestroyImmediate(doorGO);
            Object.DestroyImmediate(coordGO);
        }

        [Test]
        public void SkipLevelRequested_Event_AdvancesLevels()
        {
            var managerGO = CreateTestGameObject("TestLPM");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                manager.Initialize(events, player, cam);
                Assert.AreEqual(1, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(2, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(3, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(4, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(5, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(6, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(7, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(8, manager.CurrentLevelNumber);

                events.PublishSkipLevelRequested();
                Assert.AreEqual(1, manager.CurrentLevelNumber);
            }
            finally
            {
                manager.Dispose();
                Object.DestroyImmediate(managerGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void LevelProgressionManager_Level5_ConfiguredProperly()
        {
            var managerGO = CreateTestGameObject("TestLPM5");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam5");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                manager.Initialize(events, player, cam);

                // Advance to Level 5
                manager.ApplyLevelConfig(4, immediate: true);
                Assert.AreEqual(5, manager.CurrentLevelNumber);
                Assert.AreEqual(Game.Gameplay.Player.LocomotionMode.Material, player.CurrentLocomotionMode);

                var matCtrl = player.GetComponent<Project.Player.PlayerMaterialController>();
                Assert.IsNotNull(matCtrl, "Player should have PlayerMaterialController in Level 5.");
                Assert.IsTrue(matCtrl.enabled, "PlayerMaterialController should be enabled in Level 5.");
                Assert.AreEqual(Project.Player.MaterialType.Paper, matCtrl.CurrentMaterial, "Default material in Level 5 must be Paper.");
                Assert.AreEqual(5, matCtrl.RemainingTransformations, "Player should have 5 switch charges in Level 5.");
                Assert.AreEqual(5, matCtrl.MaxTransformations, "Max switch charges in Level 5 must be 5.");
            }
            finally
            {
                manager.Dispose();
                Object.DestroyImmediate(managerGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void LevelProgressionManager_Level6_ConfiguredProperly()
        {
            var managerGO = CreateTestGameObject("TestLPM6");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam6");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                manager.Initialize(events, player, cam);

                // Advance to Level 6 (index 5)
                manager.ApplyLevelConfig(5, immediate: true);
                Assert.AreEqual(6, manager.CurrentLevelNumber);
                Assert.AreEqual(Game.Gameplay.Player.LocomotionMode.Material, player.CurrentLocomotionMode);

                var matCtrl = player.GetComponent<Project.Player.PlayerMaterialController>();
                Assert.IsNotNull(matCtrl, "Player should have PlayerMaterialController in Level 6.");
                Assert.IsTrue(matCtrl.enabled, "PlayerMaterialController should be enabled in Level 6.");
                Assert.AreEqual(Project.Player.MaterialType.Paper, matCtrl.CurrentMaterial, "Default material in Level 6 must be Paper.");
                Assert.AreEqual(5, matCtrl.RemainingTransformations, "Player should have 5 switch charges in Level 6.");
                Assert.AreEqual(5, matCtrl.MaxTransformations, "Max switch charges in Level 6 must be 5.");
                Assert.AreEqual(400.60f, player.transform.position.x, 0.05f, "Player should spawn at Level 6 spawn point X.");
                Assert.AreEqual(0.30f, player.transform.position.y, 0.05f, "Player should spawn at Level 6 spawn point Y.");
                Assert.AreEqual(46.0f, cam.orthographicSize, 0.01f);
            }
            finally
            {
                manager.Dispose();
                Object.DestroyImmediate(managerGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void PlayerMaterialController_MaterialSwitchLimit_EnforcedStrictly()
        {
            var testGO = CreateTestGameObject("TestMaterialPlayer");
            var rb = testGO.AddComponent<Rigidbody2D>();
            var col = testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);

                Assert.AreEqual(Project.Player.MaterialType.Paper, matCtrl.CurrentMaterial);
                Assert.AreEqual(5, matCtrl.RemainingTransformations);

                // Switching to same material does not consume charge
                bool sameMatResult = matCtrl.TryTransform(Project.Player.MaterialType.Paper);
                Assert.IsFalse(sameMatResult, "Transforming to current material must return false.");
                Assert.AreEqual(5, matCtrl.RemainingTransformations, "Charges must not decrease when transforming to same material.");

                // 1st switch: Paper -> Stone
                Assert.IsTrue(matCtrl.TryTransform(Project.Player.MaterialType.Stone));
                Assert.AreEqual(Project.Player.MaterialType.Stone, matCtrl.CurrentMaterial);
                Assert.AreEqual(4, matCtrl.RemainingTransformations);

                // 2nd switch: Stone -> Rubber
                Assert.IsTrue(matCtrl.TryTransform(Project.Player.MaterialType.Rubber));
                Assert.AreEqual(Project.Player.MaterialType.Rubber, matCtrl.CurrentMaterial);
                Assert.AreEqual(3, matCtrl.RemainingTransformations);

                // 3rd switch: Rubber -> Paper
                Assert.IsTrue(matCtrl.TryTransform(Project.Player.MaterialType.Paper));
                Assert.AreEqual(Project.Player.MaterialType.Paper, matCtrl.CurrentMaterial);
                Assert.AreEqual(2, matCtrl.RemainingTransformations);

                // 4th switch: Paper -> Stone
                Assert.IsTrue(matCtrl.TryTransform(Project.Player.MaterialType.Stone));
                Assert.AreEqual(Project.Player.MaterialType.Stone, matCtrl.CurrentMaterial);
                Assert.AreEqual(1, matCtrl.RemainingTransformations);

                // 5th switch: Stone -> Rubber
                Assert.IsTrue(matCtrl.TryTransform(Project.Player.MaterialType.Rubber));
                Assert.AreEqual(Project.Player.MaterialType.Rubber, matCtrl.CurrentMaterial);
                Assert.AreEqual(0, matCtrl.RemainingTransformations);

                // 6th switch attempt: Must fail because remaining is 0!
                Assert.IsFalse(matCtrl.TryTransform(Project.Player.MaterialType.Paper), "6th switch must be rejected.");
                Assert.AreEqual(Project.Player.MaterialType.Rubber, matCtrl.CurrentMaterial, "Material must remain unchanged after rejected switch.");
                Assert.AreEqual(0, matCtrl.RemainingTransformations, "Remaining charges must remain 0.");
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void PlayerMaterialController_RubberProgressiveBounce_TiersAdvanceCorrectly()
        {
            var testGO = CreateTestGameObject("TestRubberBouncePlayer");
            var rb = testGO.AddComponent<Rigidbody2D>();
            testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Rubber, switches: 5);
                Assert.AreEqual(Project.Player.MaterialType.Rubber, matCtrl.CurrentMaterial);
                Assert.AreEqual(0, matCtrl.BounceComboTier, "Initial bounce combo tier should be 0.");

                // 1st Bounce: bounces a little (13.0f)
                matCtrl.ExecuteRubberBounce();
                Assert.AreEqual(matCtrl.RubberBounce1Velocity, rb.linearVelocity.y, 0.01f, "1st bounce should have RubberBounce1Velocity.");
                Assert.AreEqual(1, matCtrl.BounceComboTier, "After 1st bounce, combo tier should be 1.");

                // 2nd Bounce: bounces a little higher (18.5f)
                matCtrl.ExecuteRubberBounce();
                Assert.AreEqual(matCtrl.RubberBounce2Velocity, rb.linearVelocity.y, 0.01f, "2nd bounce should have RubberBounce2Velocity.");
                Assert.AreEqual(2, matCtrl.BounceComboTier, "After 2nd bounce, combo tier should be 2.");

                // 3rd Bounce: bounces a lil more higher (24.0f)
                matCtrl.ExecuteRubberBounce();
                Assert.AreEqual(matCtrl.RubberBounce3Velocity, rb.linearVelocity.y, 0.01f, "3rd bounce should have RubberBounce3Velocity.");
                Assert.AreEqual(0, matCtrl.BounceComboTier, "After 3rd bounce, combo tier should cycle back to 0.");

                // Verify relative bounce heights: Tier 1 < Tier 2 < Tier 3
                Assert.Less(matCtrl.RubberBounce1Velocity, matCtrl.RubberBounce2Velocity);
                Assert.Less(matCtrl.RubberBounce2Velocity, matCtrl.RubberBounce3Velocity);
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void PlayerMaterialController_RubberProgressiveBounce_ResetsOnMaterialSwitch()
        {
            var testGO = CreateTestGameObject("TestRubberBounceReset");
            var rb = testGO.AddComponent<Rigidbody2D>();
            testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Rubber, switches: 5);
                matCtrl.ExecuteRubberBounce(); // Advance to tier 1
                Assert.AreEqual(1, matCtrl.BounceComboTier);

                // Switch to Stone -> Bounce combo must reset to 0
                matCtrl.TryTransform(Project.Player.MaterialType.Stone);
                Assert.AreEqual(0, matCtrl.BounceComboTier, "Switching material must reset bounce combo.");

                // Switch back to Rubber -> Starts at tier 0
                matCtrl.TryTransform(Project.Player.MaterialType.Rubber);
                Assert.AreEqual(0, matCtrl.BounceComboTier, "Switching back to Rubber must start at tier 0.");
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void WindObstacle_ParticleEffect_ConfiguredAndPlays()
        {
            var windGO = CreateTestGameObject("TestWindSpawner");
            var col = windGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 18f);
            col.offset = new Vector2(0f, 9f);
            var wind = windGO.AddComponent<Project.Environment.WindObstacle>();

            var psChild = new GameObject("WindParticles");
            psChild.transform.SetParent(windGO.transform);
            var ps = psChild.AddComponent<ParticleSystem>();

            try
            {
                var method = typeof(Project.Environment.WindObstacle).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method.Invoke(wind, null);

                Assert.IsNotNull(wind.WindParticles, "WindObstacle should automatically find or assign its child ParticleSystem.");
                Assert.IsTrue(col.isTrigger, "WindObstacle collider must be a trigger.");
            }
            finally
            {
                Object.DestroyImmediate(windGO);
            }
        }

        [Test]
        public void PlayerMaterialController_FormDurationTimer_RevertsToPaperAfter10Seconds()
        {
            var testGO = CreateTestGameObject("TestFormDurationTimer");
            testGO.AddComponent<Rigidbody2D>();
            testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);
                Assert.IsFalse(matCtrl.HasFormTimerActive);
                Assert.AreEqual(0f, matCtrl.FormDurationTimer, 0.01f);

                // Transform to Rubber
                bool transformed = matCtrl.TryTransform(Project.Player.MaterialType.Rubber);
                Assert.IsTrue(transformed);
                Assert.AreEqual(Project.Player.MaterialType.Rubber, matCtrl.CurrentMaterial);
                Assert.IsTrue(matCtrl.HasFormTimerActive);
                Assert.AreEqual(10f, matCtrl.FormDurationTimer, 0.01f);
                Assert.AreEqual(4, matCtrl.RemainingTransformations);

                // Reverting to Paper resets timer and preserves remaining transformation charges
                matCtrl.RevertToDefaultPaperForm();
                Assert.AreEqual(Project.Player.MaterialType.Paper, matCtrl.CurrentMaterial);
                Assert.IsFalse(matCtrl.HasFormTimerActive);
                Assert.AreEqual(0f, matCtrl.FormDurationTimer, 0.01f);
                Assert.AreEqual(4, matCtrl.RemainingTransformations, "Reversion to Paper must not consume an extra charge.");
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void PlayerMaterialController_RubberWindLift_AppliesModerateLiftAndCapsHeight()
        {
            var testGO = CreateTestGameObject("TestRubberWindLift");
            var rb = testGO.AddComponent<Rigidbody2D>();
            testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);

                // 1. Stone form immunity
                matCtrl.TryTransform(Project.Player.MaterialType.Stone);
                rb.linearVelocity = Vector2.zero;
                matCtrl.ApplyWindForce(new Vector2(0f, 25f), ventBaseY: 0f);
                Assert.AreEqual(0f, rb.linearVelocity.y, 0.01f, "Stone must be completely unaffected by wind.");

                // 2. Rubber form moderate lift below max flying height
                matCtrl.TryTransform(Project.Player.MaterialType.Rubber);
                testGO.transform.position = new Vector3(0f, 2f, 0f); // 2m above vent base 0m
                rb.linearVelocity = Vector2.zero;
                matCtrl.ApplyWindForce(new Vector2(0f, 25f), ventBaseY: 0f);
                Assert.Greater(rb.linearVelocity.y, 0f, "Rubber should lift upward in wind.");
                Assert.LessOrEqual(rb.linearVelocity.y, matCtrl.RubberMaxWindLiftSpeed + 0.1f, "Rubber lift speed must respect max wind lift speed.");

                // 3. Rubber form hovering / dampening above max flying height
                testGO.transform.position = new Vector3(0f, 8f, 0f); // 8m > 6.5m default
                rb.linearVelocity = new Vector2(0f, 5f);
                matCtrl.ApplyWindForce(new Vector2(0f, 25f), ventBaseY: 0f);
                Assert.Less(rb.linearVelocity.y, 5f, "Rubber upward velocity must be dampened when above max flying height.");
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void PlayerMaterialController_Rubber_NoMidAirDoubleJump_OnlyBouncesWhenGrounded()
        {
            var testGO = CreateTestGameObject("TestRubberGroundBounce");
            testGO.AddComponent<Rigidbody2D>();
            testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);
                matCtrl.TryTransform(Project.Player.MaterialType.Rubber);

                // Rubber must NOT have mid-air double jump!
                Assert.IsFalse(matCtrl.CanDoubleJump, "Rubber must not have mid-air double jump enabled.");
                Assert.IsFalse(matCtrl.IsImmuneToHazard("Spike"), "Rubber must be vulnerable to Spikes.");

                // Ground bounce execution advances tiers
                Assert.AreEqual(0, matCtrl.BounceComboTier);
                matCtrl.ExecuteRubberBounce();
                Assert.AreEqual(1, matCtrl.BounceComboTier);
                matCtrl.ExecuteRubberBounce();
                Assert.AreEqual(2, matCtrl.BounceComboTier);
                matCtrl.ExecuteRubberBounce();
                Assert.AreEqual(0, matCtrl.BounceComboTier);
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void PlayerMaterialController_StoneImmunity_ImmuneToWindAndSpikes()
        {
            var pGO = CreateTestGameObject("TestStoneImmunityPlayer");
            var rb = pGO.AddComponent<Rigidbody2D>();
            pGO.AddComponent<BoxCollider2D>();
            var playerCtrl = pGO.AddComponent<Game.Gameplay.Player.AutonomousPlayerController>();
            var matCtrl = pGO.AddComponent<Project.Player.PlayerMaterialController>();

            var spikeGO = CreateTestGameObject("TestSpikeHazard");
            var spikeCol = spikeGO.AddComponent<BoxCollider2D>();
            spikeCol.isTrigger = true;
            var spike = spikeGO.AddComponent<Game.Gameplay.Combat.Spike>();

            try
            {
                matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);

                // 1. In Paper form, touching Spike eliminates player
                var tryEliminateMethod = typeof(Game.Gameplay.Combat.Hazard2D).GetMethod("TryEliminate", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                tryEliminateMethod.Invoke(spike, new object[] { pGO });
                Assert.IsTrue(playerCtrl.IsDead, "Paper form must be eliminated by Spikes.");

                // Reset player for Stone test
                playerCtrl.ResetState(Vector2.zero);
                Assert.IsFalse(playerCtrl.IsDead);

                // 2. Transform to Stone
                matCtrl.TryTransform(Project.Player.MaterialType.Stone);
                Assert.IsTrue(matCtrl.IsStone);
                Assert.IsTrue(matCtrl.IsImmuneToHazard("Spike"), "Stone must be immune to Spikes.");
                Assert.IsTrue(playerCtrl.IsImmuneToHazard("Spike"), "PlayerController must be immune to Spikes when in Stone form.");

                // Contact with spike hazard must NOT eliminate Stone player!
                tryEliminateMethod.Invoke(spike, new object[] { pGO });
                Assert.IsFalse(playerCtrl.IsDead, "Stone form must NOT be eliminated by Spikes!");

                // Calling Kill("Eliminated by Spike") directly must also be deflected!
                playerCtrl.Kill("Eliminated by Spike");
                Assert.IsFalse(playerCtrl.IsDead, "Stone form must deflect Kill calls caused by Spikes!");

                // 3. Stone is also unaffected by heavy wind
                rb.linearVelocity = Vector2.zero;
                matCtrl.ApplyWindForce(new Vector2(0f, 42f), ventBaseY: 0f);
                Assert.AreEqual(0f, rb.linearVelocity.y, 0.01f, "Stone must remain completely unaffected by heavy wind.");
            }
            finally
            {
                Object.DestroyImmediate(pGO);
                Object.DestroyImmediate(spikeGO);
            }
        }

        [Test]
        public void PlayerMaterialController_SuperHeavyWind_AcceleratesPaperAndCapsRubber()
        {
            var testGO = CreateTestGameObject("TestSuperHeavyWind");
            var rb = testGO.AddComponent<Rigidbody2D>();
            testGO.AddComponent<BoxCollider2D>();
            var matCtrl = testGO.AddComponent<Project.Player.PlayerMaterialController>();

            try
            {
                // 1. Paper: blasted rapidly upward by super heavy wind
                matCtrl.ResetToDefault(Project.Player.MaterialType.Paper, switches: 5);
                rb.linearVelocity = Vector2.zero;
                matCtrl.ApplyWindForce(new Vector2(0f, 42f), ventBaseY: 0f);
                Assert.Greater(rb.linearVelocity.y, 5.0f, "Paper must experience a strong initial blast from super heavy wind.");
                Assert.LessOrEqual(rb.linearVelocity.y, matCtrl.PaperMaxWindLiftSpeed + 0.1f, "Paper must respect max wind lift speed.");

                // 2. Rubber: lifted moderately and hovers at max flying height
                matCtrl.TryTransform(Project.Player.MaterialType.Rubber);
                testGO.transform.position = new Vector3(0f, 2f, 0f);
                rb.linearVelocity = Vector2.zero;
                matCtrl.ApplyWindForce(new Vector2(0f, 42f), ventBaseY: 0f);
                Assert.Greater(rb.linearVelocity.y, 0f, "Rubber should lift upward in super heavy wind.");

                // Above max flying height, upward velocity is damped
                testGO.transform.position = new Vector3(0f, 8f, 0f);
                rb.linearVelocity = new Vector2(0f, 5f);
                matCtrl.ApplyWindForce(new Vector2(0f, 42f), ventBaseY: 0f);
                Assert.Less(rb.linearVelocity.y, 5f, "Rubber upward velocity must be damped above max flying height.");
            }
            finally
            {
                Object.DestroyImmediate(testGO);
            }
        }

        [Test]
        public void LevelProgressionManager_Level7_ConfiguredProperly()
        {
            var managerGO = CreateTestGameObject("TestLPM7");
            var manager = managerGO.AddComponent<Game.Gameplay.LevelProgressionManager>();
            var camGO = CreateTestGameObject("TestCam7");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                manager.Initialize(events, player, cam);

                // Advance to Level 7 (index 6)
                manager.ApplyLevelConfig(6, immediate: true);
                Assert.AreEqual(7, manager.CurrentLevelNumber);
                Assert.AreEqual(Game.Gameplay.Player.LocomotionMode.Manual, player.CurrentLocomotionMode);
                Assert.AreEqual(543.30f, player.transform.position.x, 0.05f, "Player should spawn at Level 7 spawn point X.");
                Assert.AreEqual(-0.90f, player.transform.position.y, 0.05f, "Player should spawn at Level 7 spawn point Y.");
                Assert.AreEqual(42.0f, cam.orthographicSize, 0.01f);
            }
            finally
            {
                manager.Dispose();
                Object.DestroyImmediate(managerGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void TransmutableWall_InvertAndRevert_TogglesCollisionAndTimer()
        {
            var wallGO = CreateTestGameObject("TestWall");
            var col = wallGO.AddComponent<BoxCollider2D>();
            var sr = wallGO.AddComponent<SpriteRenderer>();
            var wall = wallGO.AddComponent<TransmutableWall>();

            try
            {
                // Default state: solid
                Assert.IsFalse(wall.IsInverted);
                Assert.IsFalse(col.isTrigger, "Wall must be solid initially.");

                // Invert for 4.0s
                wall.Invert(4.0f);
                Assert.IsTrue(wall.IsInverted, "Wall must be in inverted state.");
                Assert.IsTrue(col.isTrigger, "Wall collider must be a pass-through trigger when inverted.");
                Assert.AreEqual(4.0f, wall.RemainingDuration, 0.01f);

                // Revert
                wall.Revert();
                Assert.IsFalse(wall.IsInverted, "Wall should no longer be inverted after revert.");
                Assert.IsFalse(col.isTrigger, "Wall collider must become solid again after revert.");
            }
            finally
            {
                Object.DestroyImmediate(wallGO);
            }
        }

        [Test]
        public void TransmutableSpike_InvertAndBounce_AppliesUpwardVelocity()
        {
            var spikeGO = CreateTestGameObject("TestSpike");
            var col = spikeGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var spikeComp = spikeGO.AddComponent<Game.Gameplay.Combat.Spike>();
            var spike = spikeGO.AddComponent<TransmutableSpike>();

            var jumperGO = CreateTestGameObject("TestJumper");
            var rb = jumperGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;

            try
            {
                // Default: lethal hazard and Spike script active
                Assert.IsFalse(spike.IsInverted);
                Assert.IsTrue(spikeComp.enabled, "Spike script must be enabled initially.");

                // Invert into Trampoline (3 seconds)
                spike.Invert(3.0f);
                Assert.IsTrue(spike.IsInverted, "Spike must be inverted.");
                Assert.IsFalse(spikeComp.enabled, "Spike script must be disabled while trampoline.");
                Assert.AreEqual(3.0f, spike.RemainingDuration, 0.01f);

                // Player touching inverted spike does NOT die
                var triggerMethod = typeof(AutonomousPlayerController).GetMethod("OnTriggerEnter2D",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                triggerMethod.Invoke(player, new object[] { col });
                Assert.IsFalse(player.IsDead, "Player must NOT die when touching inverted trampoline spike.");

                // Trigger bounce
                var bounceMethod = typeof(TransmutableSpike).GetMethod("CheckAndBounce",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bounceMethod.Invoke(spike, new object[] { jumperGO });

                Assert.AreEqual(spike.BounceVelocity, rb.linearVelocity.y, 0.05f, "Jumper must receive trampoline bounce upward velocity.");

                // Revert
                spike.Revert();
                Assert.IsFalse(spike.IsInverted);
                Assert.IsTrue(spikeComp.enabled, "Spike script must be re-enabled after revert.");
            }
            finally
            {
                Object.DestroyImmediate(spikeGO);
                Object.DestroyImmediate(jumperGO);
            }
        }

        [Test]
        public void Level7PatrolEnemy_PatrolAndAlibiTransmutation_TogglesLethalAndPlatform()
        {
            var enemyGO = CreateTestGameObject("TestEnemy");
            var sr = enemyGO.AddComponent<SpriteRenderer>();
            var enemy = enemyGO.AddComponent<Level7PatrolEnemy>();

            var p1GO = CreateTestGameObject("P1");
            p1GO.transform.position = new Vector3(0f, 0f, 0f);
            var p2GO = CreateTestGameObject("P2");
            p2GO.transform.position = new Vector3(10f, 0f, 0f);
            enemy.Configure(p1GO.transform, p2GO.transform);

            try
            {
                // Default: Enemy mode (lethal active, platform inactive)
                Assert.IsFalse(enemy.IsInverted, "Enemy should start in default mode.");

                var platformCol = enemy.PlatformCollider;
                Assert.IsNotNull(platformCol, "Enemy should have a platform collider.");
                Assert.IsFalse(platformCol.enabled, "Platform collider must be disabled in enemy mode.");

                // Invert into Alibi Ally mode
                enemy.Invert(4.0f);
                Assert.IsTrue(enemy.IsInverted, "Enemy must be in inverted alibi mode.");
                Assert.IsTrue(platformCol.enabled, "Platform collider must be enabled in alibi mode so player can climb/stand.");
                Assert.AreEqual(4.0f, enemy.RemainingDuration, 0.01f);

                // Revert back to Enemy mode
                enemy.Revert();
                Assert.IsFalse(enemy.IsInverted, "Enemy should revert back to normal mode.");
                Assert.IsFalse(platformCol.enabled, "Platform collider must be disabled when reverted back to enemy.");
            }
            finally
            {
                Object.DestroyImmediate(enemyGO);
                Object.DestroyImmediate(p1GO);
                Object.DestroyImmediate(p2GO);
            }
        }

        [Test]
        public void Level7DarknessOrbit_TriggerSurge_EnablesDarknessAndSlowMotion_AndEndsCleanly()
        {
            var orbitGO = CreateTestGameObject("TestDarknessOrbit");
            var orbit = orbitGO.AddComponent<Level7DarknessOrbit>();

            try
            {
                // Initially, darkness is not active and time is 1.0f
                Assert.IsFalse(orbit.IsSurgeActive, "Darkness surge should start inactive.");
                Assert.AreEqual(1.0f, Time.timeScale, 0.01f);

                // Trigger surge: 1.0s, slow motion 0.5x
                orbit.TriggerSurge(1.0f, 0.5f);
                Assert.IsTrue(orbit.IsSurgeActive, "Surge should be active.");
                Assert.AreEqual(0.5f, Time.timeScale, 0.01f, "Time.timeScale should be 0.5x.");
                Assert.AreEqual(0.01f, Time.fixedDeltaTime, 0.001f, "Time.fixedDeltaTime should adapt to slow motion.");

                // End surge: restores normal time and disables darkness
                orbit.EndSurge();
                Assert.IsFalse(orbit.IsSurgeActive, "Surge should be inactive.");
                Assert.AreEqual(1.0f, Time.timeScale, 0.01f, "Time.timeScale should restore to 1.0x.");
                Assert.AreEqual(0.02f, Time.fixedDeltaTime, 0.001f, "Time.fixedDeltaTime should restore to 0.02s.");
            }
            finally
            {
                orbit.RestoreNormalTime();
                Object.DestroyImmediate(orbitGO);
            }
        }

        [Test]
        public void Level8_InitializationAndProgression_SpawnsAtLevel8()
        {
            var managerGO = CreateTestGameObject("TestLPM8");
            var manager = managerGO.AddComponent<LevelProgressionManager>();

            var camGO = CreateTestGameObject("TestCam8");
            var cam = camGO.AddComponent<Camera>();

            try
            {
                manager.Initialize(events, player, cam);

                // Advance to Level 8 (index 7)
                manager.ApplyLevelConfig(7, immediate: true);
                Assert.AreEqual(8, manager.CurrentLevelNumber);
                Assert.AreEqual(Game.Gameplay.Player.LocomotionMode.Manual, player.CurrentLocomotionMode);
                Assert.AreEqual(662.50f, player.transform.position.x, 0.05f, "Player should spawn at Level 8 spawn point X.");
                Assert.AreEqual(-2.80f, player.transform.position.y, 0.05f, "Player should spawn at Level 8 spawn point Y.");
                Assert.AreEqual(42.0f, cam.orthographicSize, 0.01f);
            }
            finally
            {
                manager.Dispose();
                Object.DestroyImmediate(managerGO);
                Object.DestroyImmediate(camGO);
            }
        }

        [Test]
        public void Level7PatrolEnemy_AlibiBoostJump_PropelsPlayerUpwardWithBoostVelocity()
        {
            var enemyGO = CreateTestGameObject("TestEnemyAlibi");
            var enemy = enemyGO.AddComponent<Level7PatrolEnemy>();
            enemyGO.transform.position = new Vector3(0f, 0f, 0f);

            var jumperGO = CreateTestGameObject("TestJumperPlayer");
            jumperGO.transform.position = new Vector3(0f, 0.5f, 0f);
            var jumperCol = jumperGO.AddComponent<BoxCollider2D>();
            var jumperCtrl = jumperGO.AddComponent<AutonomousPlayerController>();
            jumperCtrl.Initialize(events);
            var rb = jumperGO.GetComponent<Rigidbody2D>();
            rb.gravityScale = 1.0f;
            rb.linearVelocity = new Vector2(0f, -1.0f); // falling onto alibi

            try
            {
                // Invert into alibi mode
                enemy.Invert(3.0f);
                Assert.IsTrue(enemy.IsInverted);

                // Simulate player landing on top of alibi
                var boostMethod = typeof(Level7PatrolEnemy).GetMethod("CheckAlibiBoostJump",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                boostMethod.Invoke(enemy, new object[] { jumperGO, null });

                Assert.AreEqual(enemy.BoostJumpVelocity, rb.linearVelocity.y, 0.05f,
                    "Jumping on top of Alibi must apply high boost jump velocity.");
                Assert.AreEqual(19.5f, enemy.BoostJumpVelocity, 0.05f, "Boost jump velocity should be 19.5f.");
            }
            finally
            {
                Object.DestroyImmediate(enemyGO);
                Object.DestroyImmediate(jumperGO);
            }
        }

        [Test]
        public void Level8_EnemyTransmutationAndBoostJump_WorksIdenticalToLevel7()
        {
            var parentGO = CreateTestGameObject("enemy to alabi (1)");
            var enemy = parentGO.AddComponent<Level7PatrolEnemy>();
            var p1 = CreateTestGameObject("path 1 for enemy  (1)");
            p1.transform.SetParent(parentGO.transform);
            p1.transform.localPosition = new Vector3(-5f, 0f, 0f);

            var p2 = CreateTestGameObject("path 2 for enemy  (1)");
            p2.transform.SetParent(parentGO.transform);
            p2.transform.localPosition = new Vector3(5f, 0f, 0f);

            enemy.Configure(p1.transform, p2.transform);

            var jumperGO = CreateTestGameObject("TestJumper8");
            jumperGO.transform.position = parentGO.transform.position + new Vector3(0f, 0.8f, 0f);
            var jumperCol = jumperGO.AddComponent<BoxCollider2D>();
            var jumperCtrl = jumperGO.AddComponent<AutonomousPlayerController>();
            jumperCtrl.Initialize(events);
            var rb = jumperGO.GetComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(0f, -2.0f);

            try
            {
                // Invert into alibi mode
                enemy.Invert(4.0f);
                Assert.IsTrue(enemy.IsInverted);

                // Boost jump check
                var boostMethod = typeof(Level7PatrolEnemy).GetMethod("CheckAlibiBoostJump",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                boostMethod.Invoke(enemy, new object[] { jumperGO, null });

                Assert.AreEqual(enemy.BoostJumpVelocity, rb.linearVelocity.y, 0.05f);
                Assert.AreEqual(19.5f, enemy.BoostJumpVelocity, 0.05f);

                // Revert
                enemy.Revert();
                Assert.IsFalse(enemy.IsInverted);
            }
            finally
            {
                Object.DestroyImmediate(parentGO);
                Object.DestroyImmediate(jumperGO);
            }
        }

        [Test]
        public void Character_SpriteSheets_AreProperlySlicedAndGrounded()
        {
            string[] sheets = new string[]
            {
                "Assets/assets/SpriteSheet/character_idle_breath_strip.png",
                "Assets/assets/SpriteSheet/character_run_strip.png",
                "Assets/assets/SpriteSheet/character_jump_strip.png",
                "Assets/assets/SpriteSheet/character_zerog_strip.png",
                "Assets/assets/SpriteSheet/character_idle_front_noblink_strip.png"
            };

            foreach (var path in sheets)
            {
                var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .ToArray();
                Assert.AreEqual(5, sprites.Length, $"Sheet {path} must contain exactly 5 slices.");
                foreach (var s in sprites)
                {
                    Assert.AreEqual(112f, s.pivot.x, 1f, $"Sprite {s.name} pivot X should be centered at 112px.");
                    Assert.IsTrue(s.pixelsPerUnit >= 80f && s.pixelsPerUnit <= 120f, $"Sprite {s.name} PPU should be ~100 for environmental golden ratio.");
                }
            }
        }

        [Test]
        public void Character_AnimatorController_ContainsAllStatesAndClips()
        {
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                "Assets/_Game/Presentation/Animation/PlayerAnimatorController.controller");
            Assert.IsNotNull(controller, "PlayerAnimatorController must exist.");

            var stateNames = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            Assert.Contains("Idle", stateNames);
            Assert.Contains("Run", stateNames);
            Assert.Contains("Jump", stateNames);
            Assert.Contains("Fall", stateNames);
            Assert.Contains("Climb", stateNames);
            Assert.Contains("ZeroG", stateNames);

            var paramNames = controller.parameters.Select(p => p.name).ToList();
            Assert.Contains("Speed", paramNames);
            Assert.Contains("IsGrounded", paramNames);
            Assert.Contains("VerticalVelocity", paramNames);
            Assert.Contains("IsClimbing", paramNames);
            Assert.Contains("IsZeroG", paramNames);
            Assert.Contains("IsDead", paramNames);
        }

        [Test]
        public void Character_VisualAnimator_UpdatesAnimatorParameters()
        {
            var testGO = CreateTestGameObject("VisualAnimTest");
            var sr = testGO.AddComponent<SpriteRenderer>();
            var rb = testGO.AddComponent<Rigidbody2D>();
            var ctrl = testGO.AddComponent<AutonomousPlayerController>();
            var anim = testGO.AddComponent<Animator>();
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/_Game/Presentation/Animation/PlayerAnimatorController.controller");
            anim.runtimeAnimatorController = controller;

            var visualAnim = testGO.AddComponent<Game.Presentation.Animation.PlayerVisualAnimator>();
            rb.linearVelocity = new Vector2(5.5f, 3.2f);

            var updateMethod = typeof(Game.Presentation.Animation.PlayerVisualAnimator)
                .GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(visualAnim, null);

            Assert.AreEqual(5.5f, anim.GetFloat("Speed"), 0.05f);
            Assert.AreEqual(3.2f, anim.GetFloat("VerticalVelocity"), 0.05f);
            Assert.IsFalse(sr.flipX, "Should not be flipped when moving right");

            rb.linearVelocity = new Vector2(-4.0f, -1.0f);
            updateMethod.Invoke(visualAnim, null);
            Assert.AreEqual(4.0f, anim.GetFloat("Speed"), 0.05f);
            Assert.IsTrue(sr.flipX, "Should be flipped when moving left");
        }

        [Test]
        public void Character_ScaleRatio_ToEnvironment_IsProportional()
        {
            var ladderTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/assets/Props/Ladder.png");
            Assert.IsNotNull(ladderTex, "Ladder texture must exist.");
            float ladderPixelHeight = 1006f;
            float charPixelHeight = 276f;
            float ratio = ladderPixelHeight / charPixelHeight;
            Assert.GreaterOrEqual(ratio, 2.5f, "Ladder should be at least 2.5x player height for readable climbing");
            Assert.LessOrEqual(ratio, 4.5f, "Ladder should not exceed 4.5x player height");
        }

        [Test]
        public void Character_Climbing_SmoothCenteringAndDismount()
        {
            var ladderGO = CreateTestGameObject("TestLadder");
            ladderGO.transform.position = new Vector3(9900f, 500f, 0f);
            var ladderCol = ladderGO.AddComponent<BoxCollider2D>();
            ladderCol.size = new Vector2(1.0f, 6.0f);
            ladderCol.isTrigger = true;
            var ladderZone = ladderGO.AddComponent<Game.Gameplay.Interaction.LadderClimbZone>();

            var playerGO = CreateTestGameObject("TestClimbPlayer");
            playerGO.transform.position = new Vector3(9900.2f, 497.5f, 0f);
            var rb = playerGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            var col = playerGO.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.85f, 2.45f);
            col.offset = new Vector2(0f, 1.25f);
            var ctrl = playerGO.AddComponent<AutonomousPlayerController>();

            var events = new Game.Core.Events.GameEvents();
            ctrl.Initialize(events);

            // Trigger enter climbing
            var onTriggerEnter = typeof(AutonomousPlayerController).GetMethod("OnTriggerEnter2D",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onTriggerEnter.Invoke(ctrl, new object[] { ladderCol });

            Assert.IsTrue(ctrl.IsClimbing, "Player must enter climbing state upon entering ladder trigger");
            Assert.AreEqual(0f, rb.gravityScale, "Gravity scale must be zero while climbing");

            // FixedUpdate centers player X
            var fixedUpdate = typeof(AutonomousPlayerController).GetMethod("FixedUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fixedUpdate.Invoke(ctrl, null);

            Assert.Less(Mathf.Abs(rb.position.x - 9900f), 0.2f, "Player X should move towards ladder center");

            // Advance near top elevation and test clean dismount
            playerGO.transform.position = new Vector3(9900f, ladderZone.TopElevation - 0.2f, 0f);
            fixedUpdate.Invoke(ctrl, null);

            Assert.IsFalse(ctrl.IsClimbing, "Player must dismount climbing state upon reaching ladder top");
            Assert.AreEqual(1f, rb.gravityScale, "Gravity scale must restore to 1.0 after dismount");
            Assert.Greater(rb.linearVelocity.x, 0f, "Player must step forward onto upper platform upon dismount");
        }
    }
}
#endif
