using UnityEngine;
using Project.Player;

namespace Game.Presentation.Environment
{
    /// <summary>
    /// Ink countdown bar floating above the player while a temporary Stone/Rubber form is active
    /// (material levels). Drains as the form runs out and blinks red in the final seconds.
    /// Pure presentation: reads PlayerMaterialController, never changes it.
    /// </summary>
    public class MaterialFormTimerVisual : MonoBehaviour
    {
        [SerializeField] private float barWidth = 2.6f;
        [SerializeField] private float barHeight = 0.34f;
        [SerializeField] private float outline = 0.09f;
        [SerializeField] private float headClearance = 0.5f;
        [SerializeField] private float warningSeconds = 1.5f;
        [SerializeField] private int sortingOrder = 60;
        [SerializeField] private Color inkColor = new Color(0.10f, 0.08f, 0.07f, 0.95f);
        [SerializeField] private Color trackColor = new Color(0.93f, 0.90f, 0.84f, 0.95f);
        [SerializeField] private Color stoneColor = new Color(0.45f, 0.46f, 0.52f, 1f);
        [SerializeField] private Color rubberColor = new Color(0.30f, 0.66f, 0.36f, 1f);
        [SerializeField] private Color warningColor = new Color(0.78f, 0.18f, 0.14f, 1f);

        private PlayerMaterialController material;
        private SpriteRenderer playerSprite;
        private SpriteRenderer inkRenderer, trackRenderer, fillRenderer;

        public void Bind(PlayerMaterialController materialController)
        {
            material = materialController;
            playerSprite = materialController != null ? materialController.GetComponent<SpriteRenderer>() : null;
        }

        private void Awake()
        {
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
            inkRenderer = CreatePart("Ink", sprite, inkColor, sortingOrder);
            trackRenderer = CreatePart("Track", sprite, trackColor, sortingOrder + 1);
            fillRenderer = CreatePart("Fill", sprite, stoneColor, sortingOrder + 2);
            SetVisible(false);
        }

        private SpriteRenderer CreatePart(string partName, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private void LateUpdate()
        {
            bool active = material != null && material.isActiveAndEnabled && material.HasFormTimerActive
                          && material.FormDurationSeconds > 0f;
            SetVisible(active);
            if (!active) return;

            float fraction = Mathf.Clamp01(material.FormDurationTimer / material.FormDurationSeconds);
            Vector3 top = playerSprite != null ? new Vector3(playerSprite.bounds.center.x, playerSprite.bounds.max.y, 0f) : material.transform.position;
            transform.position = top + new Vector3(0f, headClearance, -0.1f);

            float left = -barWidth * 0.5f;
            Place(inkRenderer.transform, left - outline, barWidth + outline * 2f, barHeight + outline * 2f);
            Place(trackRenderer.transform, left, barWidth, barHeight);
            Place(fillRenderer.transform, left, barWidth * fraction, barHeight);

            bool warning = material.FormDurationTimer <= warningSeconds;
            Color baseColor = material.CurrentMaterial == MaterialType.Stone ? stoneColor : rubberColor;
            if (warning)
            {
                float blink = Mathf.PingPong(Time.time * 6f, 1f);
                fillRenderer.color = Color.Lerp(baseColor, warningColor, blink);
            }
            else
            {
                fillRenderer.color = baseColor;
            }
        }

        private static void Place(Transform part, float x, float width, float height)
        {
            part.localPosition = new Vector3(x, 0f, 0f);
            part.localScale = new Vector3(Mathf.Max(0f, width), height, 1f);
        }

        private void SetVisible(bool visible)
        {
            if (inkRenderer != null && inkRenderer.enabled != visible)
            {
                inkRenderer.enabled = visible;
                trackRenderer.enabled = visible;
                fillRenderer.enabled = visible;
            }
        }
    }
}
