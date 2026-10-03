using System;
using UnityEngine;
using Game.Core.Events;

namespace Game.Gameplay.Player
{
    public enum GravityMode
    {
        Earth,
        Moon,
        InvertedRoof
    }

    /// <summary>
    /// Manages 3 gravity modes in Level 3:
    /// 1. Earth (Normal 1.0g, upright, standard jump)
    /// 2. Moon (Low 0.38g, upright, floaty jump)
    /// 3. InvertedRoof (Reversal -0.85g, upside down with feet at roof, inverted controls)
    /// Alternates modes automatically on a cycle timer, supports manual testing toggle (G key).
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 10 (State Pattern).
    /// </summary>
    public class PlayerGravityController : MonoBehaviour
    {
        [Header("Gravity Scales")]
        [SerializeField] private float earthGravityScale = 1.0f;
        [SerializeField] private float moonGravityScale = 0.25f;
        [SerializeField] private float invertedRoofGravityScale = -0.85f;

        [Header("Cycle Configuration")]
        [Tooltip("Switch interval in seconds. Default: 3.0 seconds.")]
        [SerializeField] private float switchIntervalSeconds = 3.0f;
        [SerializeField] private bool autoCycleEnabled = true;
        [SerializeField] private bool randomCycleEnabled = false;
        [SerializeField] private bool includeInvertedMode = false;

        [Header("Runtime State")]
        [SerializeField] private GravityMode currentMode = GravityMode.Earth;
        private float cycleTimer = 0f;
        private bool isSimulating = false;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private GameEvents events;

        public GravityMode CurrentMode => currentMode;
        public float EarthGravityScale => earthGravityScale;
        public float MoonGravityScale => moonGravityScale;
        public float InvertedRoofGravityScale => invertedRoofGravityScale;
        public float SwitchIntervalSeconds => switchIntervalSeconds;
        public float TimeRemainingInMode => Mathf.Max(0f, switchIntervalSeconds - cycleTimer);
        public bool IsSimulating => isSimulating;
        public bool IsAutoCycleEnabled { get => autoCycleEnabled; set => autoCycleEnabled = value; }
        public bool IsRandomCycleEnabled { get => randomCycleEnabled; set => randomCycleEnabled = value; }
        public bool IncludeInvertedMode { get => includeInvertedMode; set => includeInvertedMode = value; }

        public event Action<GravityMode> GravityModeChanged;

        public void Initialize(Rigidbody2D body, SpriteRenderer renderer, GameEvents gameEvents = null)
        {
            rb = body != null ? body : GetComponent<Rigidbody2D>();
            spriteRenderer = renderer != null ? renderer : GetComponentInChildren<SpriteRenderer>();
            events = gameEvents;

            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStarted += OnSimulationStarted;

                events.SimulationStopped -= OnSimulationStopped;
                events.SimulationStopped += OnSimulationStopped;

                events.LevelResetRequested -= OnLevelResetRequested;
                events.LevelResetRequested += OnLevelResetRequested;
            }

            SetGravityMode(GravityMode.Earth, force: true);
        }

        public void Dispose()
        {
            if (events != null)
            {
                events.SimulationStarted -= OnSimulationStarted;
                events.SimulationStopped -= OnSimulationStopped;
                events.LevelResetRequested -= OnLevelResetRequested;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnDisable()
        {
            isSimulating = false;
        }

        public void SetSwitchInterval(float seconds)
        {
            switchIntervalSeconds = Mathf.Max(0.5f, seconds);
        }

        private void OnSimulationStarted()
        {
            if (this == null || !enabled) return;
            isSimulating = true;
            cycleTimer = 0f;
            ApplyCurrentModePhysics();
        }

        private void OnSimulationStopped()
        {
            if (this == null || !enabled) return;
            isSimulating = false;
            cycleTimer = 0f;
            SetGravityMode(GravityMode.Earth, force: true);
        }

        private void OnLevelResetRequested()
        {
            if (this == null || !enabled) return;
            isSimulating = false;
            cycleTimer = 0f;
            SetGravityMode(GravityMode.Earth, force: true);
        }

        public void ResetCycle()
        {
            cycleTimer = 0f;
            SetGravityMode(GravityMode.Earth, force: true);
        }

        public void SetRandomCycle(bool enabled)
        {
            randomCycleEnabled = enabled;
        }

        public void ConfigureCycle(bool includeInverted, float intervalSeconds = 3.0f, bool random = false)
        {
            includeInvertedMode = includeInverted;
            switchIntervalSeconds = Mathf.Max(0.5f, intervalSeconds);
            randomCycleEnabled = random;
        }

        public void SwitchToRandomMode()
        {
            GravityMode[] allModes = includeInvertedMode
                ? new GravityMode[] { GravityMode.Earth, GravityMode.Moon, GravityMode.InvertedRoof }
                : new GravityMode[] { GravityMode.Earth, GravityMode.Moon };

            var candidates = new System.Collections.Generic.List<GravityMode>();
            for (int i = 0; i < allModes.Length; i++)
            {
                if (allModes[i] != currentMode) candidates.Add(allModes[i]);
            }
            GravityMode next = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            SetGravityMode(next);
        }

        public void ToggleGravityMode()
        {
            if (randomCycleEnabled)
            {
                SwitchToRandomMode();
                return;
            }

            GravityMode next;
            if (!includeInvertedMode)
            {
                // Level 3: strictly toggle between Earth and Moon!
                next = currentMode == GravityMode.Earth ? GravityMode.Moon : GravityMode.Earth;
            }
            else
            {
                next = currentMode switch
                {
                    GravityMode.Earth => GravityMode.Moon,
                    GravityMode.Moon => GravityMode.InvertedRoof,
                    GravityMode.InvertedRoof => GravityMode.Earth,
                    _ => GravityMode.Earth
                };
            }
            SetGravityMode(next);
        }

        public void SetGravityMode(GravityMode newMode, bool force = false)
        {
            if (!force && currentMode == newMode) return;

            currentMode = newMode;
            cycleTimer = 0f;

            ApplyCurrentModePhysics();

            Debug.Log($"[PlayerGravityController] Switched to {currentMode} gravity! (Scale: {rb?.gravityScale}, Interval: {switchIntervalSeconds}s)");
            GravityModeChanged?.Invoke(currentMode);
        }

        private void ApplyCurrentModePhysics()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            switch (currentMode)
            {
                case GravityMode.Earth:
                    if (rb != null)
                    {
                        rb.gravityScale = earthGravityScale;
                        transform.localEulerAngles = Vector3.zero;
                        Physics2D.SyncTransforms();

                        var playerCtrl = GetComponent<AutonomousPlayerController>();
                        bool isActive = isSimulating || (playerCtrl != null && playerCtrl.CurrentLocomotionMode == LocomotionMode.Manual);

                        // Sudden Earth gravity transition:
                        // Instantly cancel any residual upward float/velocity from Moon or InvertedRoof mode.
                        // If player is airborne, immediately snap downward velocity so they drop with sudden force.
                        if (isActive)
                        {
                            bool isGrounded = playerCtrl != null && playerCtrl.CheckSurfaceGrounded(Vector2.down);

                            if (!isGrounded)
                            {
                                // Sudden downward plunge upon entering Earth gravity
                                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -11.0f);
                                rb.gravityScale = earthGravityScale * 1.8f;
                            }
                            else
                            {
                                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f));
                            }
                        }
                    }
                    break;

                case GravityMode.Moon:
                    if (rb != null)
                    {
                        rb.gravityScale = moonGravityScale;
                        // Automatic float lift when Moon mode activates!
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 4.2f));
                    }
                    transform.localEulerAngles = Vector3.zero;
                    break;

                case GravityMode.InvertedRoof:
                    if (rb != null)
                    {
                        rb.gravityScale = invertedRoofGravityScale;
                        // Upward pull towards roof
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 3.2f));
                    }
                    // Upside down: feet point at the roof
                    transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                    break;
            }
        }

        private void Update()
        {
            if (!enabled) return;

            // Manual hotkey G to toggle gravity anytime during testing
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.gKey.wasPressedThisFrame)
            {
                ToggleGravityMode();
                return;
            }

            var player = GetComponent<AutonomousPlayerController>();
            bool isManualMode = player != null && player.CurrentLocomotionMode == LocomotionMode.Manual;
            bool shouldCycle = (isSimulating || isManualMode) && autoCycleEnabled;
            if (!shouldCycle) return;

            cycleTimer += Time.deltaTime;
            if (cycleTimer >= switchIntervalSeconds)
            {
                cycleTimer = 0f;
                ToggleGravityMode();
            }
        }
    }
}
