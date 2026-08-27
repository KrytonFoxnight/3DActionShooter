using Combat;
using UnityEngine;

namespace UI.HealthBarUI.PlayerUI.NearestEnemyHpBar
{
    public class NearestEnemyHpBarPresenter : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NearestEnemyHpBarView view;

        [Header("Tracking")]
        [SerializeField] private Transform anchor;
        [SerializeField] private float worldHeightOffset = 2.2f;

        private Health _health;
        private Camera _camera;

        private bool _initialized;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init()
        {
            if (_initialized) return true;
            if (!view || !anchor) return false;

            var mainCamera = Camera.main;
            if (!mainCamera) return false;

            _camera = mainCamera;

            view.SetVisible(false);

            _initialized = true;
            return true;
        }

        public void Dispose()
        {
            if (_health != null) _health.Changed -= Refresh;

            _health = null;
            _camera = null;
            _initialized = false;
        }

        #endregion

        // 표시 대상 교체 API
        public void SetTarget(Health health, string displayName)
        {
            if (!_initialized) return;
            if (ReferenceEquals(_health, health)) return;

            if (_health != null) _health.Changed -= Refresh;

            _health = health;

            if (_health != null) _health.Changed += Refresh;

            view.SetEnemyNameText(displayName);

            Refresh();
        }

        private void LateUpdate()
        {
            if (!_initialized) return;

            if (_health == null)
            {
                view.SetVisible(false);
                return;
            }

            var screenPosition = _camera.WorldToScreenPoint(anchor.position + Vector3.up * worldHeightOffset);

            if (screenPosition.z < 0f)
            {
                view.SetVisible(false);
                return;
            }

            view.SetVisible(true);
            view.SetScreenPosition(screenPosition);
        }

        // UI 최신화
        private void Refresh()
        {
            if (_health == null) return;

            view.SetFillAmount(_health.Max > 0 ? (float)_health.Current / _health.Max : 0f);
        }
    }
}
