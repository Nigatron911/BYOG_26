using NUnit.Framework;
using UnityEngine;
using Game.Core.Events;
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

        [SetUp]
        public void SetUp()
        {
            events = new GameEvents();
            playerGO = new GameObject("TestPlayer");
            playerGO.AddComponent<Rigidbody2D>();
            playerGO.AddComponent<BoxCollider2D>();
            player = playerGO.AddComponent<AutonomousPlayerController>();
            playerGO.transform.position = new Vector3(-7.2f, -1.8f, 0f);
            player.Initialize(events);
        }

        [TearDown]
        public void TearDown()
        {
            if (playerGO != null) Object.DestroyImmediate(playerGO);
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
            var toolGO = new GameObject("TestTool");
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
    }
}
