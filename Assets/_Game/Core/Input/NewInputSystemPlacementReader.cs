using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Interfaces;

namespace Game.Core.Input
{
    /// <summary>
    /// Implements IPlacementInput via Unity's New Input System.
    /// Provides zero-allocation cursor tracking and button state queries.
    /// </summary>
    public class NewInputSystemPlacementReader : IPlacementInput
    {
        public Vector2 GetCursorScreenPosition()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.isInProgress)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }

            return Vector2.zero;
        }

        public Vector2 GetCursorWorldPosition(Camera worldCamera)
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return Vector2.zero;
            }

            Vector2 screenPos = GetCursorScreenPosition();
            Vector3 worldPos = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -worldCamera.transform.position.z));
            return new Vector2(worldPos.x, worldPos.y);
        }

        public bool IsPointerDown()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool IsPointerPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return true;
            }

            return false;
        }

        public bool IsPointerUp()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool IsRotateRequested()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool IsCancelRequested()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }
    }
}
