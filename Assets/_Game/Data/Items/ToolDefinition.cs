using UnityEngine;
using Game.Core.Events;

namespace Game.Data.Items
{
    /// <summary>
    /// Configuration asset defining a placeable tool's metadata, icon, and prefab.
    /// Strictly immutable at runtime adhering to Section 8 of the Architecture Contract.
    /// </summary>
    [CreateAssetMenu(fileName = "ToolDefinition", menuName = "Game/Data/Tool Definition")]
    public class ToolDefinition : ScriptableObject
    {
        [SerializeField] private ToolType toolType;
        [SerializeField] private string displayName = "Tool";
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int maxCount = 1;

        public ToolType ToolType => toolType;
        public string DisplayName => displayName;
        public string ToolName => displayName;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public int MaxCount => maxCount;
    }
}
