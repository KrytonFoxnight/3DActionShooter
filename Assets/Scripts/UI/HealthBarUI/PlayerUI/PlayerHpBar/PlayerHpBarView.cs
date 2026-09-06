using TMPro;
using UnityEngine;

namespace UI.HealthBarUI.PlayerUI.PlayerHpBar
{
    public class PlayerHpBarView : HealthBarViewBase
    {
        [SerializeField] private TextMeshProUGUI hpValueTMPro;

        public void SetHpValueText(int current, int max)
        {
            if (!hpValueTMPro) return;

            var valueText = $"{current} / {max}";
            if (hpValueTMPro.text == valueText) return;

            hpValueTMPro.text = valueText;
        }
    }
}
