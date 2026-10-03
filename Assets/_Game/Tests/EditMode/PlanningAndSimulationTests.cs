using NUnit.Framework;
using UnityEngine;
using Game.Core.Events;
using Game.Gameplay;
using Game.Gameplay.Player;
using Game.Gameplay.Interaction;

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

            // Level 1: lifetime 6.0s
            manager.ApplyLevelConfig(0, immediate: true);
            Assert.AreEqual(1, manager.CurrentLevelNumber);
            Assert.AreEqual(6.0f, manager.CurrentLevel.toolLifetimeSeconds, 0.01f, "Level 1 tool lifetime should be 6.0s.");

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
    }
}
