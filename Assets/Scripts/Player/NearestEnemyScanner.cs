using Combat;
using Enemy;
using Enemy.State;
using UI.HealthBarUI.EnemyUI.EnemyHpBar;
using UnityEngine;

namespace Player
{
    public class NearestEnemyScanner : MonoBehaviour
    {
        [Header("Components")] [SerializeField]
        private Transform characterTransform;

        [Header("Range")] [SerializeField] private float scanRange = 8f;

        [Header("Scan")] [SerializeField] private float scanInterval = 0.25f;

        private EnemyHealth[] _candidates = System.Array.Empty<EnemyHealth>();
        private float _nextScanTime;

        private bool _initialized;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        private EnemyHealth _nearestEnemyHealth;
        private EnemyHpBarPresenter _suppressedPresenter;

        public Health NearestHealth { get; private set; }
        public string NearestDisplayName { get; private set; } = string.Empty;

        public bool Init()
        {
            if (_initialized) return true;
            if (!characterTransform) return false;

            Scan();

            _initialized = true;
            return true;
        }

        public void Tick()
        {
            if (Time.time >= _nextScanTime) Scan();

            UpdateNearestEnemyHealth();
            ProcessNearestEnemyHealthBar();
        }

        public void Dispose()
        {
            if (_suppressedPresenter) _suppressedPresenter.SetSuppressed(false);

            _candidates = System.Array.Empty<EnemyHealth>();
            _nearestEnemyHealth = null;
            _suppressedPresenter = null;
            NearestHealth = null;
            NearestDisplayName = string.Empty;
            _initialized = false;
        }

        #endregion

        private void Scan()
        {
            _nextScanTime = Time.time + scanInterval;

            // TODO 추후 인게임 전체 상태를 관할하는 관리자 class가 구현될 경우, 해당 클래스에서 제공하는 api로 교체할 것
            _candidates = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        }

        private void UpdateNearestEnemyHealth()
                {
                    var originPosition = characterTransform.position;
                    var rangeSqr = scanRange * scanRange;
                    var nearestSqr = rangeSqr;

                    EnemyHealth nearest = null;

                    foreach (var candidate in _candidates)
                    {
                        if (!candidate || !candidate.IsInitialized || candidate.IsDepleted) continue;

                        var delta = candidate.transform.position - originPosition;
                        delta.y = 0f;

                        var distanceSqr = delta.sqrMagnitude;
                        var detected = distanceSqr <= rangeSqr;

                        if (!detected || distanceSqr > nearestSqr) continue;

                        nearestSqr = distanceSqr;
                        nearest = candidate;
                    }

                    _nearestEnemyHealth = nearest;
                }

        private void ProcessNearestEnemyHealthBar()
        {
            var next = _nearestEnemyHealth &&
                       _nearestEnemyHealth.TryGetComponent<EnemyHpBarPresenter>(out var presenter)
                ? presenter
                : null;

            if (ReferenceEquals(_suppressedPresenter, next)) return;

            if (_suppressedPresenter) _suppressedPresenter.SetSuppressed(false);
            if (next) next.SetSuppressed(true);

            _suppressedPresenter = next;

            NearestHealth = _nearestEnemyHealth ? _nearestEnemyHealth.Model : null;
            NearestDisplayName = _nearestEnemyHealth &&
                                 _nearestEnemyHealth.TryGetComponent<EnemyState>(out var state)
                ? state.DisplayName
                : string.Empty;
        }
    }
}
