using UnityEngine;
using UnityEngine.UI;

namespace UI.HealthBarUI
{
    /// <summary>
    /// 체력바 View들이 공통으로 갖는 표현 기능
    /// </summary>
    public abstract class HealthBarViewBase : MonoBehaviour
    {
        [SerializeField] private Image fillImage;               // 현재 체력 상태값을 표현하는 Image

        private RectTransform _rectTransform;

        protected RectTransform RectTransform => _rectTransform;

        protected virtual void Awake()
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

        // 활성화 여부 API
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;

            gameObject.SetActive(visible);
        }
    }
}
