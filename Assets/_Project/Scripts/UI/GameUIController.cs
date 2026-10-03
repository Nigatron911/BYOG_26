using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Core;
using Project.Managers;
using Project.Player;

namespace Project.UI
{
    public class GameUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMaterialController player;
        [SerializeField] private RespawnManager respawnManager;
        [SerializeField] private ObjectivesManager objectivesManager;

        [Header("HUD Text Elements")]
        [SerializeField] private Text materialText;
        [SerializeField] private Text transformationsText;
        [SerializeField] private Text keyPaperText;
        [SerializeField] private Text keyStoneText;
        [SerializeField] private Text keyRubberText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text notificationText;

        [Header("Victory Screen Elements")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Text victoryStatsText;

        private Coroutine notificationRoutine;

        private void Awake()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMaterialController>();
            if (respawnManager == null) respawnManager = FindFirstObjectByType<RespawnManager>();
            if (objectivesManager == null) objectivesManager = FindFirstObjectByType<ObjectivesManager>();

            if (materialText == null) materialText = transform.Find("HUDCard/MaterialText")?.GetComponent<Text>();
            if (transformationsText == null) transformationsText = transform.Find("HUDCard/TransText")?.GetComponent<Text>();
            if (keyPaperText == null) keyPaperText = transform.Find("HUDCard/Key1")?.GetComponent<Text>();
            if (keyStoneText == null) keyStoneText = transform.Find("HUDCard/Key2")?.GetComponent<Text>();
            if (keyRubberText == null) keyRubberText = transform.Find("HUDCard/Key3")?.GetComponent<Text>();
            if (objectiveText == null) objectiveText = transform.Find("ObjectiveCard/ObjectiveText")?.GetComponent<Text>();
            if (notificationText == null) notificationText = transform.Find("NotificationCard/NotificationText")?.GetComponent<Text>();
            if (victoryPanel == null) victoryPanel = transform.Find("VictoryPanel")?.gameObject;
            if (victoryTitleText == null) victoryTitleText = transform.Find("VictoryPanel/VictoryTitle")?.GetComponent<Text>();
            if (victoryStatsText == null) victoryStatsText = transform.Find("VictoryPanel/VictoryStats")?.GetComponent<Text>();

            // Ensure crisp font rendering in URP
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                if (t != null && t.font != null)
                {
                    t.material = t.font.material;
                }
            }

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (notificationText != null) notificationText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.OnMaterialChanged += HandleMaterialChanged;
                player.OnPlayerNotice += ShowNotification;
            }

            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice += ShowNotification;
            }

            if (objectivesManager != null)
            {
                objectivesManager.OnObjectiveChanged += HandleObjectiveChanged;
            }

            FinishTrigger.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.OnMaterialChanged -= HandleMaterialChanged;
                player.OnPlayerNotice -= ShowNotification;
            }

            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice -= ShowNotification;
            }

            if (objectivesManager != null)
            {
                objectivesManager.OnObjectiveChanged -= HandleObjectiveChanged;
            }

            FinishTrigger.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMaterialController>();
            if (player != null)
            {
                player.OnMaterialChanged -= HandleMaterialChanged;
                player.OnMaterialChanged += HandleMaterialChanged;
                player.OnPlayerNotice -= ShowNotification;
                player.OnPlayerNotice += ShowNotification;
                HandleMaterialChanged(player.CurrentMaterial, player.RemainingTransformations, player.MaxTransformations);
            }

            if (objectivesManager == null) objectivesManager = FindFirstObjectByType<ObjectivesManager>();
            if (objectivesManager != null)
            {
                objectivesManager.OnObjectiveChanged -= HandleObjectiveChanged;
                objectivesManager.OnObjectiveChanged += HandleObjectiveChanged;
                HandleObjectiveChanged(ObjectivesManager.GetObjectiveText(objectivesManager.CurrentStep));
            }

            if (respawnManager == null) respawnManager = FindFirstObjectByType<RespawnManager>();
            if (respawnManager != null)
            {
                respawnManager.OnRespawnNotice -= ShowNotification;
                respawnManager.OnRespawnNotice += ShowNotification;
            }
        }

        public void HandleMaterialChanged(MaterialType mat, int remaining, int max)
        {
            // Update Material Text & Colors
            if (materialText != null)
            {
                switch (mat)
                {
                    case MaterialType.Paper:
                        materialText.text = "MATERIAL: <color=#FFFFFF>PAPER</color>";
                        break;
                    case MaterialType.Stone:
                        materialText.text = "MATERIAL: <color=#94A3B8>STONE</color>";
                        break;
                    case MaterialType.Rubber:
                        materialText.text = "MATERIAL: <color=#00E5FF>RUBBER</color>";
                        break;
                }
            }

            // Update Transformations Count
            if (transformationsText != null)
            {
                if (remaining > 0)
                {
                    transformationsText.text = $"TRANSFORMATIONS: <color=#38BDF8>{remaining}/{max}</color>";
                }
                else
                {
                    transformationsText.text = $"TRANSFORMATIONS: <color=#EF4444>0/{max}</color> <size=14><color=#F87171>(EXHAUSTED)</color></size>";
                }
            }

            // Update Key list highlights
            UpdateKeyHighlight(keyPaperText, "1 PAPER", mat == MaterialType.Paper, remaining);
            UpdateKeyHighlight(keyStoneText, "2 STONE", mat == MaterialType.Stone, remaining);
            UpdateKeyHighlight(keyRubberText, "3 RUBBER", mat == MaterialType.Rubber, remaining);
        }

        private void UpdateKeyHighlight(Text textComp, string label, bool isActive, int remaining)
        {
            if (textComp == null) return;

            if (isActive)
            {
                textComp.text = $"<color=#FDE047>▶ [ {label} ] ◀</color>";
            }
            else
            {
                string col = remaining > 0 ? "#CBD5E1" : "#64748B";
                textComp.text = $"<color={col}>   {label}</color>";
            }
        }

        public void HandleObjectiveChanged(string obj)
        {
            if (objectiveText != null)
            {
                objectiveText.text = $"OBJECTIVE:\n<color=#FDE047>{obj}</color>";
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
            if (notificationText.transform.parent != null)
            {
                notificationText.transform.parent.gameObject.SetActive(true);
            }
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);

            // Pop animation
            notificationText.transform.localScale = Vector3.one * 1.25f;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                notificationText.transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, t / 0.2f);
                yield return null;
            }
            notificationText.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(1.8f);

            notificationText.gameObject.SetActive(false);
            if (notificationText.transform.parent != null)
            {
                notificationText.transform.parent.gameObject.SetActive(false);
            }
            notificationRoutine = null;
        }

        public void HandleLevelCompleted(int remainingTransformations)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (victoryTitleText != null)
            {
                victoryTitleText.text = "★ LEVEL COMPLETE ★";
            }

            if (victoryStatsText != null)
            {
                victoryStatsText.text = $"TRANSFORMATIONS REMAINING: {remainingTransformations}\n\n<size=18><color=#94A3B8>Press [R] to Play Again</color></size>";
            }
        }
    }
}
