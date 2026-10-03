using UnityEngine;
using Project.Player;

namespace Project.Core
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class KillZone : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled) return;
            var player = other.GetComponent<PlayerMaterialController>();
            if (player != null)
            {
                RespawnManager.Instance?.RespawnPlayer();
            }
        }
    }
}
