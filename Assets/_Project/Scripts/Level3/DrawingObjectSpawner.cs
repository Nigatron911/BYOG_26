using System.Collections.Generic;
using UnityEngine;
using Project.Player;

namespace Project.Level3
{
    public class DrawingObjectSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnForwardOffset = 2.5f;
        [SerializeField] private float spawnHeightOffset = 0.8f;

        [Header("Sprite References")]
        [SerializeField] private Sprite woodSprite;
        [SerializeField] private Sprite rubberSprite;
        [SerializeField] private Sprite anvilSprite;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Sprite sparkSprite;

        public static DrawingObjectSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public GameObject SpawnCreation(CreationMaterial material, Vector2 drawnSize, List<Vector2> points)
        {
            var player = FindFirstObjectByType<PlayerMaterialController>();
            Vector3 spawnPos = Vector3.zero;

            if (player != null)
            {
                // Determine facing direction from player scale or velocity
                float facing = player.transform.localScale.x >= 0 ? 1f : -1f;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                {
                    facing = Mathf.Sign(rb.linearVelocity.x);
                }

                spawnPos = player.transform.position + new Vector3(facing * spawnForwardOffset, spawnHeightOffset, 0f);

                if (material == CreationMaterial.Anvil)
                {
                    // Anvil spawns slightly higher so it falls with gravity
                    spawnPos += new Vector3(0f, 1.2f, 0f);
                }
            }

            // Create GameObject
            var go = new GameObject($"Creation_{material}");
            go.transform.position = spawnPos;

            // Compute size based on drawing and material
            Vector2 finalSize;
            switch (material)
            {
                case CreationMaterial.Wood:
                    // Bridge/platform: wide horizontal span
                    float woodWidth = Mathf.Clamp(drawnSize.x * 0.015f + 2.0f, 2.8f, 5.5f);
                    finalSize = new Vector2(woodWidth, 0.6f);
                    break;

                case CreationMaterial.Rubber:
                    // Bouncy pad
                    float rubberWidth = Mathf.Clamp(drawnSize.x * 0.012f + 2.0f, 2.5f, 4.5f);
                    finalSize = new Vector2(rubberWidth, 0.6f);
                    break;

                case CreationMaterial.Anvil:
                default:
                    // Heavy square block
                    float anvilSize = Mathf.Clamp(Mathf.Max(drawnSize.x, drawnSize.y) * 0.008f + 1.2f, 1.3f, 2.0f);
                    finalSize = new Vector2(anvilSize, anvilSize);
                    break;
            }

            // Setup SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;

            // Setup Collider
            var col = go.AddComponent<BoxCollider2D>();

            // Setup Dissolve Particles
            var psGo = new GameObject("DissolveVFX");
            psGo.transform.SetParent(go.transform);
            psGo.transform.localPosition = Vector3.zero;
            var ps = psGo.AddComponent<ParticleSystem>();
            var psr = psGo.GetComponent<ParticleSystemRenderer>();
            if (particleMaterial != null) psr.material = particleMaterial;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = 0.25f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            // Assign sprite and material component
            switch (material)
            {
                case CreationMaterial.Wood:
                    sr.sprite = woodSprite;
                    sr.color = Color.white;
                    main.startColor = new Color(0.4f, 0.25f, 0.15f, 1f);
                    go.AddComponent<WoodCreation>();
                    break;

                case CreationMaterial.Rubber:
                    sr.sprite = rubberSprite;
                    sr.color = Color.white;
                    main.startColor = new Color(0.1f, 0.8f, 1.0f, 1f);
                    go.AddComponent<RubberCreation>();
                    break;

                case CreationMaterial.Anvil:
                    sr.sprite = anvilSprite;
                    sr.color = Color.white;
                    main.startColor = new Color(0.6f, 0.65f, 0.75f, 1f);
                    go.AddComponent<AnvilCreation>();
                    break;
            }

            // Apply tiled size to renderer and collider
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = finalSize;
            col.size = finalSize;

            return go;
        }
    }
}
