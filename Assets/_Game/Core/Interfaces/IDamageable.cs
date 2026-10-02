namespace Game.Core.Interfaces
{
    /// <summary>
    /// Contract for any entity capable of taking damage or being eliminated by hazards.
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void Kill(string cause);
    }
}
