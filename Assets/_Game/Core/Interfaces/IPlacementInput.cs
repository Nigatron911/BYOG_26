using UnityEngine;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Abstraction for pointer / placement input sources (Mouse, Touch, Gamepad).
    /// Prevents gameplay systems from coupling directly to hardware APIs.
    /// </summary>
    public interface IPlacementInput
    {
        Vector2 GetCursorScreenPosition();
        Vector2 GetCursorWorldPosition(Camera worldCamera);
        bool IsPointerDown();
        bool IsPointerPressed();
        bool IsPointerUp();
        bool IsRotateRequested();
        bool IsCancelRequested();
    }
}
