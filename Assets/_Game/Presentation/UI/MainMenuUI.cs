using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Core.Events;
using Game.Core.Save;

namespace Game.Presentation.UI
{
    /// <summary>
    /// "TOTAL FLUX" title screen (Play/Continue, New Game, Levels, Credits, Quit) and the level-select screen.
    /// Publishes requests only; the flow controller and level manager act on them (Section 13).
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private GameEvents events;
        private ProgressStore progress;
        private IReadOnlyList<string> levelNames;
        private CreditsUI credits;

        private VisualElement titleOverlay;
        private VisualElement levelSelectOverlay;
        private VisualElement levelGrid;
        private Label playLabel;
        private Button btnPlay, btnNewGame, btnQuit;
        private bool menuActive;
        private VisualElement[] covers = new VisualElement[0];
        private VisualElement[] vignettes = new VisualElement[0];

        public void Initialize(GameEvents gameEvents, ProgressStore progressStore, IReadOnlyList<string> names, CreditsUI creditsUI, UIDocument doc = null)
        {
            if (doc != null) uiDocument = doc;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            Unsubscribe();
            events = gameEvents;
            progress = progressStore;
            levelNames = names;
            credits = creditsUI;

            BindUI();
            if (events != null) events.FlowStateChanged += OnFlowStateChanged;
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;
            var root = uiDocument.rootVisualElement;
            titleOverlay = root.Q<VisualElement>("MainMenuOverlay");
            levelSelectOverlay = root.Q<VisualElement>("LevelSelectOverlay");
            levelGrid = root.Q<VisualElement>("LevelGrid");
            playLabel = root.Q<Label>("Label_Play");
            covers = root.Query<VisualElement>(className: "title-cover").ToList().ToArray();
            vignettes = root.Query<VisualElement>(className: "screen-vignette").ToList().ToArray();

            btnPlay = Rebind(root, "Btn_Play", OnPlay);
            btnNewGame = Rebind(root, "Btn_NewGame", OnNewGame);
            Rebind(root, "Btn_LevelSelect", ShowLevelSelect);
            Rebind(root, "Btn_Credits", ShowCredits);
            btnQuit = Rebind(root, "Btn_Quit", OnQuit);
            Rebind(root, "Btn_LevelSelectBack", ShowTitle);

#if UNITY_WEBGL
            if (btnQuit != null) btnQuit.style.display = DisplayStyle.None;
#endif
        }

        private static Button Rebind(VisualElement root, string name, System.Action handler)
        {
            var b = root.Q<Button>(name);
            if (b != null) b.clickable = new Clickable(handler);
            return b;
        }

        private void OnFlowStateChanged(GameFlowState state)
        {
            menuActive = state == GameFlowState.MainMenu;
            if (menuActive) ShowTitle();
            else HideAll();
        }

        private void ShowTitle()
        {
            if (titleOverlay == null) BindUI();
            if (titleOverlay == null) return;

            bool hasProgress = progress != null && progress.HasProgress;
            if (playLabel != null) playLabel.text = hasProgress ? $"CONTINUE  {LevelCode(progress.HighestUnlockedLevelIndex)}" : "PLAY";
            if (btnNewGame != null) btnNewGame.style.display = hasProgress ? DisplayStyle.Flex : DisplayStyle.None;

            SetDisplay(levelSelectOverlay, false);
            SetDisplay(titleOverlay, true);
        }

        private void ShowLevelSelect()
        {
            BuildLevelGrid();
            SetDisplay(titleOverlay, false);
            SetDisplay(levelSelectOverlay, true);
        }

        private void ShowCredits()
        {
            if (credits == null) return;
            SetDisplay(titleOverlay, false);
            credits.Open(() => { if (menuActive) ShowTitle(); });
        }

        private void HideAll()
        {
            SetDisplay(titleOverlay, false);
            SetDisplay(levelSelectOverlay, false);
        }

        private static void SetDisplay(VisualElement e, bool visible)
        {
            if (e != null) e.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Update()
        {
            AnimateCover();
            if (!menuActive || levelSelectOverlay == null || levelSelectOverlay.style.display != DisplayStyle.Flex) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) ShowTitle();
        }

        /// <summary>
        /// Gives the cover art a slow, living-paper feel: a gentle breathing zoom, a drift like a sheet
        /// resting on a desk, and a soft flicker in the vignette. Runs on unscaled time (menus freeze time).
        /// </summary>
        private void AnimateCover()
        {
            if (covers.Length == 0) return;
            float t = Time.unscaledTime;
            float scale = 1.02f + 0.012f * Mathf.Sin(t * 0.21f);
            float rot = 0.35f * Mathf.Sin(t * 0.13f + 0.6f);
            float dx = 6f * Mathf.Sin(t * 0.09f);
            float dy = 4f * Mathf.Sin(t * 0.11f + 1.3f);
            for (int i = 0; i < covers.Length; i++)
            {
                var c = covers[i];
                if (c == null || c.parent == null || c.parent.resolvedStyle.display == DisplayStyle.None) continue;
                c.style.scale = new Scale(new Vector3(scale, scale, 1f));
                c.style.rotate = new Rotate(new Angle(rot, AngleUnit.Degree));
                c.style.translate = new Translate(dx, dy);
            }
            float vig = 0.9f + 0.08f * Mathf.Sin(t * 0.7f) + 0.03f * Mathf.Sin(t * 2.3f);
            for (int i = 0; i < vignettes.Length; i++)
            {
                if (vignettes[i] != null) vignettes[i].style.opacity = vig;
            }
        }

        private void OnPlay()
        {
            int index = progress != null ? progress.HighestUnlockedLevelIndex : 0;
            if (levelNames != null && levelNames.Count > 0) index = Mathf.Clamp(index, 0, levelNames.Count - 1);
            events?.PublishStartLevelRequested(index);
        }

        private void OnNewGame()
        {
            progress?.ResetProgress();
            events?.PublishStartLevelRequested(0);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildLevelGrid()
        {
            if (levelGrid == null || levelNames == null) return;
            levelGrid.Clear();
            int unlocked = progress != null ? progress.HighestUnlockedLevelIndex : 0;

            for (int i = 0; i < levelNames.Count; i++)
            {
                int index = i;
                bool isUnlocked = i <= unlocked || Debug.isDebugBuild;

                var card = new Button(() => events?.PublishStartLevelRequested(index));
                card.AddToClassList("level-card");
                card.AddToClassList($"level-card-world-{Mathf.Min(4, i / 2 + 1)}");

                var name = new Label(ShortName(levelNames[i])) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("level-card-name");
                var number = new Label(LevelCode(i)) { pickingMode = PickingMode.Ignore };
                number.AddToClassList("level-card-number");
                var lockLabel = new Label(isUnlocked ? string.Empty : "locked") { pickingMode = PickingMode.Ignore };
                lockLabel.AddToClassList("level-card-lock");

                card.Add(name);
                card.Add(number);
                card.Add(lockLabel);
                card.SetEnabled(isUnlocked);
                card.focusable = false;
                levelGrid.Add(card);
            }
        }

        /// <summary>Level index (0-based) -> Figma card code: 0 -> "1.A", 1 -> "1.B", 2 -> "2.A" ...</summary>
        public static string LevelCode(int levelIndex)
        {
            int world = levelIndex / 2 + 1;
            char part = (char)('A' + levelIndex % 2);
            return $"{world}.{part}";
        }

        /// <summary>"Level 3 - Gravity Inversion Chasm" -> "Gravity Inversion Chasm".</summary>
        public static string ShortName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return string.Empty;
            int dash = levelName.IndexOf(" - ", System.StringComparison.Ordinal);
            return dash >= 0 ? levelName.Substring(dash + 3) : levelName;
        }

        private void Unsubscribe()
        {
            if (events != null) events.FlowStateChanged -= OnFlowStateChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
