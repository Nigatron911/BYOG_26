using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Full-screen credits page. Opened from the title screen or the ending; returns to whoever opened it.
    /// The list scrolls slowly on its own and can be scrolled manually.
    /// </summary>
    public class CreditsUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float autoScrollPixelsPerSecond = 28f;
        [SerializeField] private float autoScrollDelaySeconds = 1.5f;

        private VisualElement overlay;
        private ScrollView scroll;
        private Action onClosed;
        private float openedAt;

        public bool IsOpen => overlay != null && overlay.resolvedStyle.display == DisplayStyle.Flex;

        public void Initialize(UIDocument doc = null)
        {
            if (doc != null) uiDocument = doc;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            var root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root == null) return;

            overlay = root.Q<VisualElement>("CreditsOverlay");
            scroll = root.Q<ScrollView>("CreditsScroll");
            var back = root.Q<Button>("Btn_CreditsBack");
            if (back != null) back.clickable = new Clickable(Close);
            Hide();
        }

        public void Open(Action closedCallback = null)
        {
            if (overlay == null) return;
            onClosed = closedCallback;
            openedAt = Time.unscaledTime;
            if (scroll != null) scroll.scrollOffset = Vector2.zero;
            overlay.style.display = DisplayStyle.Flex;
        }

        public void Close()
        {
            Hide();
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }

        private void Hide()
        {
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (overlay == null || overlay.style.display != DisplayStyle.Flex) return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Close(); return; }

            if (scroll != null && Time.unscaledTime - openedAt > autoScrollDelaySeconds)
            {
                var offset = scroll.scrollOffset;
                offset.y += autoScrollPixelsPerSecond * Time.unscaledDeltaTime;
                scroll.scrollOffset = offset;
            }
        }
    }
}
