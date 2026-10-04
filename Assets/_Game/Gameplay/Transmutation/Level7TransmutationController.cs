using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Game.Core.Input;
using Game.Core.Interfaces;
using Project.Audio;

namespace Game.Gameplay.Transmutation
{
    /// <summary>
    /// Handles Level 7 mouse cursor crosshair, raycasting, and left-click object transmutation.
    /// Manages custom crosshair cursor and transmutation execution (4.0s inverted state).
    /// </summary>
    public class Level7TransmutationController : MonoBehaviour
    {
        [Header("Transmutation Tuning")]
        [SerializeField] private float invertDuration = 3.0f;
        [SerializeField] private float darknessSurgeDuration = 1.0f;
        [SerializeField] private float slowMotionScale = 0.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private Level7DarknessOrbit darknessOrbit;

        [Header("Crosshair Cursor")]
        [SerializeField] private bool useHardwareCursor = true;
        [SerializeField] private Color crosshairNormalColor = new Color(0.2f, 0.9f, 1.0f, 1.0f);
        [SerializeField] private Color crosshairHoverColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);

        private Texture2D crosshairTexture;
        private Texture2D crosshairHoverTexture;
        private ITransmutable currentHovered;
        private Camera targetCamera;

        public event Action<ITransmutable> OnObjectTransmuted;

        public void SetDarknessOrbit(Level7DarknessOrbit orbit) => darknessOrbit = orbit;

        private void Awake()
        {
            GenerateCrosshairTextures();
            if (placementInput == null) placementInput = new NewInputSystemPlacementReader();
        }

        private void OnEnable()
        {
            if (placementInput == null) placementInput = new NewInputSystemPlacementReader();
            targetCamera = Camera.main;
            SetCrosshairCursor(false);
        }

        private void OnDisable()
        {
            ResetCursor();
            if (darknessOrbit != null)
            {
                darknessOrbit.EndSurge();
            }
        }

        private void Update()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            Vector2 mouseScreenPos = GetMouseScreenPosition();
            Vector3 mouseWorldPos = targetCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
            Vector2 mouseWorld2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            // Raycast / overlap query for ITransmutable objects
            ITransmutable hitTransmutable = QueryTransmutableAt(mouseWorld2D);

            // Hover state feedback
            if (hitTransmutable != currentHovered)
            {
                currentHovered = hitTransmutable;
                SetCrosshairCursor(currentHovered != null);
            }

            // Left click detection
            if (IsLeftClickDown())
            {
                var target = currentHovered ?? QueryTransmutableAt(mouseWorld2D);
                if (target != null)
                {
                    ExecuteTransmutation(target);
                }
            }
        }

        public void ExecuteTransmutation(ITransmutable transmutable)
        {
            if (transmutable == null) return;

            transmutable.Invert(invertDuration);
            OnObjectTransmuted?.Invoke(transmutable);

            if (darknessOrbit == null)
            {
                darknessOrbit = FindFirstObjectByType<Level7DarknessOrbit>();
            }

            if (darknessOrbit != null)
            {
                darknessOrbit.TriggerSurge(darknessSurgeDuration, slowMotionScale);
            }

            // Audio & feedback juice
            ProceduralAudio.Instance?.PlayDoubleJump();
        }

        private ITransmutable QueryTransmutableAt(Vector2 point)
        {
            // 1. Point overlap query (includes triggers and solid colliders)
            Collider2D[] colliders = Physics2D.OverlapPointAll(point, interactableLayers);
            foreach (var col in colliders)
            {
                var trans = col.GetComponent<ITransmutable>()
                    ?? col.GetComponentInParent<ITransmutable>()
                    ?? col.GetComponentInChildren<ITransmutable>();
                if (trans != null) return trans;
            }

            // 2. Small circle overlap query for forgiving click radius
            Collider2D[] circleHits = Physics2D.OverlapCircleAll(point, 0.75f, interactableLayers);
            foreach (var col in circleHits)
            {
                var trans = col.GetComponent<ITransmutable>()
                    ?? col.GetComponentInParent<ITransmutable>()
                    ?? col.GetComponentInChildren<ITransmutable>();
                if (trans != null) return trans;
            }

            return null;
        }

        private IPlacementInput placementInput;

        public void Initialize(IPlacementInput input = null, Camera cam = null)
        {
            placementInput = input ?? new NewInputSystemPlacementReader();
            if (cam != null) targetCamera = cam;
        }

        private Vector2 GetMouseScreenPosition()
        {
            if (placementInput != null)
            {
                return placementInput.GetCursorScreenPosition();
            }
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.isInProgress)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif
            return Vector2.zero;
        }

        private bool IsLeftClickDown()
        {
            if (placementInput != null)
            {
                return placementInput.IsPointerDown();
            }
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
#endif
            return false;
        }

        private void SetCrosshairCursor(bool isHovering)
        {
            if (!useHardwareCursor) return;

            var tex = isHovering ? crosshairHoverTexture : crosshairTexture;
            if (tex != null)
            {
                Cursor.SetCursor(tex, new Vector2(16, 16), CursorMode.Auto);
            }
        }

        private void ResetCursor()
        {
            if (useHardwareCursor)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private void GenerateCrosshairTextures()
        {
            const int size = 32;
            crosshairTexture = CreateCrosshairTex(size, crosshairNormalColor);
            crosshairHoverTexture = CreateCrosshairTex(size, crosshairHoverColor);
        }

        private Texture2D CreateCrosshairTex(int size, Color reticleColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Crosshair_Cursor_Procedural";
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            int center = size / 2; // 16

            // Draw center dot
            pixels[center * size + center] = reticleColor;
            pixels[(center - 1) * size + center] = reticleColor;
            pixels[center * size + (center - 1)] = reticleColor;
            pixels[(center - 1) * size + (center - 1)] = reticleColor;

            // Reticle crosshairs (top, bottom, left, right)
            for (int d = 4; d <= 12; d++)
            {
                // Vertical lines
                pixels[(center + d) * size + center] = reticleColor;
                pixels[(center - d - 1) * size + center] = reticleColor;

                // Horizontal lines
                pixels[center * size + (center + d)] = reticleColor;
                pixels[center * size + (center - d - 1)] = reticleColor;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            ResetCursor();
            if (crosshairTexture != null) Destroy(crosshairTexture);
            if (crosshairHoverTexture != null) Destroy(crosshairHoverTexture);
        }
    }
}
