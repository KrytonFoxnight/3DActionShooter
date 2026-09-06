using UnityEngine;

namespace UI.HealthBarUI.EnemyUI.EnemyHpBar
{
    /// <summary>
    /// EnemyHpBarPresenter에서 체력바를 표현하는 View Prefab 컴포넌트
    /// </summary>
    public class EnemyHpBarView : HealthBarViewBase
    {
        // 거리에 따른 체력바 크기 설정 API
        public void SetScale(float scale)
        {
            RectTransform.localScale = Vector3.one * scale;
        }
    }
}
