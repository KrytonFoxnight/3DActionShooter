using Combat;
using UnityEngine;

namespace TrainingDummy
{
    public class TrainingDummyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 100;

        private Health _health;

        private void Awake()
        {
            _health = new Health(maxHp);
        }

        public void TakeDamage(int amount)
        {
            var applied = _health.ApplyDamage(amount);
            Debug.Log($"Applied damage: {applied}");
        }
    }
}
