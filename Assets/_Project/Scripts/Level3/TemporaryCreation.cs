using System;
using UnityEngine;
using Project.Audio;

namespace Project.Level3
{
    public abstract class TemporaryCreation : MonoBehaviour
    {
        [Header("Lifetime Settings")]
        [SerializeField] protected float lifetime = 8.0f;
        [SerializeField] protected float warningThreshold = 2.5f;

        [Header("Visual & Effects")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected ParticleSystem dissolveParticles;

        protected float remainingTime = 0f;
        protected bool isWarning = false;
        protected Color baseColor = Color.white;

        public float RemainingTime => remainingTime;
        public float Lifetime => lifetime;
        public bool IsWarning => isWarning;
        public abstract CreationMaterial MaterialType { get; }

        public static event Action<TemporaryCreation, float> OnCreationSpawned;
        public static event Action<float> OnCreationTick;
        public static event Action OnCreationWarning;
        public static event Action<TemporaryCreation> OnCreationDestroyed;

        protected virtual void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) baseColor = spriteRenderer.color;
            remainingTime = lifetime;
        }

        protected virtual void Start()
        {
            ProceduralAudio.Instance?.PlayCreationSpawn();
            OnCreationSpawned?.Invoke(this, lifetime);
        }

        protected virtual void Update()
        {
            remainingTime -= Time.deltaTime;

            if (!isWarning && remainingTime <= warningThreshold)
            {
                isWarning = true;
                ProceduralAudio.Instance?.PlayCreationWarning();
                OnCreationWarning?.Invoke();
            }

            if (isWarning && spriteRenderer != null)
            {
                // Rapid flashing alert
                float flash = Mathf.PingPong(Time.time * 12f, 1f);
                Color c = baseColor;
                c.a = Mathf.Lerp(0.25f, 1.0f, flash);
                spriteRenderer.color = c;
            }

            OnCreationTick?.Invoke(remainingTime);

            if (remainingTime <= 0f)
            {
                Disappear();
            }
        }

        public virtual void Disappear()
        {
            ProceduralAudio.Instance?.PlayCreationDissolve();

            if (dissolveParticles != null)
            {
                dissolveParticles.transform.SetParent(null);
                dissolveParticles.Play();
                Destroy(dissolveParticles.gameObject, 1.5f);
            }

            OnCreationDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
