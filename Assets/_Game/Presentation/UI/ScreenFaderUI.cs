using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Presentation.UI
{
    /// <summary>
    /// Presentation component managing screen fades (black screen transitions).
    /// Built on UI Toolkit (UnityEngine.UIElements).
    /// Purely reacts to commands/events; owns no gameplay logic (Section 13).
    /// </summary>
    public class ScreenFaderUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float defaultFadeDuration = 0.6f;

        private VisualElement faderOverlay;
        private Coroutine activeFadeRoutine;

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

        public void Initialize(UIDocument doc)
        {
            if (doc != null) uiDocument = doc;
            BindUI();
        }

        private void BindUI()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;
            faderOverlay = uiDocument.rootVisualElement.Q<VisualElement>("ScreenFaderOverlay");
        }

        public void FadeIn(float duration = -1f, Action onComplete = null)
        {
            float d = duration > 0f ? duration : defaultFadeDuration;
            StartFade(1f, 0f, d, onComplete);
        }

        public void FadeOut(float duration = -1f, Action onComplete = null)
        {
            float d = duration > 0f ? duration : defaultFadeDuration;
            StartFade(0f, 1f, d, onComplete);
        }

        private void StartFade(float fromAlpha, float toAlpha, float duration, Action onComplete)
        {
            if (activeFadeRoutine != null)
            {
                StopCoroutine(activeFadeRoutine);
            }
            activeFadeRoutine = StartCoroutine(FadeRoutine(fromAlpha, toAlpha, duration, onComplete));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
        {
            if (faderOverlay == null)
            {
                BindUI();
                if (faderOverlay == null) yield break;
            }

            faderOverlay.pickingMode = to > 0.05f ? PickingMode.Position : PickingMode.Ignore;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                faderOverlay.style.opacity = new StyleFloat(alpha);
                yield return null;
            }

            faderOverlay.style.opacity = new StyleFloat(to);
            faderOverlay.pickingMode = to > 0.05f ? PickingMode.Position : PickingMode.Ignore;
            activeFadeRoutine = null;
            onComplete?.Invoke();
        }
    }
}
