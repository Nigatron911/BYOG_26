using System;
using System.Collections;
using UnityEngine;

namespace Project.Level2
{
    public abstract class RealityRewriteTarget : MonoBehaviour
    {
        [Header("Target Info")]
        [SerializeField] protected string targetName = "Target";
        [SerializeField] protected float rewriteDuration = 3.0f;

        protected bool isRewritten = false;
        protected float remainingRewriteTime = 0f;
        protected bool isTargeted = false;

        public bool IsRewritten => isRewritten;
        public float RemainingRewriteTime => remainingRewriteTime;
        public float RewriteDuration => rewriteDuration;
        public string TargetName => targetName;

        public event Action<RealityRewriteTarget, float> OnTargetRewritten;
        public event Action<RealityRewriteTarget> OnTargetRestored;

        [Header("Visual References")]
        [SerializeField] protected SpriteRenderer mainRenderer;
        [SerializeField] protected GameObject highlightOutline;

        protected virtual void Awake()
        {
            if (mainRenderer == null) mainRenderer = GetComponent<SpriteRenderer>();
        }

        public virtual void SetTargeted(bool targeted)
        {
            isTargeted = targeted;
            if (highlightOutline != null)
            {
                highlightOutline.SetActive(targeted && !isRewritten);
            }
        }

        public virtual bool CanRewrite()
        {
            return !isRewritten;
        }

        public virtual void Rewrite(float duration = 3.0f)
        {
            if (isRewritten) return;
            rewriteDuration = duration;
            remainingRewriteTime = duration;
            isRewritten = true;

            if (highlightOutline != null) highlightOutline.SetActive(false);

            ApplyRewriteState();
            OnTargetRewritten?.Invoke(this, duration);
            StartCoroutine(RewriteTimerRoutine());
        }

        protected virtual IEnumerator RewriteTimerRoutine()
        {
            while (remainingRewriteTime > 0f)
            {
                remainingRewriteTime -= Time.deltaTime;
                yield return null;
            }

            remainingRewriteTime = 0f;
            RestoreOriginalState();
            isRewritten = false;
            OnTargetRestored?.Invoke(this);
        }

        protected abstract void ApplyRewriteState();
        protected abstract void RestoreOriginalState();
    }
}
