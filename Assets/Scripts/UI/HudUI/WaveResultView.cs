using TMPro;
using UnityEngine;

namespace UI.HudUI
{
    public class WaveResultView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI resultTMPro;

        [SerializeField] private Color clearedColor = new Color(0.35f, 0.85f, 1f);
        [SerializeField] private Color failedColor = new Color(1f, 0.35f, 0.35f);

        public void Show(string resultText, bool cleared)
        {
            if (resultTMPro)
            {
                resultTMPro.text = resultText;
                resultTMPro.color = cleared ? clearedColor : failedColor;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            var target = root ? root : gameObject;

            if (target.activeSelf == visible) return;

            target.SetActive(visible);
        }
    }
}
