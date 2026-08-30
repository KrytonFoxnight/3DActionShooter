using Combat;
using Core;
using Game;
using Game.State;
using UI.HealthBarUI.PlayerUI.PlayerHpBar;
using UnityEngine;

namespace UI.HudUI
{
    public class HudPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private PlayerHpBarView playerHpBarView;
        [SerializeField] private WaveInfoView waveInfoView;
        [SerializeField] private WaveResultView waveResultView;

        private GameDirector _director;
        private Health _playerHealth;

        private int _lastEnemyCount = -1;
        private int? _lastWaveNumber;

        private bool _initialized;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(GameDirector director)
        {
            if (_initialized) return true;

            var directorResult = director != null;
            var playerHpBarResult = playerHpBarView != null;
            var waveInfoResult = waveInfoView != null;
            var waveResultResult = waveResultView != null;

            if (!directorResult || !playerHpBarResult || !waveInfoResult || !waveResultResult)
            {
                LogManager.LogError("HudPresenter Init Failed\n" +
                                    $"director: {directorResult}\n" +
                                    $"playerHpBarView: {playerHpBarResult}\n" +
                                    $"waveInfoView: {waveInfoResult}\n" +
                                    $"waveResultView: {waveResultResult}", this);

                return false;
            }

            _director = director;
            _director.StateChanged += OnStateChanged;

            _initialized = true;

            OnStateChanged(_director.CurrentState);

            return true;
        }

        public void Dispose()
        {
            if (_director != null) _director.StateChanged -= OnStateChanged;

            UnbindPlayerHealth();

            _director = null;
            _initialized = false;
        }

        #endregion

        #region Player

        public void BindPlayerHealth(Health health)
        {
            if (ReferenceEquals(_playerHealth, health)) return;

            UnbindPlayerHealth();

            _playerHealth = health;

            if (_playerHealth == null) return;

            _playerHealth.Changed += RefreshPlayerHp;

            if (playerHpBarView) playerHpBarView.SetVisible(true);

            RefreshPlayerHp();
        }

        public void UnbindPlayerHealth()
        {
            if (_playerHealth == null) return;

            _playerHealth.Changed -= RefreshPlayerHp;
            _playerHealth = null;
        }

        private void RefreshPlayerHp()
        {
            if (_playerHealth == null || !playerHpBarView) return;

            playerHpBarView.SetFillAmount(_playerHealth.Max > 0 ? (float)_playerHealth.Current / _playerHealth.Max : 0f);
            playerHpBarView.SetHpValueText(_playerHealth.Current, _playerHealth.Max);
        }

        #endregion

        #region Wave

        public void Tick()
        {
            if (!_initialized) return;

            RefreshWaveInfo(false);
        }

        private void RefreshWaveInfo(bool force)
        {
            var waveNumber = _director.CurrentWaveNumber;
            if (force || waveNumber != _lastWaveNumber)
            {
                _lastWaveNumber = waveNumber;
                waveInfoView.SetWaveText(waveNumber, _director.TotalWaveCount);
            }

            var enemyCount = _director.AliveEnemyCount;
            if (force || enemyCount != _lastEnemyCount)
            {
                _lastEnemyCount = enemyCount;
                waveInfoView.SetEnemyCountText(enemyCount);
            }
        }

        private void OnStateChanged(WaveStateType state)
        {
            switch (state)
            {
                case WaveStateType.Idle:
                    waveResultView.Hide();
                    waveInfoView.SetStatusText("PRESS WAVE START");
                    break;
                case WaveStateType.InProgress:
                    waveResultView.Hide();
                    waveInfoView.SetStatusText("IN PROGRESS");
                    break;
                case WaveStateType.Preparing:
                    waveResultView.Hide();
                    waveInfoView.SetStatusText("NEXT WAVE INCOMING");
                    break;
                case WaveStateType.Cleared:
                    waveInfoView.SetStatusText(string.Empty);
                    waveResultView.Show("ALL WAVES CLEARED", true);
                    break;
                case WaveStateType.Failed:
                    waveInfoView.SetStatusText(string.Empty);
                    waveResultView.Show("DEFEATED", false);
                    break;
            }

            RefreshWaveInfo(true);
        }

        #endregion
    }
}
