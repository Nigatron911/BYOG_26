using UnityEngine;
using UnityEngine.UIElements;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Game Over modal display showing failure cause and Retry button.
    /// Built on UI Toolkit (UnityEngine.UIElements).
    /// Strictly adheres to Section 13 (UI reacts, does not run game rules).
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        private GameEvents events;
        private VisualElement gameOverOverlay;
        private Label causeLabel;
        private Button retryButton;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>() ?? GetComponentInParent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            BindUI();
        }

        public void Initialize(GameEvents gameEvents, UIDocument doc = null)
        {
            if (doc != null) uiDocument = doc;
            events = gameEvents;

            BindUI();

            if (events != null)
            {
                events.PlayerDied += OnPlayerDied;
                events.SimulationStopped += Hide;
                events.LevelResetRequested += Hide;
            }

            Hide();
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;
            gameOverOverlay = root.Q<VisualElement>("GameOverOverlay");
            causeLabel = root.Q<Label>("GameOverCauseText");
            retryButton = root.Q<Button>("Btn_Retry");

            if (retryButton != null)
            {
                retryButton.clicked -= OnRetryClicked;
                retryButton.clicked += OnRetryClicked;
            }
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.PlayerDied -= OnPlayerDied;
                events.SimulationStopped -= Hide;
                events.LevelResetRequested -= Hide;
            }

            if (retryButton != null)
            {
                retryButton.clicked -= OnRetryClicked;
            }
        }

        private void OnPlayerDied(string cause)
        {
            if (gameOverOverlay == null) BindUI();

            if (gameOverOverlay != null)
            {
                gameOverOverlay.style.display = DisplayStyle.Flex;
            }

            if (causeLabel != null)
            {
                causeLabel.text = cause;
            }
        }

        private void OnRetryClicked()
        {
            Hide();
            events?.PublishSimulationStopped();
        }

        public void Hide()
        {
            if (gameOverOverlay != null)
            {
                gameOverOverlay.style.display = DisplayStyle.None;
            }
        }
    }
}
