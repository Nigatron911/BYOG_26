using UnityEngine;

namespace Project.Level2
{
    public class Level2ObjectiveTrigger : MonoBehaviour
    {
        [SerializeField] private string objectiveText = "OBJECTIVE";
        [SerializeField] private Level2UIController uiController;
        private bool triggered = false;

        private void Awake()
        {
            if (uiController == null) uiController = FindFirstObjectByType<Level2UIController>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            if (other.CompareTag("Player"))
            {
                triggered = true;
                if (uiController == null) uiController = FindFirstObjectByType<Level2UIController>();
                if (uiController != null)
                {
                    uiController.SetObjective(objectiveText);
                }
            }
        }
    }
}
