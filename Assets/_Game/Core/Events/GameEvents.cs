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
