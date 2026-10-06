using UnityEngine;
using UnityEngine.UIElements;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>
    /// End-of-game screen shown after the final level is completed.
    /// </summary>
    public class VictoryUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private GameEvents events;
        private VisualElement overlay;

        private CreditsUI credits;
        private bool completed;

        public void Initialize(GameEvents gameEvents, CreditsUI creditsUI = null, UIDocument doc = null)
        {
            credits = creditsUI;
            if (doc != null) uiDocument = doc;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (events != null) events.FlowStateChanged -= OnFlowStateChanged;
            events = gameEvents;

            var root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root != null)
            {
                overlay = root.Q<VisualElement>("VictoryOverlay");
                var again = root.Q<Button>("Btn_PlayAgain");
                if (again != null) again.clickable = new Clickable(() => events?.PublishStartLevelRequested(0));
                var creditsButton = root.Q<Button>("Btn_VictoryCredits");
                if (creditsButton != null) creditsButton.clickable = new Clickable(ShowCredits);
                var menu = root.Q<Button>("Btn_VictoryMainMenu");
                if (menu != null) menu.clickable = new Clickable(() => events?.PublishMainMenuRequested());
            }

            if (events != null) events.FlowStateChanged += OnFlowStateChanged;
        }

        private void OnFlowStateChanged(GameFlowState state)
        {
            completed = state == GameFlowState.Completed;
            if (overlay != null) overlay.style.display = completed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowCredits()
        {
            if (credits == null || overlay == null) return;
            overlay.style.display = DisplayStyle.None;
            credits.Open(() => { if (completed && overlay != null) overlay.style.display = DisplayStyle.Flex; });
        }

        private void OnDestroy()
        {
            if (events != null) events.FlowStateChanged -= OnFlowStateChanged;
        }
    }
}
