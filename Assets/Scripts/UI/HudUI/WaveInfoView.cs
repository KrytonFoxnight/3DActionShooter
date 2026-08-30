using TMPro;
using UnityEngine;

namespace UI.HudUI
{
    public class WaveInfoView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI waveTMPro;
        [SerializeField] private TextMeshProUGUI enemyCountTMPro;
        [SerializeField] private TextMeshProUGUI statusTMPro;

        public void SetWaveText(int? waveNumber, int totalWaveCount)
        {
            if (!waveTMPro) return;

            waveTMPro.text = waveNumber.HasValue ? $"WAVE {waveNumber.Value} / {totalWaveCount}" : "WAVE - / -";
        }

        public void SetEnemyCountText(int count)
        {
            if (!enemyCountTMPro) return;

            enemyCountTMPro.text = $"ENEMIES {count}";
        }

        public void SetStatusText(string statusText)
        {
            if (!statusTMPro) return;
            if (statusTMPro.text == statusText) return;

            statusTMPro.text = statusText;
        }
    }
}
