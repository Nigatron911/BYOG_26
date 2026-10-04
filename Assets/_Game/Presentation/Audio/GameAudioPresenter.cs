using System.Collections;
using UnityEngine;
using Game.Core.Events;

namespace Game.Presentation.Audio
{
    /// <summary>
    /// Presentation-layer audio presenter handling all BGM and SFX across the 8 levels.
    /// Strictly adheres to GEMINI.md Section 14 (Audio is presentation listening to events)
    /// and Section 2 (No singletons). Dependencies are explicitly wired via Initialize().
    /// </summary>
    public class GameAudioPresenter : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource footstepSource;

        [Header("Background Music (Per Act)")]
        [SerializeField] private AudioClip act1Bgm; // Levels 1 & 2
        [SerializeField] private AudioClip act2Bgm; // Levels 3 & 4
        [SerializeField] private AudioClip act3Bgm; // Levels 5 & 6
        [SerializeField] private AudioClip act4Bgm; // Levels 7 & 8

        [Header("Locomotion SFX")]
        [SerializeField] private AudioClip jumpSfx;
        [SerializeField] private AudioClip landSfx;
        [SerializeField] private AudioClip runSfx;
        [SerializeField] private AudioClip hurtSfx;
        [SerializeField] private AudioClip doorSfx;

        [Header("Act Specific SFX")]
        [SerializeField] private AudioClip gravityShiftSfx;
        [SerializeField] private AudioClip gravityStabilizeSfx;
        [SerializeField] private AudioClip bounceSfx;
        [SerializeField] private AudioClip stoneFallSfx;
        [SerializeField] private AudioClip realityRewriteShotSfx;
        [SerializeField] private AudioClip realityRewriteExpireSfx;

        private GameEvents events;
        private Coroutine bgmFadeCoroutine;
        private int currentAct = -1;
        private bool isFootstepsPlaying = false;

        public void Initialize(GameEvents gameEvents)
        {
            events = gameEvents;

            EnsureAudioSources();
            LoadAudioClipsIfMissing();

            if (events != null)
            {
                events.LevelLoaded -= OnLevelLoaded;
                events.LevelLoaded += OnLevelLoaded;

                events.PlayerJumped -= OnPlayerJumped;
                events.PlayerJumped += OnPlayerJumped;

                events.PlayerLanded -= OnPlayerLanded;
                events.PlayerLanded += OnPlayerLanded;

                events.PlayerRunningChanged -= OnPlayerRunningChanged;
                events.PlayerRunningChanged += OnPlayerRunningChanged;

                events.PlayerDied -= OnPlayerDied;
                events.PlayerDied += OnPlayerDied;

                events.LevelCompleted -= OnLevelCompleted;
                events.LevelCompleted += OnLevelCompleted;

                events.GravityModeChanged -= OnGravityModeChanged;
                events.GravityModeChanged += OnGravityModeChanged;

                events.MaterialTypeChanged -= OnMaterialTypeChanged;
                events.MaterialTypeChanged += OnMaterialTypeChanged;

                events.ObjectBounced -= OnObjectBounced;
                events.ObjectBounced += OnObjectBounced;

                events.RealityRewriteFired -= OnRealityRewriteFired;
                events.RealityRewriteFired += OnRealityRewriteFired;

                events.RealityRewriteExpired -= OnRealityRewriteExpired;
                events.RealityRewriteExpired += OnRealityRewriteExpired;
            }

            if (bgmSource != null && !bgmSource.isPlaying)
            {
                OnLevelLoaded(1);
            }
        }

        private void Awake()
        {
            EnsureAudioSources();
            LoadAudioClipsIfMissing();
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.LevelLoaded -= OnLevelLoaded;
                events.PlayerJumped -= OnPlayerJumped;
                events.PlayerLanded -= OnPlayerLanded;
                events.PlayerRunningChanged -= OnPlayerRunningChanged;
                events.PlayerDied -= OnPlayerDied;
                events.LevelCompleted -= OnLevelCompleted;
                events.GravityModeChanged -= OnGravityModeChanged;
                events.MaterialTypeChanged -= OnMaterialTypeChanged;
                events.ObjectBounced -= OnObjectBounced;
                events.RealityRewriteFired -= OnRealityRewriteFired;
                events.RealityRewriteExpired -= OnRealityRewriteExpired;
            }
        }

        public void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                var bgmObj = transform.Find("BGM_Source");
                if (bgmObj == null)
                {
                    bgmObj = new GameObject("BGM_Source").transform;
                    bgmObj.SetParent(transform, false);
                }
                bgmSource = bgmObj.GetComponent<AudioSource>();
                if (bgmSource == null) bgmSource = bgmObj.gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f;
                bgmSource.volume = 0.55f;
            }

            if (sfxSource == null)
            {
                var sfxObj = transform.Find("SFX_Source");
                if (sfxObj == null)
                {
                    sfxObj = new GameObject("SFX_Source").transform;
                    sfxObj.SetParent(transform, false);
                }
                sfxSource = sfxObj.GetComponent<AudioSource>();
                if (sfxSource == null) sfxSource = sfxObj.gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.volume = 0.75f;
            }

            if (footstepSource == null)
            {
                var footObj = transform.Find("Footstep_Source");
                if (footObj == null)
                {
                    footObj = new GameObject("Footstep_Source").transform;
                    footObj.SetParent(transform, false);
                }
                footstepSource = footObj.GetComponent<AudioSource>();
                if (footstepSource == null) footstepSource = footObj.gameObject.AddComponent<AudioSource>();
                footstepSource.loop = true;
                footstepSource.playOnAwake = false;
                footstepSource.spatialBlend = 0f;
                footstepSource.volume = 0.45f;
            }
        }

        public void LoadAudioClipsIfMissing()
        {
#if UNITY_EDITOR
            if (act1Bgm == null) act1Bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/1/bg.mp3");
            if (act2Bgm == null) act2Bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/2/BG.mp3");
            if (act3Bgm == null) act3Bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/3/BG.mp3");
            if (act4Bgm == null) act4Bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/4/BG.mp3");

            if (jumpSfx == null) jumpSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/jump.mp3");
            if (landSfx == null) landSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/Jump_landing.mp3");
            if (runSfx == null) runSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/Running.mp3");
            if (hurtSfx == null) hurtSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/Hurt.mp3");
            if (doorSfx == null) doorSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/Door sound.mp3");

            if (gravityShiftSfx == null) gravityShiftSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/2/gravity_shift.mp3");
            if (gravityStabilizeSfx == null) gravityStabilizeSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/2/gravity_stabilize.mp3");
            if (bounceSfx == null) bounceSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/3/bounce.mp3");
            if (stoneFallSfx == null) stoneFallSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/3/stone fall.mp3");
            if (realityRewriteShotSfx == null) realityRewriteShotSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/4/reality_rewrite_shot.mp3");
            if (realityRewriteExpireSfx == null) realityRewriteExpireSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ui New/SFX/SFX/4/reality_rewrite_expire.mp3");
#endif
            if (footstepSource != null && footstepSource.clip == null && runSfx != null)
            {
                footstepSource.clip = runSfx;
            }
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnLevelLoaded(int levelNumber)
        {
            int act = levelNumber switch
            {
                1 or 2 => 1,
                3 or 4 => 2,
                5 or 6 => 3,
                7 or 8 => 4,
                _ => 1
            };

            AudioClip targetBgm = act switch
            {
                1 => act1Bgm,
                2 => act2Bgm,
                3 => act3Bgm,
                4 => act4Bgm,
                _ => act1Bgm
            };

            if (act != currentAct || (bgmSource != null && !bgmSource.isPlaying))
            {
                currentAct = act;
                PlayBgm(targetBgm);
            }

            // Stop any running footstep loop on new level load
            OnPlayerRunningChanged(false);
        }

        private void OnPlayerJumped()
        {
            PlaySfx(jumpSfx, 0.75f, 0.96f, 1.04f);
        }

        private void OnPlayerLanded()
        {
            PlaySfx(landSfx, 0.7f, 0.95f, 1.05f);
        }

        private void OnPlayerRunningChanged(bool isRunning)
        {
            if (footstepSource == null) return;

            if (isRunning && !isFootstepsPlaying)
            {
                isFootstepsPlaying = true;
                if (!footstepSource.isPlaying)
                {
                    footstepSource.Play();
                }
            }
            else if (!isRunning && isFootstepsPlaying)
            {
                isFootstepsPlaying = false;
                if (footstepSource.isPlaying)
                {
                    footstepSource.Pause();
                }
            }
        }

        private void OnPlayerDied(string reason)
        {
            OnPlayerRunningChanged(false);
            PlaySfx(hurtSfx, 0.9f, 0.98f, 1.02f);
        }

        private void OnLevelCompleted()
        {
            OnPlayerRunningChanged(false);
            PlaySfx(doorSfx, 0.9f, 1.0f, 1.0f);
        }

        private void OnGravityModeChanged(string mode)
        {
            if (mode == "Earth")
            {
                PlaySfx(gravityStabilizeSfx, 0.8f, 0.98f, 1.02f);
            }
            else
            {
                PlaySfx(gravityShiftSfx, 0.8f, 0.98f, 1.02f);
            }
        }

        private void OnMaterialTypeChanged(string mat)
        {
            if (mat == "Stone")
            {
                PlaySfx(stoneFallSfx, 0.85f, 0.95f, 1.05f);
            }
            else if (mat == "Rubber")
            {
                PlaySfx(bounceSfx, 0.85f, 0.98f, 1.02f);
            }
        }

        private void OnObjectBounced()
        {
            PlaySfx(bounceSfx, 0.85f, 0.95f, 1.05f);
        }

        private void OnRealityRewriteFired()
        {
            PlaySfx(realityRewriteShotSfx, 0.85f, 0.97f, 1.03f);
        }

        private void OnRealityRewriteExpired()
        {
            PlaySfx(realityRewriteExpireSfx, 0.75f, 0.98f, 1.02f);
        }

        // ==========================================
        // PUBLIC PLAYBACK UTILITIES
        // ==========================================

        public void PlayJump() => OnPlayerJumped();
        public void PlayLand() => OnPlayerLanded();
        public void PlayBounce() => OnObjectBounced();
        public void PlayHurt() => PlaySfx(hurtSfx, 0.9f);
        public void PlayDoor() => PlaySfx(doorSfx, 0.9f);

        public void PlayBgm(AudioClip clip, float fadeDuration = 0.5f)
        {
            if (clip == null || bgmSource == null) return;

            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            bgmFadeCoroutine = StartCoroutine(CrossfadeRoutine(clip, fadeDuration));
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
        {
            float targetVolume = 0.55f;

            if (bgmSource.isPlaying)
            {
                float elapsed = 0f;
                float startVol = bgmSource.volume;
                while (elapsed < duration * 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (duration * 0.5f));
                    yield return null;
                }
            }

            bgmSource.clip = newClip;
            bgmSource.Play();

            float fadeInElapsed = 0f;
            while (fadeInElapsed < duration * 0.5f)
            {
                fadeInElapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, fadeInElapsed / (duration * 0.5f));
                yield return null;
            }
            bgmSource.volume = targetVolume;
            bgmFadeCoroutine = null;
        }

        public void PlaySfx(AudioClip clip, float volume = 0.8f, float minPitch = 0.95f, float maxPitch = 1.05f)
        {
            if (clip == null || sfxSource == null) return;

            sfxSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
