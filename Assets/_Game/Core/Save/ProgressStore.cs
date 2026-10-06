using UnityEngine;

namespace Game.Core.Save
{
    /// <summary>
    /// Persists player progress and audio preferences via PlayerPrefs.
    /// Plain C# service created by the composition root (no static state, no singleton).
    /// </summary>
    public class ProgressStore
    {
        private const string HighestLevelKey = "byog26.highestLevelIndex";
        private const string MusicVolumeKey = "byog26.musicVolume";
        private const string SfxVolumeKey = "byog26.sfxVolume";

        public int HighestUnlockedLevelIndex => Mathf.Max(0, PlayerPrefs.GetInt(HighestLevelKey, 0));
        public bool HasProgress => HighestUnlockedLevelIndex > 0;

        public float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
            set { PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.9f);
            set { PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public void MarkLevelReached(int levelIndex)
        {
            if (levelIndex <= HighestUnlockedLevelIndex) return;
            PlayerPrefs.SetInt(HighestLevelKey, levelIndex);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(HighestLevelKey);
            PlayerPrefs.Save();
        }
    }
}
