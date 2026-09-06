using Game;
using Player;
using UnityEngine;

namespace UI.DebugFeatureUI
{
    public class DebugFeatureView : MonoBehaviour
    {
        [SerializeField] private GameDirector gameDirector;
        [SerializeField] private PlayerCharacter player;

        public void OnClickWaveStartButton()
        {
            gameDirector.StartWave();
        }

        public void OnClickWaveResetButton()
        {
            gameDirector.ResetToIdle();
        }

        public void OnClickInvincibleButton()
        {
            player.SetInvincible(!player.IsInvincible);
        }
    }
}
