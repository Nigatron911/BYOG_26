namespace Game.Core.Interfaces
{
    /// <summary>
    /// Contract for entities capable of deflecting or resisting specific hazards (e.g. Stone form deflecting Spikes).
    /// Adheres to Section 6 of GEMINI.md (Interfaces Define Boundaries).
    /// </summary>
    public interface IHazardImmunity
    {
        bool IsImmuneToHazard(string hazardType);
    }
}
