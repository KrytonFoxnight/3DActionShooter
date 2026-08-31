using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Enemy;
using Enemy.Spawn;
using Game.State;
using Player;
using UI.HudUI;
using UnityEngine;

namespace Game
{
    public class GameDirector : MonoBehaviour
    {
        [SerializeField] private EnemySpawner enemySpawner;

        [SerializeField] private PlayerCharacter player;

        [SerializeField] private Transform enemyTarget;

        [SerializeField] private WaveDefinition[] waves;

        [SerializeField] private HudPresenter hudPresenter;

        [SerializeField] private float spawnInterval = 0.25f;

        [SerializeField] private float wavePrepareDuration = 3f;

        public event Action<WaveStateType> StateChanged;

        private readonly List<EnemyCharacter> _spawned = new List<EnemyCharacter>();

        private WaveStateType _state = WaveStateType.Idle;
        private int? _waveIndex;
        private Coroutine _spawnRoutine;
        private float _prepareEndTime;
        private bool _initialized;

        public WaveStateType CurrentState => _state;

        public int? CurrentWaveNumber => _waveIndex + 1;

        public int AliveEnemyCount => _spawned.Count;

        public int TotalWaveCount => waves?.Length ?? 0;

        public PlayerCharacter Player => player;

        private bool IsSpawning => _spawnRoutine != null;

        private int NextWaveIndex => _waveIndex + 1 ?? 0;

        private bool HasNextWave => waves != null && NextWaveIndex < waves.Length;

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (_initialized) Tick();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private bool Init()
        {
            var spawnerInitResult = enemySpawner != null && enemySpawner.Init();
            var playerResult = player != null;
            var waveResult = ValidateWaves();
            var hudResult = hudPresenter != null && hudPresenter.Init(this);

            if (!spawnerInitResult || !playerResult || !waveResult || !hudResult)
            {
                LogManager.LogError("GameDirector Init Failed\n" +
                                    $"enemySpawner: {spawnerInitResult}\n" +
                                    $"player: {playerResult}\n" +
                                    $"waves: {waveResult}\n" +
                                    $"hudPresenter: {hudResult}", this);

                return false;
            }

            player.Died += OnPlayerDied;

            _initialized = true;

            return true;
        }

        private bool ValidateWaves()
        {
            if (waves == null || waves.Length == 0)
            {
                LogManager.LogError("Wave Data Is Empty", this);

                return false;
            }

            for (var i = 0; i < waves.Length; i++)
            {
                if (waves[i] != null && waves[i].IsValid) continue;

                LogManager.LogError($"Invalid Wave Definition At Index {i}", this);

                return false;
            }

            return true;
        }

        public void StartWave()
        {
            if (!_initialized) return;
            if (_state != WaveStateType.Idle) return;

            ChangeState(WaveStateType.InProgress);
        }

        public void ResetToIdle()
        {
            if (!_initialized) return;
            if (_state == WaveStateType.Idle) return;

            ChangeState(WaveStateType.Idle);
        }

        private void Tick()
        {
            switch (_state)
            {
                case WaveStateType.Idle:
                    break;
                case WaveStateType.InProgress:
                    _spawned.RemoveAll(e => !e || !e.IsAlive);

                    if (IsSpawning) break;
                    if (_spawned.Count > 0) break;

                    ChangeState(HasNextWave ? WaveStateType.Preparing : WaveStateType.Cleared);
                    break;
                case WaveStateType.Preparing:
                    if (Time.time < _prepareEndTime) break;

                    ChangeState(WaveStateType.InProgress);
                    break;
                case WaveStateType.Cleared:
                case WaveStateType.Failed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_state), _state, null);
            }

            hudPresenter.Tick();
        }

        private void ChangeState(WaveStateType next)
        {
            if (_state == next) return;

            _state = next;

            switch (next)
            {
                case WaveStateType.Idle:
                    StopSpawnRoutine();
                    ClearSpawnedEnemies();
                    _waveIndex = null;
                    enemySpawner.ResetState();
                    player.ResetState();
                    break;
                case WaveStateType.InProgress:
                    _waveIndex = NextWaveIndex;
                    _spawned.Clear();
                    _spawnRoutine = StartCoroutine(SpawnWaveRoutine(waves[_waveIndex.Value]));
                    break;
                case WaveStateType.Preparing:
                    _prepareEndTime = Time.time + wavePrepareDuration;
                    break;
                case WaveStateType.Cleared:
                    StopSpawnRoutine();
                    LogManager.Log("All Waves Cleared", this);
                    break;
                case WaveStateType.Failed:
                    StopSpawnRoutine();
                    LogManager.Log($"Wave Failed At Wave {CurrentWaveNumber}", this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(next), next, null);
            }

            StateChanged?.Invoke(next);
        }

        private IEnumerator SpawnWaveRoutine(WaveDefinition wave)
        {
            var wait = spawnInterval > 0f ? new WaitForSeconds(spawnInterval) : null;
            var spawns = wave.Spawns;

            foreach (var request in spawns)
            {
                SpawnOne(request);

                yield return wait;
            }

            _spawnRoutine = null;
        }

        private void SpawnOne(EnemySpawnRequest request)
        {
            var spawned = enemySpawner.Spawn(request);

            if (!spawned) return;

            spawned.SetTarget(enemyTarget);

            _spawned.Add(spawned);
        }

        private void OnPlayerDied()
        {
            if (_state != WaveStateType.InProgress && _state != WaveStateType.Preparing) return;

            ChangeState(WaveStateType.Failed);
        }

        private void StopSpawnRoutine()
        {
            if (_spawnRoutine == null) return;

            StopCoroutine(_spawnRoutine);

            _spawnRoutine = null;
        }

        private void ClearSpawnedEnemies()
        {
            foreach (var enemy in _spawned)
            {
                if (enemy) Destroy(enemy.gameObject);
            }

            _spawned.Clear();
        }

        private void Dispose()
        {
            StopSpawnRoutine();

            if (player != null) player.Died -= OnPlayerDied;

            if (enemySpawner != null && enemySpawner.IsInitialized) enemySpawner.Dispose();

            if (hudPresenter != null && hudPresenter.IsInitialized) hudPresenter.Dispose();

            _spawned.Clear();

            _waveIndex = null;

            _initialized = false;
        }
    }
}
