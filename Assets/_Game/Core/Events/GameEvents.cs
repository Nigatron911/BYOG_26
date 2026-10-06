using System;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// Event bus communicating state changes between loosely coupled systems.
    /// Strictly adheres to Section 7 of the Architecture Contract (Publisher does not control listeners).
    /// </summary>
    public class GameEvents
    {
        public event Action<ToolType> ToolSelected;
        public event Action<ToolType, Vector2> ToolPlaced;
        public event Action ClearToolsRequested;
        public event Action SimulationStarted;
        public event Action SimulationStopped;
        public event Action<string> PlayerDied;
        public event Action LevelCompleted;
        public event Action LevelResetRequested;
        public event Action ToolRotateRequested;
        public event Action<int> LevelLoaded;
        public event Action SkipLevelRequested;

        // Game flow (menus, pause, completion)
        public event Action<int> StartLevelRequested;   // 0-based level index
        public event Action PauseToggleRequested;
        public event Action ResumeRequested;
        public event Action MainMenuRequested;
        public event Action GameCompleted;
        public event Action<GameFlowState> FlowStateChanged;

        // Level presentation
        public event Action<LevelFraming> LevelFramingChanged;
        /// <summary>A new level was entered through a transition: show its how-to-play card (game waits).</summary>
        public event Action<int> LevelBriefingRequested;

        public void PublishToolSelected(ToolType type) => ToolSelected?.Invoke(type);
        public void PublishToolPlaced(ToolType type, Vector2 position) => ToolPlaced?.Invoke(type, position);
        public void PublishClearToolsRequested() => ClearToolsRequested?.Invoke();
        public void PublishSimulationStarted() => SimulationStarted?.Invoke();
        public void PublishSimulationStopped() => SimulationStopped?.Invoke();
        public void PublishPlayerDied(string reason) => PlayerDied?.Invoke(reason);
        public void PublishLevelCompleted() => LevelCompleted?.Invoke();
        public void PublishLevelResetRequested() => LevelResetRequested?.Invoke();
        public void PublishToolRotateRequested() => ToolRotateRequested?.Invoke();
        public void PublishLevelLoaded(int levelNumber) => LevelLoaded?.Invoke(levelNumber);
        public void PublishSkipLevelRequested() => SkipLevelRequested?.Invoke();

        public void PublishStartLevelRequested(int levelIndex) => StartLevelRequested?.Invoke(levelIndex);
        public void PublishPauseToggleRequested() => PauseToggleRequested?.Invoke();
        public void PublishResumeRequested() => ResumeRequested?.Invoke();
        public void PublishMainMenuRequested() => MainMenuRequested?.Invoke();
        public void PublishGameCompleted() => GameCompleted?.Invoke();
        public void PublishFlowStateChanged(GameFlowState state) => FlowStateChanged?.Invoke(state);
        public void PublishLevelFramingChanged(LevelFraming framing) => LevelFramingChanged?.Invoke(framing);
        public void PublishLevelBriefingRequested(int levelNumber) => LevelBriefingRequested?.Invoke(levelNumber);
    }

    /// <summary>World-space area a level occupies and how the camera should present it.</summary>
    public struct LevelFraming
    {
        public int LevelNumber;
        public Rect Bounds;
        /// <summary>True: camera follows the player inside Bounds. False: static view of the whole level (planning levels).</summary>
        public bool FollowPlayer;
    }

    public enum GameFlowState
    {
        MainMenu = 0,
        Playing = 1,
        Paused = 2,
        Completed = 3,
        /// <summary>Level how-to-play card is open; the game is frozen until the player begins.</summary>
        Briefing = 4
    }

    public enum ToolType
    {
        None = 0,
        Plank = 1,
        Ladder = 2,
        Platform = 3,
        Chain = 4,
        // Backward compatibility aliases:
        Ramp = 1,
        Box = 3
    }
}
