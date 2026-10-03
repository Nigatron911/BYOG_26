using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Project.Core;

namespace Project.Level3
{
    public class Level3UIController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PaperResourceController resourceController;
        [SerializeField] private DrawingCanvasController canvasController;
        [SerializeField] private RespawnManager respawnManager;

        [Header("Top Header & Objective")]
        [SerializeField] private Text levelTitleText;
        [SerializeField] private Text objectiveText;

        [Header("Resource & Status HUD")]
        [SerializeField] private Text paperScrapsText;
        [SerializeField] private Text materialText;
        [SerializeField] private Text drawingStatusText;
        [SerializeField] private Text creationStabilityText;

        [Header("Center Notification Toast")]
        [SerializeField] private GameObject notificationCard;
        [SerializeField] private Text notificationText;

        [Header("Victory Screen")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Text victorySubtitleText;

        private Coroutine notificationRoutine;

        private void Awake()
        {
            if (resourceController == null) resourceController = FindFirstObjectByType<PaperResourceController>();
            if (canvasController == null) canvasController = FindFirstObjectByType<DrawingCanvasController>();
            if (respawnManager == null) respawnManager = FindFirstObjectByType<RespawnManager>();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (notificationCard != null) notificationCard.SetActive(false);
            if (creationStabilityText != null) creationStabilityText.text = "";

            // Crisp font rendering in URP
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                if (t != null && t.font != null) t.material = t.font.material;
            }
        }

        private void OnEnable()
        {
            if (resourceController != null)
            {
                resourceController.OnScrapsChanged += HandleScrapsChanged;
                resourceController.OnResourceNotice += ShowNotification;
            }

            if (canvasController != null)
            {
                canvasController.OnCanvasToggled += HandleCanvasToggled;
                canvasController.OnMaterialChanged += HandleMaterialChanged;
            }

            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice += ShowNotification;
            }

            TemporaryCreation.OnCreationSpawned += HandleCreationSpawned;
            TemporaryCreation.OnCreationTick += HandleCreationTick;
            TemporaryCreation.OnCreationWarning += HandleCreationWarning;
            TemporaryCreation.OnCreationDestroyed += HandleCreationDestroyed;

            FinishTrigger.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            if (resourceController != null)
            {
                resourceController.OnScrapsChanged -= HandleScrapsChanged;
                resourceController.OnResourceNotice -= ShowNotification;
            }

            if (canvasController != null)
            {
                canvasController.OnCanvasToggled -= HandleCanvasToggled;
                canvasController.OnMaterialChanged -= HandleMaterialChanged;
            }

            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice -= ShowNotification;
            }

            TemporaryCreation.OnCreationSpawned -= HandleCreationSpawned;
            TemporaryCreation.OnCreationTick -= HandleCreationTick;
            TemporaryCreation.OnCreationWarning -= HandleCreationWarning;
            TemporaryCreation.OnCreationDestroyed -= HandleCreationDestroyed;

            FinishTrigger.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void Start()
        {
            if (resourceController != null)
            {
                HandleScrapsChanged(resourceController.CurrentScraps, resourceController.TotalScraps);
            }
            if (canvasController != null)
            {
                HandleMaterialChanged(canvasController.CurrentMaterial);
            }
            HandleCanvasToggled(false);
            SetObjective("COLLECT PAPER SCRAPS");
        }

        public void SetObjective(string obj)
        {
            if (objectiveText != null)
            {
                objectiveText.text = $"OBJECTIVE:\n<color=#FDE047>{obj}</color>";
            }
        }

        private void HandleScrapsChanged(int current, int total)
        {
            if (paperScrapsText != null)
            {
                paperScrapsText.text = $"PAPER SCRAPS: <color=#00E5FF><b>{current} / {total}</b></color>";
            }
        }

        private void HandleMaterialChanged(CreationMaterial mat)
        {
            if (materialText != null)
            {
                string col = mat == CreationMaterial.Wood ? "#D4A373" : (mat == CreationMaterial.Rubber ? "#00D2FF" : "#94A3B8");
                materialText.text = $"MATERIAL: <color={col}><b>{mat.ToString().ToUpper()}</b></color>";
            }
        }

        private void HandleCanvasToggled(bool isOpen)
        {
            if (drawingStatusText != null)
            {
                if (isOpen)
                {
                    drawingStatusText.text = "DRAWING: <color=#FBBF24>CANVAS OPEN</color>";
                }
                else
                {
                    drawingStatusText.text = "DRAWING: <color=#4ADE80>READY [TAB / C]</color>";
                }
            }
        }

        private void HandleCreationSpawned(TemporaryCreation creation, float duration)
        {
            if (creationStabilityText != null)
            {
                creationStabilityText.text = $"CREATION STABILITY: <color=#00E5FF>{duration:F1}s</color>";
            }
            ShowNotification($"CREATED {creation.MaterialType.ToString().ToUpper()} OBJECT!");
        }

        private void HandleCreationTick(float remaining)
        {
            if (creationStabilityText != null)
            {
                if (remaining > 2.5f)
                {
                    creationStabilityText.text = $"CREATION STABILITY: <color=#00E5FF>{remaining:F1}s</color>";
                }
                else if (remaining > 0.05f)
                {
                    creationStabilityText.text = $"<color=#F87171><b>WARNING: CREATION FADING! ({remaining:F1}s)</b></color>";
                }
                else
                {
                    creationStabilityText.text = "<color=#94A3B8>CREATION DISSOLVED</color>";
                }
            }
        }

        private void HandleCreationWarning()
        {
            ShowNotification("WARNING: CREATION FADING! EVADE NOW!");
        }

        private void HandleCreationDestroyed(TemporaryCreation creation)
        {
            if (creationStabilityText != null)
            {
                creationStabilityText.text = "<color=#94A3B8>CREATION DISSOLVED</color>";
            }
        }

        public void ShowNotification(string message)
        {
            if (notificationText == null) return;
            if (notificationRoutine != null) StopCoroutine(notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationSequence(message));
        }

        private IEnumerator NotificationSequence(string message)
        {
            if (notificationCard != null) notificationCard.SetActive(true);
            notificationText.gameObject.SetActive(true);
            notificationText.text = message;

            // Unscaled time animation so toasts show during pause
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.unscaledDeltaTime;
                notificationText.transform.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, t / 0.15f);
                yield return null;
            }
            notificationText.transform.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(2.0f);

            if (notificationCard != null) notificationCard.SetActive(false);
            notificationText.gameObject.SetActive(false);
            notificationRoutine = null;
        }

        private void HandleLevelCompleted(int unused)
        {
            SetObjective("LEVEL 3 COMPLETE!");
            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (victoryTitleText != null) victoryTitleText.text = "★ BLANK CANVAS COMPLETE ★";
            if (victorySubtitleText != null) victorySubtitleText.text = "YOUR CREATION SURVIVED\n\n<size=18><color=#94A3B8>Press [ENTER] to Continue  •  [R] to Restart</color></size>";
        }
    }
}
