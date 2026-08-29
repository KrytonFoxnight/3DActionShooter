using Game;
using UnityEngine;

namespace UI.DebugFeatureUI
{
    public class DebugFeatureView : MonoBehaviour
    {
        [SerializeField] private GameDirector gameDirector;

        public void OnClickWaveStartButton()
        {
            gameDirector.StartWave();
        }

        public void OnClickWaveResetButton()
        {
            gameDirector.ResetToIdle();
        }
    }
}
