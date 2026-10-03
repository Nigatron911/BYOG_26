using System;
using UnityEngine;
using Project.Player;

namespace Project.Core
{
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [SerializeField] private PlayerMaterialController player;
        [SerializeField] private Transform initialSpawnPoint;

        private Vector3 currentRespawnPoint;
        private int currentCheckpointIndex = -1;

        public event Action<string> OnRespawnNotice;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (player == null) player = FindFirstObjectByType<PlayerMaterialController>();

            currentRespawnPoint = initialSpawnPoint != null ? initialSpawnPoint.position : (player != null ? player.transform.position : Vector3.zero);
        }

        private void OnEnable()
        {
            Checkpoint.OnCheckpointActivated += HandleCheckpointActivated;
        }

        private void OnDisable()
        {
            Checkpoint.OnCheckpointActivated -= HandleCheckpointActivated;
        }

        private void HandleCheckpointActivated(Checkpoint cp)
        {
            if (cp.CheckpointIndex > currentCheckpointIndex)
            {
                currentCheckpointIndex = cp.CheckpointIndex;
                currentRespawnPoint = cp.SpawnPosition;
                OnRespawnNotice?.Invoke("CHECKPOINT REACHED");
            }
        }

        public void RespawnPlayer()
        {
            if (player != null)
            {
                player.ResetToCheckpoint(currentRespawnPoint);
            }
        }
    }
}
