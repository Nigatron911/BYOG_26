using UnityEngine;

namespace Project.Level3
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WoodCreation : TemporaryCreation
    {
        public override CreationMaterial MaterialType => CreationMaterial.Wood;

        protected override void Awake()
        {
            base.Awake();
            // Solid platform setup
            gameObject.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = false;
        }
    }
}
