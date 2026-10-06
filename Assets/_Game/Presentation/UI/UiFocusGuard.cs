using UnityEngine.UIElements;

namespace Game.Presentation.UI
{
    /// <summary>
    /// The game is played with the keyboard (Space/W jump, A/D move) and the UI with the mouse.
    /// UI Toolkit buttons take keyboard focus when clicked, and Space/Enter then "submits" the focused
    /// button - e.g. pressing Space to jump would press Simulate, Retry or Play Again.
    /// Making buttons non-focusable keeps gameplay keys out of the UI.
    /// </summary>
    public static class UiFocusGuard
    {
        public static void MakeButtonsMouseOnly(VisualElement root)
        {
            if (root == null) return;
            root.Query<Button>().ForEach(b => b.focusable = false);
            root.focusController?.focusedElement?.Blur();
        }
    }
}
