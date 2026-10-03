using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Project.Core;

namespace Project.Level2
{
    public class Level2UIController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private RealityRewriteController rewriteController;
        [SerializeField] private RespawnManager respawnManager;

        [Header("Top Header & Objective")]
        [SerializeField] private Text levelTitleText;
        [SerializeField] private Text objectiveText;

        [Header("Ability & Cooldown HUD")]
        [SerializeField] private Text cooldownText;
        [SerializeField] private Text rewriteActiveText;

        [Header("Center Reticle UI")]
        [SerializeField] private RectTransform reticleContainer;
        [SerializeField] private Image reticleCrosshair;
        [SerializeField] private Image reticleBracketLeft;
        [SerializeField] private Image reticleBracketRight;
        [SerializeField] private Text targetPromptText;

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
            if (rewriteController == null) rewriteController = FindFirstObjectByType<RealityRewriteController>();
            if (respawnManager == null) respawnManager = FindFirstObjectByType<RespawnManager>();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (notificationCard != null) notificationCard.SetActive(false);
            if (rewriteActiveText != null) rewriteActiveText.text = "";

            // Crisp font rendering in URP
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                if (t != null && t.font != null) t.material = t.font.material;
            }
        }

        private void OnEnable()
        {
            if (rewriteController != null)
            {
                rewriteController.OnTargetStatusChanged += HandleTargetStatusChanged;
                rewriteController.OnRewriteFired += HandleRewriteFired;
                rewriteController.OnRewriteTick += HandleRewriteTick;
                rewriteController.OnRewriteRestored += HandleRewriteRestored;
                rewriteController.OnCooldownTick += HandleCooldownTick;
                rewriteController.OnCooldownReady += HandleCooldownReady;
                rewriteController.OnPlayerNotice += ShowNotification;
            }

            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice += ShowNotification;
            }

            FinishTrigger.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            if (rewriteController != null)
            {
                rewriteController.OnTargetStatusChanged -= HandleTargetStatusChanged;
                rewriteController.OnRewriteFired -= HandleRewriteFired;
                rewriteController.OnRewriteTick -= HandleRewriteTick;
                rewriteController.OnRewriteRestored -= HandleRewriteRestored;
                rewriteController.OnCooldownTick -= HandleCooldownTick;
                rewriteController.OnCooldownReady -= HandleCooldownReady;
                rewriteController.OnPlayerNotice -= ShowNotification;
            }

            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice -= ShowNotification;
            }

            FinishTrigger.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void Start()
        {
            HandleCooldownReady();
            SetObjective("LEARN TO REWRITE REALITY");
        }

        public void SetObjective(string obj)
        {
            if (objectiveText != null)
            {
                objectiveText.text = $"OBJECTIVE:\n<color=#FDE047>{obj}</color>";
            }
        }

        private void HandleTargetStatusChanged(bool isValid, string prompt, RealityRewriteTarget target)
        {
            if (targetPromptText != null)
            {
                if (isValid)
                {
                    targetPromptText.text = $"<color=#00FFCC><b>[LMB] {prompt}</b></color>\n<size=12><color=#E2E8F0>Target: {target?.TargetName}</color></size>";
                }
                else
                {
                    targetPromptText.text = "<color=#94A3B8>TARGET INVALID</color>";
                }
            }

            // Reticle colors and brackets
            Color col = isValid ? new Color(0.0f, 1.0f, 0.8f, 1f) : new Color(0.85f, 0.35f, 0.35f, 0.7f);
            if (reticleCrosshair != null) reticleCrosshair.color = col;
            if (reticleBracketLeft != null) reticleBracketLeft.color = col;
            if (reticleBracketRight != null) reticleBracketRight.color = col;

            if (reticleContainer != null)
            {
                float targetScale = isValid ? 1.25f : 1.0f;
                reticleContainer.localScale = Vector3.Lerp(reticleContainer.localScale, Vector3.one * targetScale, Time.deltaTime * 15f);
            }
        }

        private void HandleRewriteFired(RealityRewriteTarget target, float duration)
        {
            if (rewriteActiveText != null)
            {
                rewriteActiveText.text = $"REALITY REWRITE: <color=#00E5FF>{duration:F1}s</color>";
            }
            ShowNotification($"REWRITING: {target.TargetName}!");
        }

        private void HandleRewriteTick(float remaining)
        {
            if (rewriteActiveText != null)
            {
                if (remaining > 0.05f)
                {
                    rewriteActiveText.text = $"REALITY REWRITE: <color=#00E5FF>{remaining:F1}s</color>";
                }
                else
                {
                    rewriteActiveText.text = "<color=#FBBF24>REALITY RESTORING...</color>";
                }
            }
        }

        private void HandleRewriteRestored()
        {
            if (rewriteActiveText != null)
            {
                rewriteActiveText.text = "<color=#94A3B8>REALITY RESTORED</color>";
            }
            ShowNotification("REALITY RESTORED");
        }

        private void HandleCooldownTick(float remaining, float max)
        {
            if (cooldownText != null)
            {
                cooldownText.text = $"REWRITE COOLDOWN: <color=#F87171>{remaining:F1}s</color>";
            }
        }

        private void HandleCooldownReady()
        {
            if (cooldownText != null)
            {
                cooldownText.text = "REWRITE: <color=#4ADE80>READY [LMB]</color>";
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

            // Pop animation
            notificationText.transform.localScale = Vector3.one * 1.2f;
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                notificationText.transform.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, t / 0.15f);
                yield return null;
            }
            notificationText.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(1.8f);

            if (notificationCard != null) notificationCard.SetActive(false);
            notificationText.gameObject.SetActive(false);
            notificationRoutine = null;
        }

        private void HandleLevelCompleted(int unused)
        {
            SetObjective("LEVEL 2 COMPLETE!");
            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (victoryTitleText != null) victoryTitleText.text = "★ REALITY REWRITE COMPLETE ★";
            if (victorySubtitleText != null) victorySubtitleText.text = "LEVEL 2 COMPLETE\n\n<size=18><color=#94A3B8>Press [ENTER] to Continue  •  [R] to Restart</color></size>";
        }
    }
}
