using UnityEngine;
using UnityEngine.UI;

namespace UI.HealthBarUI
{
    /// <summary>
    /// HealthBarPresenter에서 체력바를 표현하는 View Prefab 컴포넌트
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;               // 현재 체력 상태값을 표현하는 Image

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        // filled 값 설정 API
        public void SetFillAmount(float ratio)
        {
            fillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        // 그리고자 하는 Canvas에서의 위치 설정 API
        public void SetScreenPosition(Vector3 screenPosition)
        {
            _rectTransform.position = screenPosition;
        }

        // 거리에 따른 체력바 크기 설정 API
        public void SetScale(float scale)
        {
            _rectTransform.localScale = Vector3.one * scale;
        }

        // 활성화 여부 API
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;

            gameObject.SetActive(visible);
        }
    }
}
