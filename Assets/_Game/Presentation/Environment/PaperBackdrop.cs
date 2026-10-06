using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.Environment
{
    /// <summary>
    /// Camera-relative sketchbook background: a paper sheet that always fills the view,
    /// horizontally tiled parallax mountain layers anchored to the bottom of the screen,
    /// and slowly drifting clouds. Scales with the camera's orthographic size, so it works
    /// with every per-level camera framing. Pure presentation (Section 14).
    /// </summary>
    [ExecuteAlways]
    public class PaperBackdrop : MonoBehaviour
    {
        [System.Serializable]
        public class StripLayer
        {
            public string name = "Layer";
            public Sprite sprite;
            [Tooltip("0 = locked to camera, 1 = moves with the world.")]
            [Range(0f, 1f)] public float parallax = 0.2f;
            [Tooltip("Layer height as a fraction of the visible screen height.")]
            [Range(0.02f, 1f)] public float heightFraction = 0.3f;
            [Tooltip("Bottom edge, as a fraction of screen height above the bottom of the view.")]
            [Range(-0.5f, 1f)] public float bottomFraction = 0f;
            [Tooltip("When enabled the layer's bottom sits at a fixed world height (e.g. the ground line) instead of the screen bottom.")]
            public bool anchorToWorldY = false;
            public float worldBottomY = 0f;
            public Color tint = Color.white;
            public int sortingOrder = -90;
        }

        [System.Serializable]
        public class CloudLayer
        {
            public Sprite[] sprites = new Sprite[0];
            [Min(1)] public int count = 5;
            [Range(0.02f, 0.5f)] public float heightFraction = 0.12f;
            [Tooltip("Vertical band (fraction of screen height from the bottom) clouds are spread across.")]
            public Vector2 verticalBand = new Vector2(0.62f, 0.92f);
            [Tooltip("Drift speed in screen widths per minute.")]
            public float driftScreensPerMinute = 0.25f;
            [Range(0f, 1f)] public float parallax = 0.1f;
            public Color tint = new Color(1f, 1f, 1f, 0.85f);
            public int sortingOrder = -95;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float referenceOrthoSize = 20f;

        [Header("Parallax focus")]
        [Tooltip("Optional target (usually the player). Its movement drives the parallax, so layers slide even while the camera holds still.")]
        [SerializeField] private Transform focusTarget;
        [Tooltip("How strongly the focus target's offset from the camera centre feeds the parallax.")]
        [SerializeField] private float focusInfluence = 1f;
        [SerializeField, Min(0.01f)] private float focusSmoothTime = 0.35f;

        [Header("Vignette")]
        [SerializeField] private Sprite vignetteSprite;
        [SerializeField] private Color vignetteTint = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private int vignetteSortingOrder = -70;

        [Header("Paper")]
        [SerializeField] private Sprite paperSprite;
        [SerializeField] private Color paperTint = new Color(0.98f, 0.96f, 0.91f, 1f);
        [SerializeField] private int paperSortingOrder = -100;

        [Header("Layers")]
        [SerializeField] private List<StripLayer> strips = new List<StripLayer>();
        [SerializeField] private CloudLayer clouds = new CloudLayer();
        [SerializeField] private float depth = 20f;

        private const string GeneratedRootName = "__Generated";
        private Transform generatedRoot;
        private SpriteRenderer paperRenderer;
        private readonly List<SpriteRenderer[]> stripTiles = new List<SpriteRenderer[]>();
        private readonly List<SpriteRenderer> cloudRenderers = new List<SpriteRenderer>();
        private float[] cloudSeeds = new float[0];
        private SpriteRenderer vignetteRenderer;
        private bool built;
        private Vector2 smoothedFocusOffset;
        private Vector2 focusVelocity;

        public void BindCamera(Camera cam)
        {
            targetCamera = cam;
        }

        public void BindFocus(Transform target)
        {
            focusTarget = target;
        }

        private void OnEnable()
        {
            built = false;
        }

        private void OnValidate()
        {
            built = false;
        }

        private void Rebuild()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            var rootGo = new GameObject(GeneratedRootName) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
            generatedRoot = rootGo.transform;
            generatedRoot.SetParent(transform, false);

            stripTiles.Clear();
            cloudRenderers.Clear();

            paperRenderer = CreateRenderer("Paper", paperSprite, paperTint, paperSortingOrder);
            vignetteRenderer = CreateRenderer("Vignette", vignetteSprite, vignetteTint, vignetteSortingOrder);

            foreach (var layer in strips)
            {
                var tiles = new SpriteRenderer[4];
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = CreateRenderer($"{layer.name}_{i}", layer.sprite, layer.tint, layer.sortingOrder);
                }
                stripTiles.Add(tiles);
            }

            if (clouds != null && clouds.sprites != null && clouds.sprites.Length > 0)
            {
                var rng = new System.Random(1234);
                cloudSeeds = new float[clouds.count * 2];
                for (int i = 0; i < clouds.count; i++)
                {
                    var sprite = clouds.sprites[i % clouds.sprites.Length];
                    cloudRenderers.Add(CreateRenderer($"Cloud_{i}", sprite, clouds.tint, clouds.sortingOrder));
                    cloudSeeds[i * 2] = (float)rng.NextDouble();
                    cloudSeeds[i * 2 + 1] = (float)rng.NextDouble();
                }
            }

            built = true;
        }

        private SpriteRenderer CreateRenderer(string objName, Sprite sprite, Color tint, int order)
        {
            var go = new GameObject(objName) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
            go.transform.SetParent(generatedRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = order;
            sr.enabled = sprite != null;
            return sr;
        }

        private void LateUpdate()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || !cam.orthographic) return;
            if (!built || generatedRoot == null) Rebuild();

            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            Vector3 camPos = cam.transform.position;
            float bottom = camPos.y - cam.orthographicSize;
            float z = camPos.z + depth;

            // Focus offset (player position relative to the camera centre), smoothed so respawns don't snap.
            Vector2 targetOffset = Vector2.zero;
            if (focusTarget != null && focusTarget.gameObject.activeInHierarchy)
            {
                targetOffset = ((Vector2)focusTarget.position - (Vector2)camPos) * focusInfluence;
            }
            if (Application.isPlaying)
            {
                smoothedFocusOffset = Vector2.SmoothDamp(smoothedFocusOffset, targetOffset, ref focusVelocity, focusSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            else
            {
                smoothedFocusOffset = targetOffset;
            }
            float parallaxSourceX = camPos.x + smoothedFocusOffset.x;
            float parallaxSourceY = smoothedFocusOffset.y;

            if (vignetteRenderer != null && vignetteSprite != null)
            {
                Vector2 vs = vignetteSprite.bounds.size;
                vignetteRenderer.transform.position = new Vector3(camPos.x, camPos.y, z - 2f);
                vignetteRenderer.transform.localScale = new Vector3(viewW / vs.x * 1.02f, viewH / vs.y * 1.02f, 1f);
            }

            // Paper sheet: cover the whole view (cropping to keep its aspect ratio).
            if (paperRenderer != null && paperSprite != null)
            {
                Vector2 s = paperSprite.bounds.size;
                float scale = Mathf.Max(viewW / s.x, viewH / s.y) * 1.02f;
                paperRenderer.transform.position = new Vector3(camPos.x, camPos.y, z + 1f);
                paperRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // Tiled parallax strips.
            for (int li = 0; li < strips.Count && li < stripTiles.Count; li++)
            {
                var layer = strips[li];
                var tiles = stripTiles[li];
                if (layer.sprite == null) continue;

                Vector2 s = layer.sprite.bounds.size;
                float scale = (viewH * layer.heightFraction) / s.y;
                float tileW = s.x * scale;
                // Offset in world units, scaled relative to the reference zoom so layers move consistently.
                float scroll = parallaxSourceX * layer.parallax * (referenceOrthoSize / Mathf.Max(0.01f, cam.orthographicSize));
                float offset = Mathf.Repeat(scroll, tileW);
                float startX = camPos.x - viewW * 0.5f - offset;
                float baseY = layer.anchorToWorldY ? layer.worldBottomY : bottom + viewH * layer.bottomFraction;
                // A little vertical parallax too: far layers drift slightly against the focus height.
                float y = baseY - layer.sprite.bounds.min.y * scale - parallaxSourceY * layer.parallax * 0.25f;

                for (int i = 0; i < tiles.Length; i++)
                {
                    var t = tiles[i];
                    if (t == null) continue;
                    float x = startX + i * tileW - layer.sprite.bounds.min.x * scale;
                    t.transform.position = new Vector3(x, y, z - li * 0.01f);
                    t.transform.localScale = new Vector3(scale, scale, 1f);
                    t.enabled = x + layer.sprite.bounds.min.x * scale < camPos.x + viewW * 0.5f;
                }
            }

            // Drifting clouds that wrap around the view.
            if (clouds != null && cloudRenderers.Count > 0)
            {
                float time = Application.isPlaying ? Time.unscaledTime : 0f;
                float span = viewW * 1.4f;
                for (int i = 0; i < cloudRenderers.Count; i++)
                {
                    var sr = cloudRenderers[i];
                    if (sr == null || sr.sprite == null) continue;
                    Vector2 s = sr.sprite.bounds.size;
                    float scale = (viewH * clouds.heightFraction) / s.y * Mathf.Lerp(0.75f, 1.25f, cloudSeeds[i * 2 + 1]);
                    float drift = time / 60f * clouds.driftScreensPerMinute * viewW * Mathf.Lerp(0.7f, 1.3f, cloudSeeds[i * 2 + 1]);
                    float baseX = cloudSeeds[i * 2] * span + drift - parallaxSourceX * clouds.parallax;
                    float x = camPos.x - span * 0.5f + Mathf.Repeat(baseX, span);
                    float band = Mathf.Lerp(clouds.verticalBand.x, clouds.verticalBand.y, (i + 0.5f) / cloudRenderers.Count);
                    float y = bottom + viewH * band;
                    sr.transform.position = new Vector3(x, y, z - 0.5f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }
    }
}
