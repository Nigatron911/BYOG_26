using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Presentation component managing the in-game Rulebook viewer.
    /// Displays the 4 illustrated rulebook pages (Fading Ink, Fluxed Gravity, Material Metamorphosis, Reality Rewrite).
    /// Strictly adheres to GEMINI.md Section 13 (UI is presentation) and Section 7 (Event-driven).
    /// </summary>
    public class RulebookUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Rulebook Pages")]
        [SerializeField] private Sprite[] rulebookPages = new Sprite[4];

        private readonly string[] actTitles = new string[]
        {
            "Page 1 / 4: Act 1 — Fading Ink",
            "Page 2 / 4: Act 2 — Fluxed Gravity",
            "Page 3 / 4: Act 3 — Material Metamorphosis",
            "Page 4 / 4: Act 4 — Reality Rewrite"
        };

        private GameEvents events;
        private VisualElement overlay;
        private VisualElement modal;
        private VisualElement pageImageElement;
        private Label pageIndicatorLabel;
        private Button btnPrev;
        private Button btnNext;
        private Button btnClose;
        private Button btnOpenHud;

        private int currentPageIndex = 0;
        private bool isOpen = false;

        public bool IsOpen => isOpen;
        public int CurrentPageIndex => currentPageIndex;

        public void Initialize(GameEvents gameEvents, UIDocument doc = null)
        {
            events = gameEvents;
            if (doc != null) uiDocument = doc;

            LoadPagesIfMissing();
            BindUI();

            if (events != null)
            {
                events.LevelLoaded -= OnLevelLoaded;
                events.LevelLoaded += OnLevelLoaded;

                events.OpenRulebookRequested -= OpenToPage;
                events.OpenRulebookRequested += OpenToPage;

                events.CloseRulebookRequested -= Close;
                events.CloseRulebookRequested += Close;

                events.RulebookToggleRequested -= Toggle;
                events.RulebookToggleRequested += Toggle;
            }
        }

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>() ?? GetComponentInParent<UIDocument>();
            }
            LoadPagesIfMissing();
            BindUI();
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.LevelLoaded -= OnLevelLoaded;
                events.OpenRulebookRequested -= OpenToPage;
                events.CloseRulebookRequested -= Close;
                events.RulebookToggleRequested -= Toggle;
            }
        }

        public void LoadPagesIfMissing()
        {
#if UNITY_EDITOR
            string[] paths = new string[]
            {
                "Assets/Ui New/Rulebook PNG/Rulebook/1st.png",
                "Assets/Ui New/Rulebook PNG/Rulebook/2nd.png",
                "Assets/Ui New/Rulebook PNG/Rulebook/3rd.png",
                "Assets/Ui New/Rulebook PNG/Rulebook/4th.png"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                if (rulebookPages[i] == null)
                {
                    rulebookPages[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                }
            }
#endif
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;

            overlay = root.Q<VisualElement>("RulebookOverlay");
            modal = root.Q<VisualElement>("RulebookModal");
            pageImageElement = root.Q<VisualElement>("RulebookPageImage");
            pageIndicatorLabel = root.Q<Label>("RulebookPageIndicator");

            btnPrev = root.Q<Button>("Btn_PrevPage");
            btnNext = root.Q<Button>("Btn_NextPage");
            btnClose = root.Q<Button>("Btn_CloseRulebook");
            btnOpenHud = root.Q<Button>("Btn_Rulebook");

            if (btnPrev != null)
            {
                btnPrev.clicked -= OnPrevClicked;
                btnPrev.clicked += OnPrevClicked;
            }

            if (btnNext != null)
            {
                btnNext.clicked -= OnNextClicked;
                btnNext.clicked += OnNextClicked;
            }

            if (btnClose != null)
            {
                btnClose.clicked -= Close;
                btnClose.clicked += Close;
            }

            if (btnOpenHud != null)
            {
                btnOpenHud.clicked -= Toggle;
                btnOpenHud.clicked += Toggle;
            }

            if (overlay != null)
            {
                overlay.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdatePageDisplay();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.hKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
                {
                    Toggle();
                }

                if (isOpen)
                {
                    if (kb.escapeKey.wasPressedThisFrame)
                    {
                        Close();
                    }
                    else if (kb.leftArrowKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame)
                    {
                        OnPrevClicked();
                    }
                    else if (kb.rightArrowKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                    {
                        OnNextClicked();
                    }
                }
            }
#endif
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            isOpen = true;
            if (overlay == null) BindUI();
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.Flex;
            }
            UpdatePageDisplay();
        }

        public void OpenToPage(int pageIndex)
        {
            currentPageIndex = Mathf.Clamp(pageIndex, 0, 3);
            Open();
        }

        public void Close()
        {
            isOpen = false;
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.None;
            }
        }

        private void OnPrevClicked()
        {
            currentPageIndex = (currentPageIndex - 1 + 4) % 4;
            UpdatePageDisplay();
        }

        private void OnNextClicked()
        {
            currentPageIndex = (currentPageIndex + 1) % 4;
            UpdatePageDisplay();
        }

        private void UpdatePageDisplay()
        {
            if (pageImageElement == null || rulebookPages == null) return;

            Sprite targetSprite = (currentPageIndex >= 0 && currentPageIndex < rulebookPages.Length)
                ? rulebookPages[currentPageIndex]
                : null;

            if (targetSprite != null)
            {
                pageImageElement.style.backgroundImage = new StyleBackground(targetSprite);
            }

            if (pageIndicatorLabel != null && currentPageIndex < actTitles.Length)
            {
                pageIndicatorLabel.text = actTitles[currentPageIndex];
            }
        }

        private void OnLevelLoaded(int levelNumber)
        {
            // Auto-align default rulebook page to current Act
            int targetPage = levelNumber switch
            {
                1 or 2 => 0,
                3 or 4 => 1,
                5 or 6 => 2,
                7 or 8 => 3,
                _ => 0
            };

            currentPageIndex = targetPage;
            UpdatePageDisplay();
        }
    }
}
