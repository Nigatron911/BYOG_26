using System.Collections.Generic;
using UnityEngine;
using Game.Core.Events;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Represents a placed chain connecting two anchor points in the world.
    /// Builds a walkable suspension curve with chain link visuals and anchor pins.
    /// Strictly adheres to Section 4 (Single Responsibility) and Section 14 (Juice/Presentation separation).
    /// </summary>
    [RequireComponent(typeof(DraggableTool), typeof(EdgeCollider2D))]
    public class ChainTool : MonoBehaviour
    {
        [Header("Chain Settings")]
        [SerializeField] private float linkSpacing = 0.35f;
        [SerializeField] private float maxSag = 0.15f;
        [SerializeField] private float edgeRadius = 0.10f;
        [SerializeField] private Sprite linkSprite;
        [SerializeField] private Sprite anchorSprite;

        private Vector2 pointA;
        private Vector2 pointB;
        private EdgeCollider2D edgeCollider;
        private DraggableTool draggableTool;
        private readonly List<GameObject> spawnedChildren = new List<GameObject>();

        public Vector2 PointA => pointA;
        public Vector2 PointB => pointB;
        public float Length => Vector2.Distance(pointA, pointB);
        public Sprite LinkSprite => linkSprite;
        public Sprite AnchorSprite => anchorSprite;

        private void Awake()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
            draggableTool = GetComponent<DraggableTool>();
            if (draggableTool != null)
            {
                draggableTool.SetType(ToolType.Chain);
            }
        }

        public void Initialize(Vector2 start, Vector2 end, float lifetime = 6.5f, Sprite customLink = null, Sprite customAnchor = null)
        {
            if (customLink != null) linkSprite = customLink;
            if (customAnchor != null) anchorSprite = customAnchor;

            pointA = start;
            pointB = end;

            if (draggableTool == null) draggableTool = GetComponent<DraggableTool>();
            if (draggableTool != null)
            {
                draggableTool.SetType(ToolType.Chain);
                draggableTool.SetLifetime(lifetime);
            }

            BuildChain(start, end);

            if (draggableTool != null)
            {
                draggableTool.SetPreviewMode(false);
                draggableTool.RecordPlacedTransform();
                draggableTool.StartLifetime();
            }
        }

        public void BuildChain(Vector2 start, Vector2 end)
        {
            pointA = start;
            pointB = end;

            // Clear previous link/anchor children
            for (int i = 0; i < spawnedChildren.Count; i++)
            {
                if (spawnedChildren[i] != null)
                {
                    if (Application.isPlaying) Destroy(spawnedChildren[i]);
                    else DestroyImmediate(spawnedChildren[i]);
                }
            }
            spawnedChildren.Clear();

            // Midpoint position for the root object
            Vector2 mid = (start + end) * 0.5f;
            transform.position = new Vector3(mid.x, mid.y, transform.position.z);

            Vector2 localA = start - mid;
            Vector2 localB = end - mid;

            float span = Vector2.Distance(start, end);
            if (span < 0.2f) span = 0.2f;

            // Dynamic sag proportional to span length
            float sag = Mathf.Clamp(span * 0.04f, 0.05f, maxSag);

            int segments = Mathf.Max(4, Mathf.RoundToInt(span / linkSpacing));
            Vector2[] curvePoints = new Vector2[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 linear = Vector2.Lerp(localA, localB, t);
                // Parabolic sag curve: 4 * t * (1 - t) peaks at 1.0 in the center
                float sagOffset = 4f * t * (1f - t) * sag;
                curvePoints[i] = linear + Vector2.down * sagOffset;
            }

            // Configure walkable EdgeCollider2D
            if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
            if (edgeCollider != null)
            {
                edgeCollider.edgeRadius = edgeRadius;
                edgeCollider.points = curvePoints;
                edgeCollider.isTrigger = false;
            }

            // Spawn chain link sprites along the curve
            for (int i = 0; i < segments; i++)
            {
                Vector2 p0 = curvePoints[i];
                Vector2 p1 = curvePoints[i + 1];
                Vector2 segmentCenter = (p0 + p1) * 0.5f;
                Vector2 dir = (p1 - p0).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                var linkGO = new GameObject($"Link_{i}");
                linkGO.transform.SetParent(transform, false);
                linkGO.transform.localPosition = new Vector3(segmentCenter.x, segmentCenter.y, 0f);
                linkGO.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

                var sr = linkGO.AddComponent<SpriteRenderer>();
                sr.sprite = linkSprite;
                sr.sortingOrder = 5;
                sr.color = Color.white;

                // Scale link sprite to match segment width
                float segmentLen = Vector2.Distance(p0, p1);
                linkGO.transform.localScale = new Vector3(segmentLen * 1.8f, 1.2f, 1f);

                spawnedChildren.Add(linkGO);
            }

            // Spawn anchor pins at both ends
            SpawnAnchorPin(localA, "Anchor_Start");
            SpawnAnchorPin(localB, "Anchor_End");

            // Cache all renderers for alpha pulse warning on DraggableTool
            if (draggableTool != null)
            {
                draggableTool.RefreshRenderers();
            }
        }

        private void SpawnAnchorPin(Vector2 localPos, string name)
        {
            var pinGO = new GameObject(name);
            pinGO.transform.SetParent(transform, false);
            pinGO.transform.localPosition = new Vector3(localPos.x, localPos.y, -0.05f);

            var sr = pinGO.AddComponent<SpriteRenderer>();
            sr.sprite = anchorSprite;
            sr.sortingOrder = 6;
            sr.color = Color.white;
            pinGO.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            spawnedChildren.Add(pinGO);
        }
    }
}
