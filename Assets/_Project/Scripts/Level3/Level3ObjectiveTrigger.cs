using UnityEngine;
using Project.Player;

namespace Project.Level3
{
    [RequireComponent(typeof(Collider2D))]
    public class Level3ObjectiveTrigger : MonoBehaviour
    {
        [SerializeField] private string objectiveMessage = "PROCEED TO NEXT SECTION";
        private bool triggered = false;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            if (other.CompareTag("Player") || other.GetComponent<PlayerMaterialController>() != null)
            {
                triggered = true;
                var ui = FindFirstObjectByType<Level3UIController>();
                if (ui != null)
                {
                    ui.SetObjective(objectiveMessage);
                }
            }
        }
    }
}
