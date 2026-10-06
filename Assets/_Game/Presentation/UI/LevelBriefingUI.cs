using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>Text shown on a level's how-to-play card.</summary>
    public struct LevelBriefing
    {
        public string Code;        // "1.A"
        public string Title;       // "FADING INK"
        public string Goal;
        public string HowToPlay;
        public string Catch;
    }

    /// <summary>
    /// Torn-paper "how to play" card shown on the black screen between levels. The game is frozen
    /// (flow state Briefing) until the player clicks "I'M READY" or presses Enter.
    /// The card drops in with a slight paper sway and lifts away when dismissed.
    /// </summary>
    public class LevelBriefingUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float swayDegrees = 0.8f;
        [SerializeField] private float swaySpeed = 0.9f;

        private GameEvents events;
        private Func<int, LevelBriefing> briefingProvider;
        private GameFlowState flowState = GameFlowState.Playing;

        private VisualElement overlay;
        private VisualElement card;
        private Label codeLabel, titleLabel, goalLabel, howLabel, catchLabel;
        private VisualElement howBlock, catchBlock;
        private bool isOpen;
        private float openedAt;

        public void Initialize(GameEvents gameEvents, Func<int, LevelBriefing> provider, UIDocument doc = null)
        {
            if (doc != null) uiDocument = doc;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            Unsubscribe();
            events = gameEvents;
            briefingProvider = provider;

            var root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root != null)
            {
                overlay = root.Q<VisualElement>("BriefingOverlay");
                card = root.Q<VisualElement>("BriefingCard");
                codeLabel = root.Q<Label>("BriefingCode");
                titleLabel = root.Q<Label>("BriefingTitle");
                goalLabel = root.Q<Label>("BriefingGoal");
                howLabel = root.Q<Label>("BriefingHowTo");
                catchLabel = root.Q<Label>("BriefingCatch");
                howBlock = root.Q<VisualElement>("BriefingHowToBlock");
                catchBlock = root.Q<VisualElement>("BriefingCatchBlock");
                var begin = root.Q<Button>("Btn_BriefingBegin");
                if (begin != null)
                {
                    begin.clickable = new Clickable(Begin);
                    begin.focusable = false;
                }
            }
            HideImmediate();

            if (events != null)
            {
                events.LevelBriefingRequested += OnBriefingRequested;
                events.FlowStateChanged += OnFlowStateChanged;
            }
        }

        private void OnBriefingRequested(int levelNumber)
        {
            if (overlay == null || briefingProvider == null) return;
            var b = briefingProvider(levelNumber);
            if (codeLabel != null) codeLabel.text = string.IsNullOrEmpty(b.Code) ? string.Empty : $"LEVEL {b.Code}";
            if (titleLabel != null) titleLabel.text = b.Title ?? string.Empty;
            if (goalLabel != null) goalLabel.text = b.Goal ?? string.Empty;
            if (howLabel != null) howLabel.text = b.HowToPlay ?? string.Empty;
            if (catchLabel != null) catchLabel.text = b.Catch ?? string.Empty;
            if (howBlock != null) howBlock.style.display = string.IsNullOrEmpty(b.HowToPlay) ? DisplayStyle.None : DisplayStyle.Flex;
            if (catchBlock != null) catchBlock.style.display = string.IsNullOrEmpty(b.Catch) ? DisplayStyle.None : DisplayStyle.Flex;

            isOpen = true;
            openedAt = Time.unscaledTime;
            if (card != null) card.style.rotate = StyleKeyword.Null;
            overlay.style.display = DisplayStyle.Flex;
            card?.RemoveFromClassList("briefing-card--in");
            card?.RemoveFromClassList("briefing-card--out");
            // Next frame: add the "in" class so the USS transition plays the drop-in.
            card?.schedule.Execute(() => card.AddToClassList("briefing-card--in")).StartingIn(30);
        }

        private void OnFlowStateChanged(GameFlowState state)
        {
            flowState = state;
            // Any other screen (menu, pause, ending) replaces the card.
            if (state != GameFlowState.Briefing && isOpen) Close();
        }

        private void Begin()
        {
            if (!isOpen) return;
            events?.PublishResumeRequested();
        }

        private void Close()
        {
            isOpen = false;
            if (card == null) { HideImmediate(); return; }
            card.RemoveFromClassList("briefing-card--in");
            card.AddToClassList("briefing-card--out");
            card.schedule.Execute(() => { if (!isOpen) HideImmediate(); }).StartingIn(380);
        }

        private void HideImmediate()
        {
            if (overlay != null) overlay.style.display = DisplayStyle.None;
            card?.RemoveFromClassList("briefing-card--in");
            card?.RemoveFromClassList("briefing-card--out");
        }

        private void Update()
        {
            if (!isOpen) return;

            var kb = Keyboard.current;
            if (kb != null && Time.unscaledTime - openedAt > 0.4f &&
                (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                Begin();
                return;
            }

            // Gentle paper sway while the card is up.
            if (card != null && card.ClassListContains("briefing-card--in"))
            {
                float t = Time.unscaledTime - openedAt;
                card.style.rotate = new Rotate(new Angle(-1.2f + Mathf.Sin(t * swaySpeed) * swayDegrees, AngleUnit.Degree));
            }
        }

        private void Unsubscribe()
        {
            if (events == null) return;
            events.LevelBriefingRequested -= OnBriefingRequested;
            events.FlowStateChanged -= OnFlowStateChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
