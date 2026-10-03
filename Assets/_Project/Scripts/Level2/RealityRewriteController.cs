using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Project.Audio;

namespace Project.Level2
{
    public class RealityRewriteController : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [SerializeField] private float aimRadius = 3.8f;
        [SerializeField] private LayerMask targetLayerMask = ~0;
        [SerializeField] private Camera mainCam;

        [Header("Ability Parameters")]
        [SerializeField] private float rewriteDuration = 3.0f;
        [SerializeField] private float cooldownDuration = 5.0f;

        [Header("VFX")]
        [SerializeField] private ParticleSystem fireShockwaveParticles;

        private RealityRewriteTarget currentTarget;
        private float currentCooldown = 0f;
        private float activeRewriteTimer = 0f;
        private bool isRewritingActive = false;

        public bool HasValidTarget => currentTarget != null && currentTarget.CanRewrite();
        public RealityRewriteTarget CurrentTarget => currentTarget;
        public float CurrentCooldown => currentCooldown;
        public float CooldownDuration => cooldownDuration;
        public float ActiveRewriteTimer => activeRewriteTimer;
        public bool IsRewritingActive => isRewritingActive;
        public bool IsCooldownReady => currentCooldown <= 0f;

        // Events
        public event Action<bool, string, RealityRewriteTarget> OnTargetStatusChanged;
        public event Action<RealityRewriteTarget, float> OnRewriteFired;
        public event Action<float> OnRewriteTick;
        public event Action OnRewriteRestored;
        public event Action<float, float> OnCooldownTick;
        public event Action OnCooldownReady;
        public event Action<string> OnPlayerNotice;

        private void Awake()
        {
            if (mainCam == null) mainCam = Camera.main;
        }

        private void Update()
        {
            UpdateTargeting();
            HandleInput();
            UpdateTimers();
        }

        private void UpdateTargeting()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            // Center-screen world position
            Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, -mainCam.transform.position.z);
            Vector2 centerWorld = (Vector2)mainCam.ScreenToWorldPoint(centerScreen);

            // Check mouse position in world space safely
            Vector3 mouseScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 mPos = Mouse.current.position.ReadValue();
                mouseScreen = new Vector3(mPos.x, mPos.y, 0f);
            }
#else
            try { mouseScreen = Input.mousePosition; } catch {}
#endif
            mouseScreen.z = -mainCam.transform.position.z;
            Vector2 mouseWorld = (Vector2)mainCam.ScreenToWorldPoint(mouseScreen);

            // Find all targets within aim area
            Collider2D[] centerHits = Physics2D.OverlapCircleAll(centerWorld, aimRadius, targetLayerMask);
            Collider2D[] mouseHits = Physics2D.OverlapCircleAll(mouseWorld, 2.5f, targetLayerMask);

            RealityRewriteTarget bestTarget = null;
            float closestDist = float.MaxValue;

            // Check center-screen reticle hits
            foreach (var hit in centerHits)
            {
                var target = hit.GetComponentInParent<RealityRewriteTarget>();
                if (target != null && target.CanRewrite())
                {
                    float dist = Vector2.Distance(centerWorld, hit.ClosestPoint(centerWorld));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = target;
                    }
                }
            }

            // Also check mouse cursor hits if no center hit
            if (bestTarget == null)
            {
                float closestMouseDist = float.MaxValue;
                foreach (var hit in mouseHits)
                {
                    var target = hit.GetComponentInParent<RealityRewriteTarget>();
                    if (target != null && target.CanRewrite())
                    {
                        float dist = Vector2.Distance(mouseWorld, hit.ClosestPoint(mouseWorld));
                        if (dist < closestMouseDist)
                        {
                            closestMouseDist = dist;
                            bestTarget = target;
                        }
                    }
                }
            }

            // Target update
            if (bestTarget != currentTarget)
            {
                if (currentTarget != null) currentTarget.SetTargeted(false);
                currentTarget = bestTarget;
                if (currentTarget != null) currentTarget.SetTargeted(true);
            }

            if (currentTarget != null && currentTarget.CanRewrite())
            {
                OnTargetStatusChanged?.Invoke(true, "CLICK TO REWRITE", currentTarget);
            }
            else
            {
                OnTargetStatusChanged?.Invoke(false, "TARGET INVALID", null);
            }
        }

        private void HandleInput()
        {
            bool clickPressed = false;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                clickPressed = true;
            }
#else
            try
            {
                if (Input.GetMouseButtonDown(0)) clickPressed = true;
            }
            catch {}
#endif

            if (clickPressed)
            {
                TryRewrite();
            }
        }

        public bool TryRewrite()
        {
            if (currentCooldown > 0f)
            {
                // On Cooldown!
                ProceduralAudio.Instance?.PlayFail();
                OnPlayerNotice?.Invoke($"REWRITE ON COOLDOWN! ({currentCooldown:F1}s)");
                return false;
            }

            if (currentTarget == null || !currentTarget.CanRewrite())
            {
                // No valid target
                OnPlayerNotice?.Invoke("NO VALID TARGET AIMED!");
                return false;
            }

            // Fire rewrite!
            var targetToRewrite = currentTarget;
            targetToRewrite.Rewrite(rewriteDuration);

            // Start cooldown & active timer
            currentCooldown = cooldownDuration;
            activeRewriteTimer = rewriteDuration;
            isRewritingActive = true;

            if (fireShockwaveParticles != null) fireShockwaveParticles.Play();
            OnRewriteFired?.Invoke(targetToRewrite, rewriteDuration);

            return true;
        }

        private void UpdateTimers()
        {
            // Active Rewrite Timer
            if (isRewritingActive)
            {
                activeRewriteTimer -= Time.deltaTime;
                if (activeRewriteTimer <= 0f)
                {
                    activeRewriteTimer = 0f;
                    isRewritingActive = false;
                    OnRewriteRestored?.Invoke();
                }
                else
                {
                    OnRewriteTick?.Invoke(activeRewriteTimer);
                }
            }

            // Cooldown Timer
            if (currentCooldown > 0f)
            {
                currentCooldown -= Time.deltaTime;
                if (currentCooldown <= 0f)
                {
                    currentCooldown = 0f;
                    ProceduralAudio.Instance?.PlayCooldownReady();
                    OnCooldownReady?.Invoke();
                }
                else
                {
                    OnCooldownTick?.Invoke(currentCooldown, cooldownDuration);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;
            Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, -mainCam.transform.position.z);
            Vector2 centerWorld = (Vector2)mainCam.ScreenToWorldPoint(centerScreen);
            Gizmos.color = HasValidTarget ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(centerWorld, aimRadius);
        }
    }
}
