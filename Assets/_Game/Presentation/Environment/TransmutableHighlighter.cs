using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Gameplay.Transmutation;

namespace Game.Presentation.Environment
{
    /// <summary>
    /// When the player uses the transmute ability, every changeable object nearby blinks with a warm
    /// highlight so it is obvious which objects can be transmuted. Uses an overlay sprite per object,
    /// so it never fights the objects' own colour feedback. Pure presentation.
    /// </summary>
    public class TransmutableHighlighter : MonoBehaviour
    {
        [SerializeField] private Color highlightColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.75f;
        [SerializeField] private int blinkCount = 3;
        [SerializeField] private float blinkSeconds = 0.3f;
        [SerializeField] private float radius = 90f;

        private Level7TransmutationController controller;
        private Transform player;
        private readonly List<MonoBehaviour> targets = new List<MonoBehaviour>();
        private readonly Dictionary<SpriteRenderer, SpriteRenderer> overlays = new Dictionary<SpriteRenderer, SpriteRenderer>();
        private Coroutine routine;

        public void Bind(Level7TransmutationController transmutation, IEnumerable<MonoBehaviour> transmutables, Transform playerTransform)
        {
            if (controller != null) controller.AbilityUsed -= OnAbilityUsed;
            controller = transmutation;
            player = playerTransform;
            targets.Clear();
            if (transmutables != null)
            {
                foreach (var t in transmutables)
                {
                    if (t is ITransmutable) targets.Add(t);
                }
            }
            if (controller != null) controller.AbilityUsed += OnAbilityUsed;
        }

        private void OnAbilityUsed()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Blink());
        }

        private IEnumerator Blink()
        {
            var active = new List<SpriteRenderer>();
            foreach (var t in targets)
            {
                if (t == null || !t.isActiveAndEnabled) continue;
                if (player != null && Vector2.Distance(player.position, t.transform.position) > radius) continue;
                var overlay = GetOverlay(t);
                // Only what the player can currently see (never the next level's objects).
                var source = overlay != null ? overlay.transform.parent.GetComponent<SpriteRenderer>() : null;
                if (overlay != null && source != null && source.isVisible) active.Add(overlay);
            }

            float total = blinkCount * blinkSeconds;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;   // transmuting triggers slow motion; blink in real time
                float phase = (elapsed % blinkSeconds) / blinkSeconds;
                float a = Mathf.Sin(phase * Mathf.PI) * peakAlpha;
                foreach (var o in active)
                {
                    if (o == null) continue;
                    SyncOverlay(o);
                    var c = highlightColor; c.a = a;
                    o.color = c;
                    o.enabled = true;
                }
                yield return null;
            }
            foreach (var o in active)
            {
                if (o != null) o.enabled = false;
            }
            routine = null;
        }

        /// <summary>Finds the renderer the player actually sees (enemies draw through a child) and gives it an overlay.</summary>
        private SpriteRenderer GetOverlay(MonoBehaviour target)
        {
            SpriteRenderer visible = null;
            foreach (var sr in target.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.enabled && sr.sprite != null && sr.name != "TransmuteHighlight") { visible = sr; break; }
            }
            if (visible == null) return null;

            if (!overlays.TryGetValue(visible, out var overlay) || overlay == null)
            {
                var go = new GameObject("TransmuteHighlight");
                go.transform.SetParent(visible.transform, false);
                overlay = go.AddComponent<SpriteRenderer>();
                overlay.enabled = false;
                overlays[visible] = overlay;
            }
            overlay.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;
            overlay.sortingLayerID = visible.sortingLayerID;
            overlay.sortingOrder = visible.sortingOrder + 1;
            SyncOverlay(overlay);
            return overlay;
        }

        private static void SyncOverlay(SpriteRenderer overlay)
        {
            var source = overlay.transform.parent != null ? overlay.transform.parent.GetComponent<SpriteRenderer>() : null;
            if (source == null) return;
            overlay.sprite = source.sprite;
            overlay.drawMode = source.drawMode;
            if (source.drawMode != SpriteDrawMode.Simple)
            {
                overlay.tileMode = source.tileMode;
                overlay.size = source.size;
            }
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
        }

        private void OnDestroy()
        {
            if (controller != null) controller.AbilityUsed -= OnAbilityUsed;
        }
    }
}
