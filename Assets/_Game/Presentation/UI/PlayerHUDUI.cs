using UnityEngine;
using UnityEngine.UIElements;
using Game.Gameplay.Player;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Displays HUD status including stuck warning timer, progress, and Level 3 gravity mode indicator.
    /// Built on UI Toolkit (UnityEngine.UIElements).
    /// Purely observational (Section 13).
    /// </summary>
    public class PlayerHUDUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        private AutonomousPlayerController player;
        private VisualElement stuckWarningCard;
        private Label stuckTimerLabel;
        private VisualElement stuckProgressFill;

        private VisualElement gravityStatusCard;
        private Label gravityModeLabel;
        private Label gravityTimerLabel;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>() ?? GetComponentInParent<UIDocument>();
            }
            BindUI();
        }

        private void OnEnable()
        {
            BindUI();
        }

        public void BindPlayer(AutonomousPlayerController targetPlayer, UIDocument doc = null)
        {
            if (doc != null) uiDocument = doc;
            player = targetPlayer;
            BindUI();
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;
            stuckWarningCard = root.Q<VisualElement>("StuckWarningPanel");
            stuckTimerLabel = root.Q<Label>("StuckTimerText");
            stuckProgressFill = root.Q<VisualElement>("StuckProgressFill");

            gravityStatusCard = root.Q<VisualElement>("GravityStatusPanel");
            gravityModeLabel = root.Q<Label>("GravityModeLabel");
            gravityTimerLabel = root.Q<Label>("GravityTimerLabel");

            if (stuckWarningCard != null)
            {
                stuckWarningCard.style.display = DisplayStyle.None;
            }

            if (gravityStatusCard != null)
            {
                gravityStatusCard.style.display = DisplayStyle.None;
            }
        }

        private void Update()
        {
            if (stuckWarningCard == null || gravityStatusCard == null)
            {
                BindUI();
                if (stuckWarningCard == null) return;
            }

            UpdateStuckIndicator();
            UpdateGravityIndicator();
        }

        private void UpdateStuckIndicator()
        {
            if (player == null || !player.IsSimulating)
            {
                if (stuckWarningCard != null && stuckWarningCard.style.display != DisplayStyle.None)
                {
                    stuckWarningCard.style.display = DisplayStyle.None;
                }
                return;
            }

            float stuckTime = player.StuckTimer;
            if (stuckTime > 0.5f && !player.IsDead)
            {
                if (stuckWarningCard.style.display != DisplayStyle.Flex)
                {
                    stuckWarningCard.style.display = DisplayStyle.Flex;
                }

                if (stuckProgressFill != null)
                {
                    stuckProgressFill.style.width = new Length(Mathf.Clamp01(player.StuckProgress) * 100f, LengthUnit.Percent);
                }

                if (stuckTimerLabel != null)
                {
                    float remaining = Mathf.Max(0f, player.StuckTimeoutSeconds - stuckTime);
                    stuckTimerLabel.text = $"Stuck! Game Over in: {remaining:F1}s";
                }
            }
            else
            {
                if (stuckWarningCard.style.display != DisplayStyle.None)
                {
                    stuckWarningCard.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateGravityIndicator()
        {
            if (gravityStatusCard == null) return;

            var gravCtrl = player != null ? player.GravityController : null;
            if (gravCtrl == null || !gravCtrl.enabled)
            {
                if (gravityStatusCard.style.display != DisplayStyle.None)
                {
                    gravityStatusCard.style.display = DisplayStyle.None;
                }
                return;
            }

            // Always display gravity status when in Level 3 with PlayerGravityController enabled
            if (gravityStatusCard.style.display != DisplayStyle.Flex)
            {
                gravityStatusCard.style.display = DisplayStyle.Flex;
            }

            gravityStatusCard.RemoveFromClassList("gravity-status-card-earth");
            gravityStatusCard.RemoveFromClassList("gravity-status-card-roof");
            gravityModeLabel?.RemoveFromClassList("gravity-mode-label-earth");
            gravityModeLabel?.RemoveFromClassList("gravity-mode-label-roof");

            switch (gravCtrl.CurrentMode)
            {
                case GravityMode.Earth:
                    gravityStatusCard.AddToClassList("gravity-status-card-earth");
                    if (gravityModeLabel != null)
                    {
                        string prefix = gravCtrl.IsRandomCycleEnabled ? "🎲 RANDOM: 🌍 EARTH GRAVITY" : "🌍 EARTH GRAVITY";
                        gravityModeLabel.text = $"{prefix} [W: Jump | A: Left | D: Right]";
                        gravityModeLabel.AddToClassList("gravity-mode-label-earth");
                    }
                    break;

                case GravityMode.Moon:
                    if (gravityModeLabel != null)
                    {
                        string prefix = gravCtrl.IsRandomCycleEnabled ? "🎲 RANDOM: 🌙 MOON GRAVITY" : "🌙 MOON GRAVITY";
                        gravityModeLabel.text = $"{prefix} (FLOATING) [W: Float Up | A: Left | D: Right]";
                    }
                    break;

                case GravityMode.InvertedRoof:
                    gravityStatusCard.AddToClassList("gravity-status-card-roof");
                    if (gravityModeLabel != null)
                    {
                        string prefix = gravCtrl.IsRandomCycleEnabled ? "🎲 RANDOM: 🔄 ROOF GRAVITY" : "🔄 ROOF GRAVITY";
                        gravityModeLabel.text = $"{prefix} [S: Jump Down | A: Right | D: Left]";
                        gravityModeLabel.AddToClassList("gravity-mode-label-roof");
                    }
                    break;
            }

            if (gravityTimerLabel != null)
            {
                if (player.IsSimulating)
                {
                    gravityTimerLabel.text = $"Switching in {gravCtrl.TimeRemainingInMode:F1}s  •  [G] Toggle";
                }
                else
                {
                    gravityTimerLabel.text = "Simulate to begin gravity cycle  •  [G] Toggle";
                }
            }
        }
    }
}
