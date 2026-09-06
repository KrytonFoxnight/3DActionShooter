using System;

namespace Combat
{
    public class Health
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDepleted => Current <= 0;

        public event Action Changed;        // 체력 변경에 대한 이벤트

        public Health(int max)
        {
            Max = max;
            Current = max;
        }

        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDepleted) return 0;

            var applied = Math.Min(amount, Current);
            Current -= applied;

            Changed?.Invoke();

            return applied;
        }

        public void ResetToFull()
        {
            Current = Max;

            Changed?.Invoke();
        }
    }
}
