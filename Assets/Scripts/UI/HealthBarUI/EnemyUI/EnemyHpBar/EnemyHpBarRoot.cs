using System.Collections.Generic;
using UnityEngine;

namespace UI.HealthBarUI.EnemyUI.EnemyHpBar
{
    public class EnemyHpBarRoot : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private EnemyHpBarView viewPrefab;

        [Header("Trim")]
        [SerializeField] private bool trimEnabled = true;
        [SerializeField] private float trimInterval = 10f;
        [SerializeField] private int maxIdleCount = 8;

        private List<EnemyHpBarView> _stocks = new List<EnemyHpBarView>();
        private List<EnemyHpBarView> _borrows = new List<EnemyHpBarView>();

        private float _nextTrimTime;

        private void Update()
        {
            if (!trimEnabled || trimInterval <= 0f) return;
            if (Time.time < _nextTrimTime) return;

            _nextTrimTime = Time.time + trimInterval;

            Trim();
        }

        public EnemyHpBarView GetEnemyHpBarView()
        {
            var view = Rent() ?? Create();
            if (!view) return null;

            view.transform.SetAsFirstSibling();
            view.SetVisible(true);

            _borrows.Add(view);

            return view;
        }

        public void ReturnEnemyHpBarView(EnemyHpBarView view)
        {
            if (!view) return;
            if (!_borrows.Remove(view)) return;

            view.SetVisible(false);

            _stocks.Add(view);
        }

        private void Trim()
        {
            for (var i = _stocks.Count - 1; i >= maxIdleCount; i--)
            {
                var view = _stocks[i];
                _stocks.RemoveAt(i);

                if (view) Destroy(view.gameObject);
            }
        }

        private EnemyHpBarView Create()
        {
            if (!viewPrefab || !canvas) return null;

            return Instantiate(viewPrefab, canvas.transform);
        }

        private EnemyHpBarView Rent()
        {
            var last = _stocks.Count - 1;
            if (last < 0) return null;

            var view = _stocks[last];
            _stocks.RemoveAt(last);

            return view;
        }
    }
}
