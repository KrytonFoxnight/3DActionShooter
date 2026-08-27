#nullable enable
using Combat;
using UnityEngine;

namespace UI.HealthBarUI.EnemyUI.EnemyHpBar
{
    public class EnemyHpBarPresenter : MonoBehaviour
    {
        [Header("Tracking")]
        [SerializeField] private Transform anchor = null!;
        [SerializeField] private float worldHeightOffset = 2f;

        [Header("Distance Scale")]
        [SerializeField] private float referenceDistance = 10f;
        [SerializeField] private float minScale = 0.4f;
        [SerializeField] private float maxScale = 1.2f;

        [Header("Death")]
        [SerializeField] private float hideDelayOnDeath = 2f;

        private Health _health = null!;
        private EnemyHpBarRoot _root = null!;
        private EnemyHpBarView _view = null!;
        private Camera _camera = null!;

        private float? _hideAt;
        private bool _hidden;
        private bool _initialized;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(Health? health)
        {
            if (_initialized) return true;
            if (health == null || !anchor) return false;

            var mainCamera = Camera.main;
            if (!mainCamera) return false;

            var root = FindFirstObjectByType<EnemyHpBarRoot>();
            if (!root) return false;

            _view = root.GetEnemyHpBarView();
            if (!_view) return false;

            _root = root;
            _camera = mainCamera;
            _health = health;
            _health.Changed += Refresh;

            Refresh();

            _initialized = true;
            return true;
        }

        public void Dispose()
        {
            _health.Changed -= Refresh;
            if (_root) _root.ReturnEnemyHpBarView(_view);

            _root = null!;
            _view = null!;
            _health = null!;
            _camera = null!;
            _hideAt = null;
            _hidden = false;
            _initialized = false;
        }

        #endregion

        private void LateUpdate()
        {
            if (!_initialized || _hidden) return;

            if (_hideAt.HasValue && Time.time >= _hideAt.Value)
            {
                _hidden = true;
                _view.SetVisible(false);
                return;
            }

            var screenPosition = _camera.WorldToScreenPoint(anchor.position + Vector3.up * worldHeightOffset);

            if (screenPosition.z < 0f)
            {
                _view.SetVisible(false);
                return;
            }

            _view.SetVisible(true);
            _view.SetScreenPosition(screenPosition);
            _view.SetScale(Mathf.Clamp(referenceDistance / screenPosition.z, minScale, maxScale));
        }

        // UI 최신화
        private void Refresh()
        {
            _view.SetFillAmount(_health.Max > 0 ? (float)_health.Current / _health.Max : 0f);
        }

        // 체력바 숨김 API
        public void HideAfterDelay() => _hideAt = Time.time + hideDelayOnDeath;
    }
}
