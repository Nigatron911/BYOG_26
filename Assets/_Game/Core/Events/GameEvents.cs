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

        // Presentation & Locomotion Events (Audio, Rulebook, Animations)
        public event Action PlayerJumped;
        public event Action PlayerLanded;
        public event Action<bool> PlayerRunningChanged;
        public event Action<string> GravityModeChanged;
        public event Action<string> MaterialTypeChanged;
        public event Action ObjectBounced;
        public event Action RealityRewriteFired;
        public event Action RealityRewriteExpired;
        public event Action<int> OpenRulebookRequested;
        public event Action CloseRulebookRequested;
        public event Action RulebookToggleRequested;

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

        public void PublishPlayerJumped() => PlayerJumped?.Invoke();
        public void PublishPlayerLanded() => PlayerLanded?.Invoke();
        public void PublishPlayerRunningChanged(bool isRunning) => PlayerRunningChanged?.Invoke(isRunning);
        public void PublishGravityModeChanged(string mode) => GravityModeChanged?.Invoke(mode);
        public void PublishMaterialTypeChanged(string mat) => MaterialTypeChanged?.Invoke(mat);
        public void PublishObjectBounced() => ObjectBounced?.Invoke();
        public void PublishRealityRewriteFired() => RealityRewriteFired?.Invoke();
        public void PublishRealityRewriteExpired() => RealityRewriteExpired?.Invoke();
        public void PublishOpenRulebookRequested(int pageIndex) => OpenRulebookRequested?.Invoke(pageIndex);
        public void PublishCloseRulebookRequested() => CloseRulebookRequested?.Invoke();
        public void PublishRulebookToggleRequested() => RulebookToggleRequested?.Invoke();
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
