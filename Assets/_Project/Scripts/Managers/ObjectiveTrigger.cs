using UnityEngine;
using Project.Managers;
using Project.Player;

namespace Project.Managers
{
    [RequireComponent(typeof(Collider2D))]
    public class ObjectiveTrigger : MonoBehaviour
    {
        [SerializeField] private ObjectivesManager.ObjectiveStep stepToTrigger;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerMaterialController>();
            if (player != null)
            {
                ObjectivesManager.Instance?.SetStep(stepToTrigger);
            }
        }
    }
}
