using System;
using UnityEngine;

namespace Project.Level3
{
    public class PaperResourceController : MonoBehaviour
    {
        [Header("Paper Scrap Resources")]
        [SerializeField] private int startingScraps = 0;
        [SerializeField] private int totalScrapsInLevel = 6;

        private int currentScraps = 0;
        private int checkpointScraps = 0;

        public int CurrentScraps => currentScraps;
        public int TotalScraps => totalScrapsInLevel;
        public bool CanDraw => currentScraps > 0;

        public event Action<int, int> OnScrapsChanged;
        public event Action<string> OnResourceNotice;

        private void Awake()
        {
            currentScraps = startingScraps;
            checkpointScraps = startingScraps;
        }

        private void Start()
        {
            // Auto count scraps in level if not manually assigned
            var scraps = FindObjectsByType<PaperScrap>(FindObjectsSortMode.None);
            if (scraps != null && scraps.Length > 0)
            {
                totalScrapsInLevel = scraps.Length;
            }

            NotifyChange();
        }

        public void AddScraps(int amount)
        {
            currentScraps += amount;
            NotifyChange();
            OnResourceNotice?.Invoke($"COLLECTED PAPER SCRAP! ({currentScraps}/{totalScrapsInLevel})");
        }

        public bool TryConsumeScrap()
        {
            if (currentScraps <= 0)
            {
                OnResourceNotice?.Invoke("NO PAPER SCRAPS AVAILABLE!");
                return false;
            }

            currentScraps--;
            NotifyChange();
            return true;
        }

        public void SaveCheckpointState()
        {
            checkpointScraps = currentScraps;
        }

        public void RestoreCheckpointState()
        {
            currentScraps = checkpointScraps;
            NotifyChange();
        }

        private void NotifyChange()
        {
            OnScrapsChanged?.Invoke(currentScraps, totalScrapsInLevel);
        }
    }
}
