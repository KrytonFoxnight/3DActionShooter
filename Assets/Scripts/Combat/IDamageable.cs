namespace Combat
{
    public interface IDamageable
    {
        bool IsDepleted { get; }

        void TakeDamage(int amount);
    }
}
