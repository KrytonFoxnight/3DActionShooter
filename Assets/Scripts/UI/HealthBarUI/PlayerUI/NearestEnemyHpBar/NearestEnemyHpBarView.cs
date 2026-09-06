using TMPro;
using UnityEngine;

namespace UI.HealthBarUI.PlayerUI.NearestEnemyHpBar
{
    public class NearestEnemyHpBarView : HealthBarViewBase
    {
        [SerializeField] private TextMeshProUGUI enemyNameTMPro;

        // enemy 이름 ui 설정 API
        public void SetEnemyNameText(string nameText)
        {
            if (enemyNameTMPro.text == nameText) return;

            enemyNameTMPro.text = nameText;
        }
    }
}
