using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.Events;
using Game.Core.Save;
using Game.Data.Audio;
using Game.Gameplay.Environment;
using Game.Gameplay.Player;
using Game.Gameplay.Transmutation;
using Project.Player;

namespace Game.Presentation.Audio
{
    /// <summary>
    /// Presentation-layer audio: level music with crossfades plus gameplay sound effects.
    /// Reacts to events and observes player locomotion; never changes gameplay state (Section 14).
    /// </summary>
    public class GameAudioController : MonoBehaviour
    {
        private const float JumpVelocityThreshold = 2.5f;
        private const float LandSpeedThreshold = 3.0f;
        private const float RunSpeedThreshold = 0.8f;
        private const float MinAirTimeForLanding = 0.12f;

        private GameAudioLibrary library;
        private GameEvents events;
        private ProgressStore settings;

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource activeMusic;
        private AudioSource sfxSource;
        private AudioSource runSource;
        private Coroutine crossfadeRoutine;
        private float activeTrackVolume = 0.6f;
        private float musicDuck = 1f;

        // Player observation
        private AutonomousPlayerController player;
        private Rigidbody2D playerBody;
        private PlayerMaterialController materialController;
        private PlayerGravityController gravityController;
        private bool wasGrounded = true;
        private float airTime;
        private float peakFallSpeed;

        private readonly List<System.Action> unbinders = new List<System.Action>();

        public float MusicVolume => settings != null ? settings.MusicVolume : 0.7f;
        public float SfxVolume => settings != null ? settings.SfxVolume : 0.9f;

        public void Initialize(GameEvents gameEvents, GameAudioLibrary audioLibrary, ProgressStore store)
        {
            Unbind();
            events = gameEvents;
            library = audioLibrary;
            settings = store;
            EnsureSources();

            if (events != null)
            {
                events.LevelLoaded += OnLevelLoaded;
                events.PlayerDied += OnPlayerDied;
                events.LevelCompleted += OnLevelCompleted;
                events.FlowStateChanged += OnFlowStateChanged;
                unbinders.Add(() =>
                {
                    events.LevelLoaded -= OnLevelLoaded;
                    events.PlayerDied -= OnPlayerDied;
                    events.LevelCompleted -= OnLevelCompleted;
                    events.FlowStateChanged -= OnFlowStateChanged;
                });
            }

            if (library == null)
            {
                Debug.LogWarning("[GameAudioController] No GameAudioLibrary assigned - audio disabled. Run 'BYOG/Setup Complete Game'.", this);
            }
        }

        public void BindPlayer(AutonomousPlayerController playerController)
        {
            player = playerController;
            if (player == null) return;

            playerBody = player.GetComponent<Rigidbody2D>();
            materialController = player.GetComponent<PlayerMaterialController>();
            gravityController = player.GravityController != null ? player.GravityController : player.GetComponent<PlayerGravityController>();

            if (gravityController != null)
            {
                gravityController.GravityModeChanged += OnGravityModeChanged;
                var gc = gravityController;
                unbinders.Add(() => gc.GravityModeChanged -= OnGravityModeChanged);
            }

            if (materialController != null)
            {
                materialController.OnRubberBounceExecuted += OnRubberBounce;
                var mc = materialController;
                unbinders.Add(() => mc.OnRubberBounceExecuted -= OnRubberBounce);
            }
        }

        public void BindWorld(
            Level7TransmutationController transmutation,
            IEnumerable<TransmutableWall> walls,
            IEnumerable<TransmutableSpike> spikes,
            IEnumerable<Level7PatrolEnemy> enemies,
            IEnumerable<Level4TimedDoor> doors)
        {
            if (transmutation != null)
            {
                transmutation.OnObjectTransmuted += OnObjectTransmuted;
                unbinders.Add(() => transmutation.OnObjectTransmuted -= OnObjectTransmuted);
            }

            if (walls != null)
            {
                foreach (var w in walls)
                {
                    if (w == null) continue;
                    var wall = w;
                    wall.OnStateChanged += OnTransmutableStateChanged;
                    unbinders.Add(() => wall.OnStateChanged -= OnTransmutableStateChanged);
                }
            }

            if (spikes != null)
            {
                foreach (var s in spikes)
                {
                    if (s == null) continue;
                    var spike = s;
                    spike.OnStateChanged += OnTransmutableStateChanged;
                    spike.OnPlayerBounced += OnGenericBounce;
                    unbinders.Add(() =>
                    {
                        spike.OnStateChanged -= OnTransmutableStateChanged;
                        spike.OnPlayerBounced -= OnGenericBounce;
                    });
                }
            }

            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    if (e == null) continue;
                    var enemy = e;
                    enemy.OnStateChanged += OnTransmutableStateChanged;
                    enemy.OnPlayerBoostJumped += OnGenericBounce;
                    unbinders.Add(() =>
                    {
                        enemy.OnStateChanged -= OnTransmutableStateChanged;
                        enemy.OnPlayerBoostJumped -= OnGenericBounce;
                    });
                }
            }

            if (doors != null)
            {
                foreach (var d in doors)
                {
                    if (d == null) continue;
                    var door = d;
                    door.Opened += OnDoorOpened;
                    unbinders.Add(() => door.Opened -= OnDoorOpened);
                }
            }
        }

        public void ApplyVolumes()
        {
            if (activeMusic != null) activeMusic.volume = activeTrackVolume * MusicVolume * musicDuck;
            if (sfxSource != null) sfxSource.volume = SfxVolume;
        }

        // ---------------------------------------------------------------- Music

        private void OnLevelLoaded(int levelNumber)
        {
            if (library == null) return;
            var track = library.GetTrackForLevel(levelNumber);
            if (track != null) PlayMusic(track.clip, track.volume);
        }

        private void OnFlowStateChanged(GameFlowState state)
        {
            switch (state)
            {
                case GameFlowState.MainMenu:
                    musicDuck = 1f;
                    if (library != null && library.MenuMusic != null) PlayMusic(library.MenuMusic, 0.6f);
                    StopRunLoop();
                    break;
                case GameFlowState.Paused:
                    musicDuck = 0.4f;
                    StopRunLoop();
                    break;
                default:
                    musicDuck = 1f;
                    break;
            }
            ApplyVolumes();
        }

        private void PlayMusic(AudioClip clip, float trackVolume)
        {
            if (clip == null) return;
            if (activeMusic != null && activeMusic.clip == clip && activeMusic.isPlaying)
            {
                activeTrackVolume = trackVolume;
                ApplyVolumes();
                return;
            }

            var from = activeMusic;
            var to = activeMusic == musicA ? musicB : musicA;
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            activeMusic = to;
            activeTrackVolume = trackVolume;

            if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = StartCoroutine(Crossfade(from, to, library != null ? library.MusicCrossfadeSeconds : 1f));
        }

        private IEnumerator Crossfade(AudioSource from, AudioSource to, float duration)
        {
            float fromStart = from != null ? from.volume : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                float target = activeTrackVolume * MusicVolume * musicDuck;
                to.volume = Mathf.Lerp(0f, target, k);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                yield return null;
            }
            if (from != null) { from.Stop(); from.clip = null; }
            crossfadeRoutine = null;
            ApplyVolumes();
        }

        // ---------------------------------------------------------------- SFX

        private void PlaySfx(AudioClip clip, float volumeScale = 1f, bool varyPitch = false)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = varyPitch && library != null ? 1f + Random.Range(-library.PitchVariance, library.PitchVariance) : 1f;
            sfxSource.PlayOneShot(clip, volumeScale * SfxVolume);
        }

        private void OnPlayerDied(string cause)
        {
            StopRunLoop();
            if (library != null) PlaySfx(library.Hurt);
        }

        private void OnLevelCompleted()
        {
            StopRunLoop();
            if (library != null) PlaySfx(library.Door);
        }

        private void OnDoorOpened()
        {
            if (library != null) PlaySfx(library.Door, 0.8f);
        }

        private void OnGravityModeChanged(GravityMode mode)
        {
            if (library == null) return;
            PlaySfx(mode == GravityMode.Earth ? library.GravityStabilize : library.GravityShift, 0.8f);
        }

        private void OnRubberBounce(int tier, float velocity)
        {
            if (library != null) PlaySfx(library.Bounce, Mathf.Lerp(0.6f, 1f, tier / 3f), true);
        }

        private void OnGenericBounce()
        {
            if (library != null) PlaySfx(library.Bounce, 0.9f, true);
        }

        private void OnObjectTransmuted(ITransmutable target)
        {
            if (library != null) PlaySfx(library.RewriteShot, 0.9f);
        }

        private void OnTransmutableStateChanged(bool transmuted)
        {
            if (!transmuted && library != null) PlaySfx(library.RewriteExpire, 0.7f);
        }

        // ---------------------------------------------------------------- Locomotion observation

        private void Update()
        {
            if (player == null || playerBody == null || library == null) return;

            if (player.IsDead || Time.timeScale <= 0f || !player.gameObject.activeInHierarchy)
            {
                StopRunLoop();
                wasGrounded = true;
                return;
            }

            float gravitySign = playerBody.gravityScale < 0f ? -1f : 1f;
            Vector2 downDir = gravitySign > 0f ? Vector2.down : Vector2.up;
            bool grounded = player.IsClimbing || player.CheckSurfaceGrounded(downDir);
            Vector2 v = playerBody.linearVelocity;
            float upSpeed = v.y * gravitySign;   // positive = moving away from the floor

            if (!grounded)
            {
                airTime += Time.deltaTime;
                peakFallSpeed = Mathf.Max(peakFallSpeed, -upSpeed);
            }

            if (wasGrounded && !grounded && upSpeed > JumpVelocityThreshold && !player.IsClimbing)
            {
                PlaySfx(library.Jump, 0.8f, true);
            }
            else if (!wasGrounded && grounded && airTime > MinAirTimeForLanding && peakFallSpeed > LandSpeedThreshold)
            {
                bool isStone = materialController != null && materialController.enabled && materialController.CurrentMaterial == MaterialType.Stone;
                if (isStone && library.StoneImpact != null) PlaySfx(library.StoneImpact, 0.9f, true);
                else PlaySfx(library.Land, Mathf.Clamp01(peakFallSpeed / 15f) * 0.5f + 0.5f, true);
            }

            if (grounded)
            {
                airTime = 0f;
                peakFallSpeed = 0f;
            }

            bool running = grounded && !player.IsClimbing && Mathf.Abs(v.x) > RunSpeedThreshold;
            if (running) StartRunLoop(); else StopRunLoop();

            wasGrounded = grounded;
        }

        private void StartRunLoop()
        {
            if (runSource == null || library == null || library.RunningLoop == null) return;
            runSource.volume = library.RunningLoopVolume * SfxVolume;
            if (runSource.isPlaying) return;
            runSource.clip = library.RunningLoop;
            runSource.Play();
        }

        private void StopRunLoop()
        {
            if (runSource != null && runSource.isPlaying) runSource.Stop();
        }

        // ---------------------------------------------------------------- Lifecycle

        private void EnsureSources()
        {
            if (musicA != null) return;
            musicA = CreateSource("Music_A", loop: true);
            musicB = CreateSource("Music_B", loop: true);
            sfxSource = CreateSource("SFX", loop: false);
            runSource = CreateSource("SFX_RunLoop", loop: true);
            sfxSource.ignoreListenerPause = true;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        private void Unbind()
        {
            for (int i = 0; i < unbinders.Count; i++) unbinders[i]?.Invoke();
            unbinders.Clear();
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
