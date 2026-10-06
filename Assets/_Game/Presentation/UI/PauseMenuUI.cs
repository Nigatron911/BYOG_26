using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Game.Core.Events;
using Game.Core.Save;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Pause menu (Esc / P / HUD button): resume, restart, skip, volume and main menu.
    /// Publishes requests only; time scale is owned by the flow controller.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private GameEvents events;
        private ProgressStore settings;
        private Action onVolumesChanged;
        private Func<string> currentLevelName;
        private GameFlowState flowState = GameFlowState.Playing;

        private VisualElement overlay;
        private VisualElement topRight;
        private VisualElement topHud;
        private VisualElement bottomToolbar;
        private Label levelLabel;
        private Slider musicSlider;
        private Slider sfxSlider;

        public void Initialize(GameEvents gameEvents, ProgressStore store, Action volumesChanged, Func<string> levelNameProvider, UIDocument doc = null)
        {
            if (doc != null) uiDocument = doc;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            Unsubscribe();
            events = gameEvents;
            settings = store;
            onVolumesChanged = volumesChanged;
            currentLevelName = levelNameProvider;

            BindUI();
            if (events != null) events.FlowStateChanged += OnFlowStateChanged;
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;
            var root = uiDocument.rootVisualElement;
            overlay = root.Q<VisualElement>("PauseOverlay");
            topRight = root.Q<VisualElement>("TopRightContainer");
            topHud = root.Q<VisualElement>("TopHUDContainer");
            bottomToolbar = root.Q<VisualElement>("BottomToolbarContainer");
            levelLabel = root.Q<Label>("PauseLevelLabel");
            musicSlider = root.Q<Slider>("Slider_Music");
            sfxSlider = root.Q<Slider>("Slider_Sfx");

            Rebind(root, "Btn_Pause", RequestToggle);
            Rebind(root, "Btn_Resume", () => events?.PublishResumeRequested());
            Rebind(root, "Btn_RestartLevel", () => events?.PublishLevelResetRequested());
            Rebind(root, "Btn_SkipLevel", () => events?.PublishSkipLevelRequested());
            Rebind(root, "Btn_PauseMainMenu", () => events?.PublishMainMenuRequested());

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(settings != null ? settings.MusicVolume : 0.7f);
                musicSlider.RegisterValueChangedCallback(OnMusicChanged);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(settings != null ? settings.SfxVolume : 0.9f);
                sfxSlider.RegisterValueChangedCallback(OnSfxChanged);
            }
        }

        private static void Rebind(VisualElement root, string name, Action handler)
        {
            var b = root.Q<Button>(name);
            if (b == null) return;
            b.clickable = new Clickable(handler);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) RequestToggle();
        }

        private void RequestToggle()
        {
            if (flowState == GameFlowState.Playing || flowState == GameFlowState.Paused)
            {
                events?.PublishPauseToggleRequested();
            }
        }

        private void OnFlowStateChanged(GameFlowState state)
        {
            flowState = state;
            if (overlay == null) BindUI();

            if (overlay != null) overlay.style.display = state == GameFlowState.Paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (topRight != null) topRight.style.display = state == GameFlowState.Playing ? DisplayStyle.Flex : DisplayStyle.None;

            // Gameplay HUD stays hidden behind the title and end screens. Visibility (not display) is used
            // so it does not fight the per-level display toggles owned by the HUD/toolbar components.
            var hudVisibility = state == GameFlowState.Playing || state == GameFlowState.Paused ? Visibility.Visible : Visibility.Hidden;
            if (topHud != null) topHud.style.visibility = hudVisibility;
            if (bottomToolbar != null) bottomToolbar.style.visibility = hudVisibility;

            if (state == GameFlowState.Paused && levelLabel != null && currentLevelName != null)
            {
                levelLabel.text = currentLevelName();
            }
        }

        private void OnMusicChanged(ChangeEvent<float> evt)
        {
            if (settings != null) settings.MusicVolume = evt.newValue;
            onVolumesChanged?.Invoke();
        }

        private void OnSfxChanged(ChangeEvent<float> evt)
        {
            if (settings != null) settings.SfxVolume = evt.newValue;
            onVolumesChanged?.Invoke();
        }

        private void Unsubscribe()
        {
            if (events != null) events.FlowStateChanged -= OnFlowStateChanged;
            musicSlider?.UnregisterValueChangedCallback(OnMusicChanged);
            sfxSlider?.UnregisterValueChangedCallback(OnSfxChanged);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
