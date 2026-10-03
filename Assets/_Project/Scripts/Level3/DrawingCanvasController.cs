using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Project.Audio;

namespace Project.Level3
{
    public class DrawingCanvasController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject canvasRootPanel;
        [SerializeField] private RawImage drawingSurface;
        [SerializeField] private RectTransform drawingRectTransform;

        [Header("HUD Info inside Canvas")]
        [SerializeField] private Text materialInfoText;
        [SerializeField] private Text paperScrapCountText;
        [SerializeField] private Text validationNoticeText;
        [SerializeField] private Image materialIndicatorColor;

        [Header("Buttons")]
        [SerializeField] private Button btnWood;
        [SerializeField] private Button btnRubber;
        [SerializeField] private Button btnAnvil;
        [SerializeField] private Button btnCreate;
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnClear;

        [Header("Drawing Surface Settings")]
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 320;
        [SerializeField] private int brushRadius = 5;

        private Texture2D drawingTexture;
        private Color[] clearPixels;
        private List<Vector2> strokePoints = new List<Vector2>();
        private CreationMaterial currentMaterial = CreationMaterial.Wood;
        private bool isCanvasOpen = false;
        private bool isDrawing = false;
        private Vector2 lastDrawPos = Vector2.negativeInfinity;
        private float savedTimeScale = 1.0f;

        public bool IsCanvasOpen => isCanvasOpen;
        public CreationMaterial CurrentMaterial => currentMaterial;

        public event Action<bool> OnCanvasToggled;
        public event Action<CreationMaterial> OnMaterialChanged;

        private void Awake()
        {
            InitializeTexture();

            if (btnWood != null) btnWood.onClick.AddListener(() => SetMaterial(CreationMaterial.Wood));
            if (btnRubber != null) btnRubber.onClick.AddListener(() => SetMaterial(CreationMaterial.Rubber));
            if (btnAnvil != null) btnAnvil.onClick.AddListener(() => SetMaterial(CreationMaterial.Anvil));
            if (btnCreate != null) btnCreate.onClick.AddListener(ConfirmCreate);
            if (btnCancel != null) btnCancel.onClick.AddListener(CloseCanvas);
            if (btnClear != null) btnClear.onClick.AddListener(ClearDrawing);

            if (canvasRootPanel != null) canvasRootPanel.SetActive(false);
            SetMaterial(CreationMaterial.Wood);
        }

        private void InitializeTexture()
        {
            drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            drawingTexture.filterMode = FilterMode.Point;
            drawingTexture.wrapMode = TextureWrapMode.Clamp;

            clearPixels = new Color[textureWidth * textureHeight];
            Color bgColor = new Color(0.06f, 0.09f, 0.15f, 0.95f);
            Color gridColor = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    bool isGrid = (x % 32 == 0) || (y % 32 == 0);
                    clearPixels[y * textureWidth + x] = isGrid ? gridColor : bgColor;
                }
            }

            ClearDrawing();

            if (drawingSurface != null)
            {
                drawingSurface.texture = drawingTexture;
            }
        }

        private void Update()
        {
            HandleGlobalToggle();

            if (isCanvasOpen)
            {
                HandleCanvasInput();
                HandleDrawing();
            }
        }

        private void HandleGlobalToggle()
        {
            bool togglePressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) togglePressed = true;
                if (isCanvasOpen && kb.escapeKey.wasPressedThisFrame) { CloseCanvas(); return; }
                if (isCanvasOpen && kb.enterKey.wasPressedThisFrame) { ConfirmCreate(); return; }
                if (isCanvasOpen && kb.digit1Key.wasPressedThisFrame) SetMaterial(CreationMaterial.Wood);
                if (isCanvasOpen && kb.digit2Key.wasPressedThisFrame) SetMaterial(CreationMaterial.Rubber);
                if (isCanvasOpen && kb.digit3Key.wasPressedThisFrame) SetMaterial(CreationMaterial.Anvil);
            }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.C)) togglePressed = true;
                if (isCanvasOpen && Input.GetKeyDown(KeyCode.Escape)) { CloseCanvas(); return; }
                if (isCanvasOpen && Input.GetKeyDown(KeyCode.Return)) { ConfirmCreate(); return; }
                if (isCanvasOpen && Input.GetKeyDown(KeyCode.Alpha1)) SetMaterial(CreationMaterial.Wood);
                if (isCanvasOpen && Input.GetKeyDown(KeyCode.Alpha2)) SetMaterial(CreationMaterial.Rubber);
                if (isCanvasOpen && Input.GetKeyDown(KeyCode.Alpha3)) SetMaterial(CreationMaterial.Anvil);
            }
            catch {}
#endif

            if (togglePressed)
            {
                if (isCanvasOpen) CloseCanvas();
                else OpenCanvas();
            }
        }

        public void OpenCanvas()
        {
            if (isCanvasOpen) return;
            isCanvasOpen = true;

            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (canvasRootPanel != null) canvasRootPanel.SetActive(true);
            ClearDrawing();
            UpdateUI();

            ProceduralAudio.Instance?.PlayCanvasOpen();
            OnCanvasToggled?.Invoke(true);
        }

        public void CloseCanvas()
        {
            if (!isCanvasOpen) return;
            isCanvasOpen = false;

            Time.timeScale = savedTimeScale <= 0f ? 1.0f : savedTimeScale;

            if (canvasRootPanel != null) canvasRootPanel.SetActive(false);

            ProceduralAudio.Instance?.PlayCanvasClose();
            OnCanvasToggled?.Invoke(false);
        }

        public void SetMaterial(CreationMaterial mat)
        {
            currentMaterial = mat;
            UpdateUI();
            OnMaterialChanged?.Invoke(currentMaterial);
        }

        private void UpdateUI()
        {
            Color matColor = Color.white;
            string matName = "WOOD";

            switch (currentMaterial)
            {
                case CreationMaterial.Wood:
                    matColor = new Color(0.85f, 0.65f, 0.40f, 1f);
                    matName = "BROWN — WOOD (Solid Platform / Bridge)";
                    break;
                case CreationMaterial.Rubber:
                    matColor = new Color(0.0f, 0.8f, 1.0f, 1f);
                    matName = "BLUE — RUBBER (Super Bouncy Pad)";
                    break;
                case CreationMaterial.Anvil:
                    matColor = new Color(0.6f, 0.65f, 0.75f, 1f);
                    matName = "GREY — HEAVY ANVIL (Presses Switches)";
                    break;
            }

            if (materialInfoText != null)
            {
                materialInfoText.text = $"MATERIAL: <color=#{ColorUtility.ToHtmlStringRGB(matColor)}><b>{matName}</b></color>";
            }

            if (materialIndicatorColor != null)
            {
                materialIndicatorColor.color = matColor;
            }

            var resourceMgr = FindFirstObjectByType<PaperResourceController>();
            if (paperScrapCountText != null && resourceMgr != null)
            {
                paperScrapCountText.text = $"PAPER SCRAPS: <b>{resourceMgr.CurrentScraps} / {resourceMgr.TotalScraps}</b>";
            }

            if (validationNoticeText != null)
            {
                validationNoticeText.text = "[HOLD LMB TO DRAW]  •  [ENTER] CREATE  •  [ESC] CANCEL";
            }
        }

        private Camera GetEventCamera()
        {
            if (drawingRectTransform == null) return null;
            Canvas canvas = drawingRectTransform.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        public void ClearDrawing()
        {
            if (drawingTexture == null || clearPixels == null) return;
            drawingTexture.SetPixels(clearPixels);
            drawingTexture.Apply();
            strokePoints.Clear();
            lastDrawPos = Vector2.negativeInfinity;
            isDrawing = false;
        }

        private void HandleCanvasInput()
        {
            var resourceMgr = FindFirstObjectByType<PaperResourceController>();
            if (paperScrapCountText != null && resourceMgr != null)
            {
                paperScrapCountText.text = $"PAPER SCRAPS: <b>{resourceMgr.CurrentScraps} / {resourceMgr.TotalScraps}</b>";
            }
        }

        private void HandleDrawing()
        {
            if (drawingRectTransform == null) return;

            Vector2 mouseScreen = Vector2.zero;
            bool isMouseDown = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mouseScreen = Mouse.current.position.ReadValue();
                isMouseDown = Mouse.current.leftButton.isPressed;
            }
            else
            {
                try
                {
                    mouseScreen = Input.mousePosition;
                    isMouseDown = Input.GetMouseButton(0);
                }
                catch {}
            }
#else
            try
            {
                mouseScreen = Input.mousePosition;
                isMouseDown = Input.GetMouseButton(0);
            }
            catch {}
#endif

            if (isMouseDown)
            {
                Camera uiCam = GetEventCamera();
                Vector2 localPoint;
                bool hit = RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingRectTransform, mouseScreen, uiCam, out localPoint);
                if (!hit && uiCam != null)
                {
                    hit = RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingRectTransform, mouseScreen, null, out localPoint);
                }

                if (hit)
                {
                    Rect rect = drawingRectTransform.rect;
                    if (rect.Contains(localPoint))
                    {
                        // Convert local rect coords [-w/2..w/2, -h/2..h/2] to texture coords [0..texW, 0..texH]
                        float u = (localPoint.x - rect.xMin) / rect.width;
                        float v = (localPoint.y - rect.yMin) / rect.height;

                        int px = Mathf.Clamp(Mathf.RoundToInt(u * textureWidth), 0, textureWidth - 1);
                        int py = Mathf.Clamp(Mathf.RoundToInt(v * textureHeight), 0, textureHeight - 1);

                        Vector2 currentPos = new Vector2(px, py);

                        if (!isDrawing)
                        {
                            isDrawing = true;
                            lastDrawPos = currentPos;
                            DrawCircle(px, py, brushRadius, GetBrushColor());
                            strokePoints.Add(currentPos);
                            drawingTexture.Apply();
                        }
                        else if (currentPos != lastDrawPos)
                        {
                            DrawLine(lastDrawPos, currentPos, brushRadius, GetBrushColor());
                            lastDrawPos = currentPos;
                            strokePoints.Add(currentPos);
                            drawingTexture.Apply();
                        }
                    }
                }
            }
            else
            {
                isDrawing = false;
                lastDrawPos = Vector2.negativeInfinity;
            }
        }

        private Color GetBrushColor()
        {
            switch (currentMaterial)
            {
                case CreationMaterial.Wood: return new Color(0.85f, 0.65f, 0.40f, 1f);
                case CreationMaterial.Rubber: return new Color(0.0f, 0.85f, 1.0f, 1f);
                case CreationMaterial.Anvil: default: return new Color(0.75f, 0.80f, 0.90f, 1f);
            }
        }

        private void DrawCircle(int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int y = -r; y <= r; y++)
            {
                int py = cy + y;
                if (py < 0 || py >= textureHeight) continue;
                for (int x = -r; x <= r; x++)
                {
                    int px = cx + x;
                    if (px < 0 || px >= textureWidth) continue;
                    if (x * x + y * y <= r2)
                    {
                        drawingTexture.SetPixel(px, py, col);
                    }
                }
            }
        }

        private void DrawLine(Vector2 p1, Vector2 p2, int r, Color col)
        {
            float dist = Vector2.Distance(p1, p2);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 p = Vector2.Lerp(p1, p2, t);
                DrawCircle(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), r, col);
            }
        }

        public void ConfirmCreate()
        {
            // 1. Check scraps
            var resourceMgr = FindFirstObjectByType<PaperResourceController>();
            if (resourceMgr == null || !resourceMgr.CanDraw)
            {
                if (validationNoticeText != null)
                {
                    validationNoticeText.text = "<color=#F87171><b>NO PAPER SCRAPS! COLLECT MORE FIRST</b></color>";
                }
                ProceduralAudio.Instance?.PlayFail();
                return;
            }

            // 2. Validate drawing
            if (strokePoints.Count < 8)
            {
                if (validationNoticeText != null)
                {
                    validationNoticeText.text = "<color=#FBBF24><b>DRAW SOMETHING FIRST!</b></color>";
                }
                ProceduralAudio.Instance?.PlayFail();
                return;
            }

            // Calculate bounding box
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var pt in strokePoints)
            {
                if (pt.x < minX) minX = pt.x;
                if (pt.x > maxX) maxX = pt.x;
                if (pt.y < minY) minY = pt.y;
                if (pt.y > maxY) maxY = pt.y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            if (width < 20f && height < 20f)
            {
                if (validationNoticeText != null)
                {
                    validationNoticeText.text = "<color=#FBBF24><b>DRAW A LARGER SHAPE!</b></color>";
                }
                ProceduralAudio.Instance?.PlayFail();
                return;
            }

            // 3. Consume scrap
            resourceMgr.TryConsumeScrap();

            // 4. Close canvas and resume time
            CloseCanvas();

            // 5. Spawn physical object in world
            DrawingObjectSpawner.Instance?.SpawnCreation(currentMaterial, new Vector2(width, height), strokePoints);
        }
    }
}
