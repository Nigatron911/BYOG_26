using UnityEngine;

namespace Game.Presentation.Environment
{
    /// <summary>
    /// Keeps an art renderer in sync with a (hidden) gameplay renderer: color, visibility and facing.
    /// Lets gameplay scripts keep driving their own SpriteRenderer feedback while the player
    /// sees hand-drawn art with the correct proportions.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteTintMirror : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [Tooltip("Multiplier applied on top of the mirrored source color.")]
        [SerializeField] private Color tintMultiplier = Color.white;
        [SerializeField] private bool mirrorFlipX = true;

        private SpriteRenderer target;

        public void Configure(SpriteRenderer sourceRenderer)
        {
            source = sourceRenderer;
        }

        private void Awake()
        {
            target = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (source == null || target == null) return;
            target.color = source.color * tintMultiplier;
            if (mirrorFlipX) target.flipX = source.flipX;
            bool visible = source.gameObject.activeInHierarchy && source.color.a > 0.001f;
            if (target.enabled != visible) target.enabled = visible;
        }
    }
}
