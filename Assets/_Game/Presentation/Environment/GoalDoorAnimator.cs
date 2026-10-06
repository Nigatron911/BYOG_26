using System.Collections;
using UnityEngine;
using Game.Gameplay.Combat;

namespace Game.Presentation.Environment
{
    /// <summary>
    /// Exit door on a level goal, driven by an Animator (states: Closed, Ajar, Opening, Opened).
    /// The door creaks ajar as the player approaches, swings open when the goal is reached and
    /// snaps shut when the goal is re-armed. Pure presentation - listens to its own LevelGoal.
    /// Falls back to direct sprite swapping when no Animator controller is assigned.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GoalDoorAnimator : MonoBehaviour
    {
        public const string StateClosed = "Closed";
        public const string StateOpening = "Opening";
        private static readonly int NearParam = Animator.StringToHash("Near");

        [SerializeField] private LevelGoal goal;
        [SerializeField] private Animator animator;
        [Tooltip("Fallback frames (closed -> fully open) used only when no Animator controller is present.")]
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [Tooltip("The door eases ajar when the player is this close.")]
        [SerializeField] private float ajarDistance = 7f;
        [SerializeField] private Transform player;
        [Tooltip("After the door opens, the player fades into the doorway over this many seconds.")]
        [SerializeField] private float enterFadeSeconds = 0.3f;
        [SerializeField] private float enterDelaySeconds = 0.22f;

        private SpriteRenderer spriteRenderer;
        private bool isOpen;
        private Coroutine enterRoutine;

        private bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;

        public void Configure(LevelGoal levelGoal, Animator doorAnimator, Sprite[] fallbackFrames)
        {
            Unsubscribe();
            goal = levelGoal;
            animator = doorAnimator;
            frames = fallbackFrames;
            Subscribe();
        }

        public void BindPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            if (goal == null) goal = GetComponentInParent<LevelGoal>();
        }

        private void OnEnable()
        {
            Subscribe();
            Close();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (goal == null) return;
            goal.Reached -= Open;
            goal.Reached += Open;
            goal.Rearmed -= Close;
            goal.Rearmed += Close;
        }

        private void Unsubscribe()
        {
            if (goal == null) return;
            goal.Reached -= Open;
            goal.Rearmed -= Close;
        }

        private void Update()
        {
            if (isOpen) return;
            bool near = player != null && player.gameObject.activeInHierarchy
                        && Vector2.Distance(player.position, transform.position) < ajarDistance;

            if (HasAnimator) animator.SetBool(NearParam, near);
            else ShowFrame(near ? 1 : 0);
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            if (HasAnimator) animator.Play(StateOpening, 0, 0f);
            else ShowFrame(frames.Length - 1);

            if (player != null && isActiveAndEnabled && Vector2.Distance(player.position, transform.position) < ajarDistance)
            {
                if (enterRoutine != null) StopCoroutine(enterRoutine);
                enterRoutine = StartCoroutine(PlayerEntersDoor());
            }
        }

        /// <summary>
        /// Visual only: fades the player sprite out as they step through the open door.
        /// The player's colour is restored by its own reset when the next level (or a retry) starts.
        /// </summary>
        private IEnumerator PlayerEntersDoor()
        {
            var playerSprite = player.GetComponent<SpriteRenderer>();
            if (playerSprite == null) yield break;
            yield return new WaitForSeconds(enterDelaySeconds);

            Color start = playerSprite.color;
            float t = 0f;
            while (t < enterFadeSeconds && isOpen)
            {
                t += Time.deltaTime;
                var c = start;
                c.a = Mathf.Lerp(start.a, 0f, t / enterFadeSeconds);
                playerSprite.color = c;
                yield return null;
            }
            enterRoutine = null;
        }

        public void Close()
        {
            isOpen = false;
            if (enterRoutine != null) { StopCoroutine(enterRoutine); enterRoutine = null; }
            if (HasAnimator)
            {
                animator.SetBool(NearParam, false);
                animator.Play(StateClosed, 0, 0f);
            }
            else
            {
                ShowFrame(0);
            }
        }

        private void ShowFrame(int index)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || frames == null || frames.Length == 0) return;
            var f = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (spriteRenderer.sprite != f) spriteRenderer.sprite = f;
        }
    }
}
