using System.Collections;
using UnityEngine;

namespace Game.Gameplay.Transmutation
{
    /// <summary>
    /// Level 7 darkness fog with a circular flashlight / torchlight spotlight following the player.
    /// Everything outside the spotlight is obscured in total pitch darkness; only the area inside is visible.
    /// Controlled authoritatively by LevelProgressionManager (active strictly in Level 7).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Level7DarknessOrbit : MonoBehaviour
    {
        [Header("Torch Light Settings")]
        [SerializeField] private float innerRadiusWorld = 6.0f;
        [SerializeField] private float outerRadiusWorld = 20.0f;
        [SerializeField] private float overlayWorldSize = 400f;
        [SerializeField] private Color darknessColor = new Color(0.005f, 0.008f, 0.015f, 1.0f);

        [Header("Power Surge (Darkness & Slow Motion)")]
        [SerializeField] private float defaultSurgeDuration = 1.0f;
        [SerializeField] private float slowMotionScale = 0.5f;
        [SerializeField] private float fadeInDuration = 0.08f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Target Tracking")]
        [SerializeField] private Transform followTarget;

        private SpriteRenderer overlayRenderer;
        private Texture2D darknessTexture;
        private Sprite darknessSprite;

        private bool isSurgeActive = false;
        private float surgeRemainingTimer = 0f;
        private float totalSurgeDuration = 1.0f;
        private float currentAlpha = 0f;
        private bool wasTimeScaleModified = false;

        public float OrbitRadius => outerRadiusWorld;
        public float InnerRadius => innerRadiusWorld;
        public float OuterRadius => outerRadiusWorld;
        public bool IsSurgeActive => isSurgeActive;
        public float CurrentAlpha => currentAlpha;
        public float SlowMotionScale => slowMotionScale;
        public void SetTarget(Transform target) => followTarget = target;

        public void SetTorchRadius(float inner, float outer)
        {
            innerRadiusWorld = inner;
            outerRadiusWorld = outer;
            GenerateOrbitTexture();
            if (overlayRenderer != null && darknessSprite != null)
            {
                overlayRenderer.sprite = darknessSprite;
            }
        }

        public void TriggerSurge(float duration = -1f, float customSlowMo = -1f)
        {
            EnsureOverlaySprite();
            totalSurgeDuration = duration > 0f ? duration : defaultSurgeDuration;
            surgeRemainingTimer = totalSurgeDuration;
            isSurgeActive = true;

            float targetScale = customSlowMo > 0f ? customSlowMo : slowMotionScale;
            Time.timeScale = targetScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            wasTimeScaleModified = true;

            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = true;
                ApplyAlpha(0.05f);
            }
        }

        public void EndSurge()
        {
            isSurgeActive = false;
            surgeRemainingTimer = 0f;
            currentAlpha = 0f;
            ApplyAlpha(0f);

            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = false;
            }

            RestoreNormalTime();
        }

        public void RestoreNormalTime()
        {
            if (wasTimeScaleModified || Mathf.Abs(Time.timeScale - 1.0f) > 0.001f)
            {
                Time.timeScale = 1.0f;
                Time.fixedDeltaTime = 0.02f;
                wasTimeScaleModified = false;
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureOverlaySprite();
            if (!visible)
            {
                EndSurge();
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void Awake()
        {
            EnsureOverlaySprite();
            ApplyAlpha(0f);
            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            EnsureOverlaySprite();
        }

        private void OnDisable()
        {
            EndSurge();
            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (isSurgeActive)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                surgeRemainingTimer -= dt;

                float elapsed = totalSurgeDuration - surgeRemainingTimer;
                float alpha = 1f;

                if (elapsed < fadeInDuration && fadeInDuration > 0f)
                {
                    alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                }
                else if (surgeRemainingTimer < fadeOutDuration && fadeOutDuration > 0f)
                {
                    alpha = Mathf.Clamp01(surgeRemainingTimer / fadeOutDuration);
                }

                currentAlpha = alpha;
                ApplyAlpha(currentAlpha);

                // Smoothly restore timescale in the final fadeOut window
                if (surgeRemainingTimer < fadeOutDuration && fadeOutDuration > 0f)
                {
                    float t = Mathf.Clamp01(surgeRemainingTimer / fadeOutDuration);
                    float targetScale = Mathf.Lerp(1.0f, slowMotionScale, t);
                    Time.timeScale = targetScale;
                    Time.fixedDeltaTime = 0.02f * Time.timeScale;
                }

                if (surgeRemainingTimer <= 0f)
                {
                    EndSurge();
                }
            }
        }

        private void ApplyAlpha(float a)
        {
            if (overlayRenderer != null)
            {
                overlayRenderer.color = new Color(1f, 1f, 1f, a);
            }
        }

        public void EnsureOverlaySprite()
        {
            if (overlayRenderer == null)
            {
                overlayRenderer = GetComponent<SpriteRenderer>();
                if (overlayRenderer == null)
                {
                    overlayRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (overlayRenderer != null)
            {
                var unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") 
                    ?? Shader.Find("Sprites/Default");
                if (unlitShader != null && (overlayRenderer.sharedMaterial == null || overlayRenderer.sharedMaterial.shader != unlitShader))
                {
                    overlayRenderer.material = new Material(unlitShader);
                }
            }

            if (darknessTexture == null)
            {
                GenerateOrbitTexture();
            }

            if (overlayRenderer != null && darknessSprite != null)
            {
                overlayRenderer.sprite = darknessSprite;
                overlayRenderer.sortingLayerName = "Default";
                overlayRenderer.sortingOrder = 500; // Above world objects, below UI
                transform.localScale = new Vector3(overlayWorldSize, overlayWorldSize, 1f);
            }
        }

        private void GenerateOrbitTexture()
        {
            const int resolution = 1024;
            if (darknessTexture == null)
            {
                darknessTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                darknessTexture.name = "Level7_TorchLight_Procedural";
                darknessTexture.wrapMode = TextureWrapMode.Clamp;
                darknessTexture.filterMode = FilterMode.Bilinear;
            }

            Color[] pixels = new Color[resolution * resolution];
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float halfWorldSize = overlayWorldSize * 0.5f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float distNormalized = Vector2.Distance(new Vector2(x, y), center) / (resolution * 0.5f);
                    float distWorld = distNormalized * halfWorldSize;
                    int idx = y * resolution + x;

                    if (distWorld <= innerRadiusWorld)
                    {
                        // Torch core: 100% clear & fully lit
                        pixels[idx] = Color.clear;
                    }
                    else if (distWorld >= outerRadiusWorld)
                    {
                        // 100% solid pitch black darkness
                        pixels[idx] = darknessColor;
                    }
                    else
                    {
                        // Smooth, natural flashlight falloff: darkness fades in softly from clear to 100% black
                        float t = Mathf.InverseLerp(innerRadiusWorld, outerRadiusWorld, distWorld);
                        float blend = Mathf.SmoothStep(0f, 1f, t);
                        pixels[idx] = new Color(darknessColor.r, darknessColor.g, darknessColor.b, blend);
                    }
                }
            }

            darknessTexture.SetPixels(pixels);
            darknessTexture.Apply();

            if (darknessSprite != null)
            {
                DestroyImmediate(darknessSprite);
            }

            darknessSprite = Sprite.Create(
                darknessTexture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution
            );
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                var p = FindFirstObjectByType<Game.Gameplay.Player.AutonomousPlayerController>();
                if (p != null) followTarget = p.transform;
            }

            if (followTarget != null)
            {
                transform.position = new Vector3(followTarget.position.x, followTarget.position.y, -2f);
            }

            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = isSurgeActive && currentAlpha > 0.001f;
            }
        }

        private void OnDestroy()
        {
            if (darknessTexture != null)
            {
                Destroy(darknessTexture);
            }
            if (darknessSprite != null)
            {
                Destroy(darknessSprite);
            }
        }
    }
}
