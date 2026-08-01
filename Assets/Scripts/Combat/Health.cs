using System;

namespace Combat
{
    public class Health
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        public Health(int max)
        {
            Max = max;
            Current = max;
        }

        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead) return 0;

            var applied = Math.Min(amount, Current);
            Current -= applied;
            return applied;
        }

        public void ResetToFull()
        {
            Current = Max;
        }
    }
}
