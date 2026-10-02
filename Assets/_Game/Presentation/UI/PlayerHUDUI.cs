using UnityEngine;
using UnityEngine.UIElements;
using Game.Gameplay.Player;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Displays HUD status including stuck warning timer and progress.
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

            if (stuckWarningCard != null)
            {
                stuckWarningCard.style.display = DisplayStyle.None;
            }
        }

        private void Update()
        {
            if (stuckWarningCard == null)
            {
                BindUI();
                if (stuckWarningCard == null) return;
            }

            if (player == null || !player.IsSimulating)
            {
                if (stuckWarningCard.style.display != DisplayStyle.None)
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
    }
}
