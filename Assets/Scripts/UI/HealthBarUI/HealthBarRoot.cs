using System.Collections.Generic;
using UnityEngine;

namespace UI.HealthBarUI
{
    public class HealthBarRoot : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private HealthBarView viewPrefab;

        [Header("Trim")]
        [SerializeField] private bool trimEnabled = true;
        [SerializeField] private float trimInterval = 10f;
        [SerializeField] private int maxIdleCount = 8;

        private List<HealthBarView> _stocks = new List<HealthBarView>();
        private List<HealthBarView> _borrows = new List<HealthBarView>();

        private float _nextTrimTime;

        private void Update()
        {
            if (!trimEnabled || trimInterval <= 0f) return;
            if (Time.time < _nextTrimTime) return;

            _nextTrimTime = Time.time + trimInterval;

            Trim();
        }

        public HealthBarView GetHealthBarView()
        {
            var view = Rent() ?? Create();
            if (!view) return null;

            view.transform.SetAsFirstSibling();
            view.SetVisible(true);

            _borrows.Add(view);

            return view;
        }

        public void ReturnHealthBarView(HealthBarView view)
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

        private HealthBarView Create()
        {
            if (!viewPrefab || !canvas) return null;

            return Instantiate(viewPrefab, canvas.transform);
        }

        private HealthBarView Rent()
        {
            var last = _stocks.Count - 1;
            if (last < 0) return null;

            var view = _stocks[last];
            _stocks.RemoveAt(last);

            return view;
        }
    }
}
