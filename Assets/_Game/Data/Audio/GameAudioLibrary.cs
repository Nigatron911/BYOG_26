using UnityEngine;

namespace Game.Data.Audio
{
    /// <summary>
    /// Read-only audio configuration: music per level group and gameplay sound effects.
    /// Pure data (Section 8) - consumed by the presentation-layer audio controller.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "Game/Audio Library")]
    public class GameAudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class MusicTrack
        {
            [Tooltip("First level number (1-based, inclusive) that uses this track.")]
            public int fromLevel = 1;
            [Tooltip("Last level number (1-based, inclusive) that uses this track.")]
            public int toLevel = 1;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.6f;
        }

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private MusicTrack[] levelMusic = new MusicTrack[0];
        [SerializeField, Min(0f)] private float musicCrossfadeSeconds = 1.2f;

        [Header("Player")]
        [SerializeField] private AudioClip jump;
        [SerializeField] private AudioClip land;
        [SerializeField] private AudioClip runningLoop;
        [SerializeField] private AudioClip hurt;

        [Header("World")]
        [SerializeField] private AudioClip door;
        [SerializeField] private AudioClip gravityShift;
        [SerializeField] private AudioClip gravityStabilize;
        [SerializeField] private AudioClip bounce;
        [SerializeField] private AudioClip stoneImpact;
        [SerializeField] private AudioClip rewriteShot;
        [SerializeField] private AudioClip rewriteExpire;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float runningLoopVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float footstepPitchVariance = 0.08f;

        public AudioClip MenuMusic => menuMusic;
        public float MusicCrossfadeSeconds => musicCrossfadeSeconds;
        public AudioClip Jump => jump;
        public AudioClip Land => land;
        public AudioClip RunningLoop => runningLoop;
        public AudioClip Hurt => hurt;
        public AudioClip Door => door;
        public AudioClip GravityShift => gravityShift;
        public AudioClip GravityStabilize => gravityStabilize;
        public AudioClip Bounce => bounce;
        public AudioClip StoneImpact => stoneImpact;
        public AudioClip RewriteShot => rewriteShot;
        public AudioClip RewriteExpire => rewriteExpire;
        public float RunningLoopVolume => runningLoopVolume;
        public float PitchVariance => footstepPitchVariance;

        public MusicTrack GetTrackForLevel(int levelNumber)
        {
            if (levelMusic == null) return null;
            for (int i = 0; i < levelMusic.Length; i++)
            {
                var t = levelMusic[i];
                if (t != null && t.clip != null && levelNumber >= t.fromLevel && levelNumber <= t.toLevel) return t;
            }
            return null;
        }
    }
}
