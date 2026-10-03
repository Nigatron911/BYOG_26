using System;
using UnityEngine;
using Game.Core.Events;

namespace Game.Gameplay.Environment
{
    /// <summary>
    /// Coordinates puzzle mechanics in Level 4:
    /// 1. Switch pressed -> Movable spikes rise up.
    /// 2. Switch released (no longer touched after press) -> Movable platform appears.
    /// 3. Movable platform touched -> Timed door opens for 4 seconds.
    /// 4. Door entered -> Level complete and black screen.
    /// Strictly adheres to Section 1 (Architecture), Section 4 (Single Responsibility), and Section 7 (Observer Pattern).
    /// </summary>
    public class Level4PuzzleCoordinator : MonoBehaviour
    {
        [Header("Puzzle Components")]
        [SerializeField] private Level4Switch puzzleSwitch;
        [SerializeField] private Level4MovableSpike movableSpike;
        [SerializeField] private Level4MovablePlatform movablePlatform;
        [SerializeField] private Level4TimedDoor timedDoor;

        private GameEvents events;

        public Level4Switch PuzzleSwitch => puzzleSwitch;
        public Level4MovableSpike MovableSpike => movableSpike;
        public Level4MovablePlatform MovablePlatform => movablePlatform;
        public Level4TimedDoor TimedDoor => timedDoor;

        public void Initialize(GameEvents gameEvents, Level4Switch sw = null, Level4MovableSpike spike = null, Level4MovablePlatform platform = null, Level4TimedDoor door = null)
        {
            events = gameEvents;

            if (sw != null) puzzleSwitch = sw;
            if (spike != null) movableSpike = spike;
            if (platform != null) movablePlatform = platform;
            if (door != null) timedDoor = door;

            AutoWireReferencesIfNull();
            Subscribe();
            ResetAll();
        }

        private void Awake()
        {
            AutoWireReferencesIfNull();
            Subscribe();
        }

        private void Start()
        {
            AutoWireReferencesIfNull();
            Subscribe();
        }

        private void OnEnable()
        {
            AutoWireReferencesIfNull();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void AutoWireReferencesIfNull()
        {
            if (puzzleSwitch == null)
            {
                puzzleSwitch = FindFirstObjectByType<Level4Switch>();
            }

            if (movableSpike == null)
            {
                movableSpike = FindFirstObjectByType<Level4MovableSpike>();
                if (movableSpike == null)
                {
                    var spikeObj = GameObject.Find("Movable spike");
                    if (spikeObj != null)
                    {
                        movableSpike = spikeObj.GetComponent<Level4MovableSpike>();
                    }
                }
            }

            if (movablePlatform == null)
            {
                movablePlatform = FindFirstObjectByType<Level4MovablePlatform>();
            }

            if (timedDoor == null)
            {
                timedDoor = FindFirstObjectByType<Level4TimedDoor>();
            }
        }

        private void Subscribe()
        {
            if (puzzleSwitch != null)
            {
                puzzleSwitch.SwitchPressed -= OnSwitchPressed;
                puzzleSwitch.SwitchPressed += OnSwitchPressed;

                puzzleSwitch.SwitchReleased -= OnSwitchReleased;
                puzzleSwitch.SwitchReleased += OnSwitchReleased;
            }

            if (movablePlatform != null)
            {
                movablePlatform.PlayerLanded -= OnPlatformLanded;
                movablePlatform.PlayerLanded += OnPlatformLanded;
            }

            if (events != null)
            {
                events.LevelResetRequested -= OnLevelReset;
                events.LevelResetRequested += OnLevelReset;

                events.SimulationStopped -= OnLevelReset;
                events.SimulationStopped += OnLevelReset;
            }
        }

        private void Unsubscribe()
        {
            if (puzzleSwitch != null)
            {
                puzzleSwitch.SwitchPressed -= OnSwitchPressed;
                puzzleSwitch.SwitchReleased -= OnSwitchReleased;
            }

            if (movablePlatform != null)
            {
                movablePlatform.PlayerLanded -= OnPlatformLanded;
            }

            if (events != null)
            {
                events.LevelResetRequested -= OnLevelReset;
                events.SimulationStopped -= OnLevelReset;
            }
        }

        private void OnSwitchPressed()
        {
            if (movableSpike != null)
            {
                movableSpike.RaiseSpikes();
            }
        }

        private void OnSwitchReleased()
        {
            if (movablePlatform != null)
            {
                movablePlatform.ShowPlatform();
            }
        }

        private void OnPlatformLanded()
        {
            if (timedDoor != null)
            {
                timedDoor.OpenDoor();
            }
        }

        private void OnLevelReset()
        {
            ResetAll();
        }

        public void ResetAll()
        {
            AutoWireReferencesIfNull();
            Subscribe();
            Debug.Log("[Level4PuzzleCoordinator] Resetting all Level 4 puzzle elements to initial state...");
            if (puzzleSwitch != null) puzzleSwitch.ResetState();
            if (movableSpike != null) movableSpike.ResetState();
            if (movablePlatform != null) movablePlatform.ResetState();
            if (timedDoor != null) timedDoor.ResetState();
        }
    }
}
