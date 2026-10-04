namespace Game.Gameplay.Transmutation
{
    /// <summary>
    /// Contract for objects that can be inverted/transmuted into their opposite state.
    /// Strictly adheres to Section 6 (Interfaces Define Boundaries) and Section 4 (Single Responsibility).
    /// </summary>
    public interface ITransmutable
    {
        /// <summary>Whether this object is currently in its inverted/transmuted state.</summary>
        bool IsInverted { get; }

        /// <summary>Remaining seconds of the current transmutation effect.</summary>
        float RemainingDuration { get; }

        /// <summary>User-friendly name of the transmutable object.</summary>
        string TransmutableName { get; }

        /// <summary>Invert the object into its opposite state for the specified duration in seconds.</summary>
        void Invert(float duration = 4.0f);

        /// <summary>Immediately revert the object back to its default state.</summary>
        void Revert();
    }
}
