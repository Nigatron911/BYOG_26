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

        private VisualElement materialStatusCard;
        private Label materialModeLabel;
        private Label materialSwitchesLabel;
        private Button btnMatPaper;
        private Button btnMatStone;
        private Button btnMatRubber;

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

            materialStatusCard = root.Q<VisualElement>("MaterialStatusPanel");
            materialModeLabel = root.Q<Label>("MaterialModeLabel");
            materialSwitchesLabel = root.Q<Label>("MaterialSwitchesLabel");
            btnMatPaper = root.Q<Button>("Btn_MatPaper");
            btnMatStone = root.Q<Button>("Btn_MatStone");
            btnMatRubber = root.Q<Button>("Btn_MatRubber");

            if (btnMatPaper != null)
            {
                btnMatPaper.clicked -= OnMatPaperClicked;
                btnMatPaper.clicked += OnMatPaperClicked;
            }
            if (btnMatStone != null)
            {
                btnMatStone.clicked -= OnMatStoneClicked;
                btnMatStone.clicked += OnMatStoneClicked;
            }
            if (btnMatRubber != null)
            {
                btnMatRubber.clicked -= OnMatRubberClicked;
                btnMatRubber.clicked += OnMatRubberClicked;
            }

            if (stuckWarningCard != null)
            {
                stuckWarningCard.style.display = DisplayStyle.None;
            }

            if (gravityStatusCard != null)
            {
                gravityStatusCard.style.display = DisplayStyle.None;
            }

            if (materialStatusCard != null)
            {
                materialStatusCard.style.display = DisplayStyle.None;
            }
        }

        private void OnMatPaperClicked() => player?.GetComponent<Project.Player.PlayerMaterialController>()?.TryTransform(Project.Player.MaterialType.Paper);
        private void OnMatStoneClicked() => player?.GetComponent<Project.Player.PlayerMaterialController>()?.TryTransform(Project.Player.MaterialType.Stone);
        private void OnMatRubberClicked() => player?.GetComponent<Project.Player.PlayerMaterialController>()?.TryTransform(Project.Player.MaterialType.Rubber);

        private void Update()
        {
            if (stuckWarningCard == null || gravityStatusCard == null || materialStatusCard == null)
            {
                BindUI();
                if (stuckWarningCard == null) return;
            }

            UpdateStuckIndicator();
            UpdateGravityIndicator();
            UpdateMaterialIndicator();
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

        private void UpdateMaterialIndicator()
        {
            if (materialStatusCard == null) return;

            var matCtrl = player != null ? player.GetComponent<Project.Player.PlayerMaterialController>() : null;
            if (matCtrl == null || !matCtrl.enabled)
            {
                if (materialStatusCard.style.display != DisplayStyle.None)
                {
                    materialStatusCard.style.display = DisplayStyle.None;
                }
                return;
            }

            if (materialStatusCard.style.display != DisplayStyle.Flex)
            {
                materialStatusCard.style.display = DisplayStyle.Flex;
            }

            if (materialModeLabel != null)
            {
                switch (matCtrl.CurrentMaterial)
                {
                    case Project.Player.MaterialType.Paper:
                        materialModeLabel.text = "📄 MATERIAL: PAPER (Light & Floaty)";
                        break;
                    case Project.Player.MaterialType.Stone:
                        materialModeLabel.text = "🪨 MATERIAL: STONE (Heavy • Immune to Wind & Spikes)";
                        break;
                    case Project.Player.MaterialType.Rubber:
                        materialModeLabel.text = "🟢 MATERIAL: RUBBER (Progressive Ground Bounce)";
                        break;
                }
            }

            if (btnMatPaper != null)
            {
                if (matCtrl.CurrentMaterial == Project.Player.MaterialType.Paper) btnMatPaper.AddToClassList("mat-badge-active");
                else btnMatPaper.RemoveFromClassList("mat-badge-active");
            }
            if (btnMatStone != null)
            {
                if (matCtrl.CurrentMaterial == Project.Player.MaterialType.Stone) btnMatStone.AddToClassList("mat-badge-active");
                else btnMatStone.RemoveFromClassList("mat-badge-active");
            }
            if (btnMatRubber != null)
            {
                if (matCtrl.CurrentMaterial == Project.Player.MaterialType.Rubber) btnMatRubber.AddToClassList("mat-badge-active");
                else btnMatRubber.RemoveFromClassList("mat-badge-active");
            }

            if (materialSwitchesLabel != null)
            {
                string timerTag = "";
                if (matCtrl.HasFormTimerActive)
                {
                    timerTag = $"  •  ⏱ {matCtrl.FormDurationTimer:F1}s";
                }

                string bounceTag = "";
                if (matCtrl.CurrentMaterial == Project.Player.MaterialType.Rubber)
                {
                    int currentTierDisplay = matCtrl.BounceComboTier == 0 ? 1 : (matCtrl.BounceComboTier + 1);
                    if (matCtrl.IsGrounded)
                    {
                        bounceTag = $"  •  [Ground Bounce: Tier {currentTierDisplay}/3]";
                    }
                    else
                    {
                        bounceTag = $"  •  [Airborne: Touch Ground for x{currentTierDisplay}/3]";
                    }
                }

                if (matCtrl.RemainingTransformations > 0)
                {
                    materialSwitchesLabel.text = $"Switches Left: {matCtrl.RemainingTransformations} / {matCtrl.MaxTransformations}{timerTag}{bounceTag}  •  [1: Paper | 2: Stone | 3: Rubber | Q/E: Cycle]";
                    materialSwitchesLabel.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                }
                else
                {
                    materialSwitchesLabel.text = $"⚠️ NO SWITCHES REMAINING! (0 / 5){timerTag}{bounceTag}";
                    materialSwitchesLabel.style.color = new StyleColor(new Color(0.96f, 0.44f, 0.44f));
                }
            }
        }
    }
}
