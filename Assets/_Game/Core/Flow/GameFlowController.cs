using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.Events;
using Game.Core.Save;

namespace Game.Core.Flow
{
    /// <summary>
    /// Owns the top-level game state machine (MainMenu -> Playing <-> Paused -> Completed).
    /// Controls time scale for pausing and records level progress. Owns no level rules.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        private GameEvents events;
        private ProgressStore progress;
        private GameFlowState state = GameFlowState.Playing;
        private float timeScaleBeforePause = 1f;

        public GameFlowState State => state;

        public void Initialize(GameEvents gameEvents, ProgressStore progressStore, bool startInMainMenu)
        {
            Dispose();
            events = gameEvents;
            progress = progressStore;

            events.StartLevelRequested += OnStartLevelRequested;
            events.PauseToggleRequested += OnPauseToggleRequested;
            events.ResumeRequested += OnResumeRequested;
            events.MainMenuRequested += OnMainMenuRequested;
            events.GameCompleted += OnGameCompleted;
            events.LevelLoaded += OnLevelLoaded;
            events.LevelResetRequested += OnResumeRequested;
            events.SkipLevelRequested += OnResumeRequested;
            events.LevelBriefingRequested += OnLevelBriefingRequested;

            SetState(startInMainMenu ? GameFlowState.MainMenu : GameFlowState.Playing);
        }

        private void SetState(GameFlowState next)
        {
            state = next;
            switch (state)
            {
                case GameFlowState.MainMenu:
                case GameFlowState.Paused:
                case GameFlowState.Briefing:
                    if (Time.timeScale > 0f) timeScaleBeforePause = Time.timeScale;
                    Time.timeScale = 0f;
                    break;
                case GameFlowState.Playing:
                    Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
                    break;
                case GameFlowState.Completed:
                    Time.timeScale = 1f;
                    break;
            }
            events?.PublishFlowStateChanged(state);
        }

        private void OnStartLevelRequested(int levelIndex)
        {
            timeScaleBeforePause = 1f;
            SetState(GameFlowState.Playing);
        }

        private void OnPauseToggleRequested()
        {
            if (state == GameFlowState.Playing) SetState(GameFlowState.Paused);
            else if (state == GameFlowState.Paused) SetState(GameFlowState.Playing);
        }

        private void OnResumeRequested()
        {
            if (state == GameFlowState.Paused || state == GameFlowState.Briefing) SetState(GameFlowState.Playing);
        }

        private void OnLevelBriefingRequested(int levelNumber)
        {
            if (state == GameFlowState.Playing) SetState(GameFlowState.Briefing);
        }

        private void OnGameCompleted()
        {
            SetState(GameFlowState.Completed);
        }

        private void OnLevelLoaded(int levelNumber)
        {
            progress?.MarkLevelReached(levelNumber - 1);
        }

        private void OnMainMenuRequested()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Dispose()
        {
            if (events == null) return;
            events.StartLevelRequested -= OnStartLevelRequested;
            events.PauseToggleRequested -= OnPauseToggleRequested;
            events.ResumeRequested -= OnResumeRequested;
            events.MainMenuRequested -= OnMainMenuRequested;
            events.GameCompleted -= OnGameCompleted;
            events.LevelLoaded -= OnLevelLoaded;
            events.LevelResetRequested -= OnResumeRequested;
            events.SkipLevelRequested -= OnResumeRequested;
            events.LevelBriefingRequested -= OnLevelBriefingRequested;
            events = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
