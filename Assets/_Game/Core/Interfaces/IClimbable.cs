using UnityEngine;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Contract for climbable structures such as ladders.
    /// Allows autonomous characters to detect and scale vertical geometry.
    /// </summary>
    public interface IClimbable
    {
        float ClimbSpeedMultiplier { get; }
        Bounds GetBounds();
        float TopElevation { get; }
    }
}
