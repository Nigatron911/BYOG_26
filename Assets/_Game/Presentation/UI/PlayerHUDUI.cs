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

        [Header("Gravity Mode Icon (Levels 3 & 4)")]
        [SerializeField] private Sprite earthIconSprite;
        [SerializeField] private Sprite moonIconSprite;
        [SerializeField] private Sprite invertedIconSprite;

        private AutonomousPlayerController player;
        private VisualElement stuckWarningCard;
        private Label stuckTimerLabel;
        private VisualElement stuckProgressFill;

        private VisualElement gravityIconBox;
        private VisualElement gravityIconImage;
        private int currentLevelNumber = 0;
        private Game.Core.Events.GameEvents events;

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
            EnsureSpritesLoaded();
            BindUI();
        }

        private void OnEnable()
        {
            EnsureSpritesLoaded();
            BindUI();
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.LevelLoaded -= OnLevelLoaded;
            }
        }

        private void EnsureSpritesLoaded()
        {
#if UNITY_EDITOR
            if (earthIconSprite == null) earthIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Paper/Icon_Gravity_Earth.png");
            if (moonIconSprite == null) moonIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Paper/Icon_Gravity_Moon.png");
            if (invertedIconSprite == null) invertedIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Paper/Icon_Gravity_Inverted.png");
#endif
        }

        public void BindPlayer(AutonomousPlayerController targetPlayer, UIDocument doc = null, Game.Core.Events.GameEvents gameEvents = null)
        {
            if (doc != null) uiDocument = doc;
            player = targetPlayer;
            if (gameEvents != null)
            {
                if (events != null) events.LevelLoaded -= OnLevelLoaded;
                events = gameEvents;
                events.LevelLoaded += OnLevelLoaded;
            }
            EnsureSpritesLoaded();
            BindUI();
        }

        private void OnLevelLoaded(int level)
        {
            currentLevelNumber = level;
            UpdateGravityIndicator();
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;
            stuckWarningCard = root.Q<VisualElement>("StuckWarningPanel");
            stuckTimerLabel = root.Q<Label>("StuckTimerText");
            stuckProgressFill = root.Q<VisualElement>("StuckProgressFill");

            gravityIconBox = root.Q<VisualElement>("GravityIconBox");
            gravityIconImage = root.Q<VisualElement>("GravityIconImage");

            gravityStatusCard = root.Q<VisualElement>("GravityStatusPanel");
            gravityModeLabel = root.Q<Label>("GravityModeLabel");
            gravityTimerLabel = root.Q<Label>("GravityTimerLabel");

            materialStatusCard = root.Q<VisualElement>("MaterialStatusPanel");
            materialModeLabel = root.Q<Label>("MaterialModeLabel");
            materialSwitchesLabel = root.Q<Label>("MaterialSwitchesLabel");
            btnMatPaper = root.Q<Button>("Btn_MatPaper");
            btnMatStone = root.Q<Button>("Btn_MatStone");
            btnMatRubber = root.Q<Button>("Btn_MatRubber");

            if (gravityIconBox != null)
            {
                gravityIconBox.style.display = DisplayStyle.None;
            }

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
            if (gravityStatusCard != null)
            {
                gravityStatusCard.style.display = DisplayStyle.None;
            }

            if (gravityIconBox == null) return;

            if (currentLevelNumber <= 0)
            {
                var lm = FindFirstObjectByType<Game.Gameplay.LevelProgressionManager>();
                if (lm != null) currentLevelNumber = lm.CurrentLevelNumber;
            }

            var gravCtrl = player != null ? player.GravityController : null;
            bool isGravityLevel = (currentLevelNumber == 3 || currentLevelNumber == 4) && gravCtrl != null && gravCtrl.enabled;

            if (!isGravityLevel)
            {
                if (gravityIconBox.style.display != DisplayStyle.None)
                {
                    gravityIconBox.style.display = DisplayStyle.None;
                }
                return;
            }

            if (gravityIconBox.style.display != DisplayStyle.Flex)
            {
                gravityIconBox.style.display = DisplayStyle.Flex;
            }

            gravityIconBox.RemoveFromClassList("gravity-icon-box--earth");
            gravityIconBox.RemoveFromClassList("gravity-icon-box--moon");
            gravityIconBox.RemoveFromClassList("gravity-icon-box--inverted");

            gravityIconImage?.RemoveFromClassList("gravity-icon-image--earth");
            gravityIconImage?.RemoveFromClassList("gravity-icon-image--moon");
            gravityIconImage?.RemoveFromClassList("gravity-icon-image--inverted");

            switch (gravCtrl.CurrentMode)
            {
                case GravityMode.Earth:
                    gravityIconBox.AddToClassList("gravity-icon-box--earth");
                    gravityIconImage?.AddToClassList("gravity-icon-image--earth");
                    if (earthIconSprite != null && gravityIconImage != null)
                    {
                        gravityIconImage.style.backgroundImage = new StyleBackground(earthIconSprite);
                    }
                    break;

                case GravityMode.Moon:
                    gravityIconBox.AddToClassList("gravity-icon-box--moon");
                    gravityIconImage?.AddToClassList("gravity-icon-image--moon");
                    if (moonIconSprite != null && gravityIconImage != null)
                    {
                        gravityIconImage.style.backgroundImage = new StyleBackground(moonIconSprite);
                    }
                    break;

                case GravityMode.InvertedRoof:
                    gravityIconBox.AddToClassList("gravity-icon-box--inverted");
                    gravityIconImage?.AddToClassList("gravity-icon-image--inverted");
                    if (invertedIconSprite != null && gravityIconImage != null)
                    {
                        gravityIconImage.style.backgroundImage = new StyleBackground(invertedIconSprite);
                    }
                    break;
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
                    materialSwitchesLabel.style.color = new StyleColor(new Color(0.37f, 0.33f, 0.30f));
                }
                else
                {
                    materialSwitchesLabel.text = $"⚠️ NO SWITCHES REMAINING! (0 / 5){timerTag}{bounceTag}";
                    materialSwitchesLabel.style.color = new StyleColor(new Color(0.71f, 0.26f, 0.18f));
                }
            }
        }
    }
}
