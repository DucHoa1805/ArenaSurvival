using System;
using System.Collections;
using System.Collections.Generic;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.Enemies;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Điều phối 1 màn: các đợt thường theo thời gian → đợt tinh anh → thắng/thua, hồi sinh, chấm sao, tính thưởng.
    /// Kết quả trả ra ngoài DUY NHẤT qua StageResult (OnStageEnded): scene Game không ghi gì vào save.
    /// Phép tính nằm ở StageRules (hàm thuần); GameManager chỉ đổi trạng thái theo lệnh của class này.
    /// </summary>
    public class StageSession : MonoBehaviour
    {
        private enum Phase { Waves, FinalWave, Ended }

        [Header("References")]
        [SerializeField] private StageCatalog _catalog;
        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private RunStats _runStats;
        [SerializeField] private Health _playerHealth;

        [Header("Hồi sinh")]
        [SerializeField, Range(0.1f, 1f)] private float _reviveHealthFraction = 0.5f;
        [SerializeField] private float _reviveInvulnerability = 2f;
        [Tooltip("Quái trong bán kính này bị đẩy ra mép, để Player vừa hồi sinh không chết lại ngay.")]
        [SerializeField] private float _revivePushRadius = 3f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Kết thúc")]
        [Tooltip("Chờ bấy nhiêu giây (thời gian thực) sau khi con quái cuối chết rồi mới hiện kết quả, để người chơi kịp thấy nó nổ.")]
        [SerializeField] private float _victoryDelay = 1f;

        public StageData Stage { get; private set; }
        public StageCatalog Catalog => _catalog;
        public int CurrentWave { get; private set; }        // 1-based; đợt cuối = TotalWaves
        public int TotalWaves => Stage != null && Stage.waves != null ? Stage.waves.Length + 1 : 0;
        public bool IsFinalWave => _phase != Phase.Waves;
        public bool ReviveAvailable => !_reviveUsed;
        public StageResult Result { get; private set; }

        /// <summary>Tiến độ màn 0..1 theo thời gian các đợt thường; đợt cuối = 1.</summary>
        public float Progress => IsFinalWave ? 1f : StageRules.Progress(_waveTimeTotal, Stage != null ? Stage.PlannedDuration : 0f);

        public event Action<int, int> OnWaveStarted;     // (đợt hiện tại, tổng số đợt)
        public event Action OnFinalWaveStarted;
        public event Action OnReviveOffered;
        public event Action<StageResult> OnStageEnded;

        private Phase _phase;
        private float _difficulty;
        private int _waveIndex;
        private float _waveTimer;
        private float _waveTimeTotal;
        private bool _reviveUsed;
        private bool _paused;

        private readonly List<(EnemyData data, EnemyModifiers mods)> _eliteQueue = new List<(EnemyData, EnemyModifiers)>();
        private int _nextElite;
        private float _eliteTimer;
        private float _clearedTimer = -1f;

        private readonly Collider2D[] _pushBuffer = new Collider2D[128];
        private ContactFilter2D _enemyFilter;

        private void OnEnable() => _playerHealth.OnDied += HandlePlayerDied;

        private void OnDisable() => _playerHealth.OnDied -= HandlePlayerDied;

        private void Start()
        {
            Stage = StageSelection.Current != null ? StageSelection.Current : _catalog.First;
            if (Stage == null)
            {
                Debug.LogError($"{nameof(StageSession)}: không có màn nào để chơi (StageCatalog trống).", this);
                enabled = false;
                return;
            }

            _enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = _enemyLayer, useTriggers = true };
            _difficulty = StageRules.DifficultyMultiplier(Stage.stageNumber);
            _waveIndex = -1;
            StartNextWave();
        }

        private void Update()
        {
            if (_phase == Phase.Ended || _paused) return;

            // deltaTime (bị timeScale ảnh hưởng): Pause / lên cấp / chờ hồi sinh thì đồng hồ màn đứng yên.
            float dt = Time.deltaTime;

            if (_phase == Phase.Waves)
            {
                _waveTimer += dt;
                _waveTimeTotal += dt;
                if (_waveTimer >= Stage.waves[_waveIndex].duration) StartNextWave();
                return;
            }

            UpdateFinalWave(dt);
        }

        private void StartNextWave()
        {
            _waveIndex++;
            _waveTimer = 0f;

            if (Stage.waves == null || _waveIndex >= Stage.waves.Length)
            {
                BeginFinalWave();
                return;
            }

            _spawner.SetWave(Stage.waves[_waveIndex], EnemyModifiers.ForDifficulty(_difficulty));
            CurrentWave = _waveIndex + 1;
            OnWaveStarted?.Invoke(CurrentWave, TotalWaves);
        }

        private void BeginFinalWave()
        {
            _phase = Phase.FinalWave;
            _spawner.StopSpawning();
            CurrentWave = TotalWaves;

            // Dựng sẵn hàng đợi 1 lần ở đầu đợt cuối, rồi rải dần từng con: sinh cùng lúc cả nhóm thì quái chồng lên nhau.
            _eliteQueue.Clear();
            if (Stage.finalWave != null)
            {
                foreach (EliteGroup group in Stage.finalWave)
                {
                    if (group?.enemy == null) continue;
                    var mods = new EnemyModifiers
                    {
                        health = _difficulty * group.healthMultiplier,
                        damage = _difficulty,
                        scale = group.scaleMultiplier,
                        tint = group.tint,
                    };
                    for (int i = 0; i < group.count; i++) _eliteQueue.Add((group.enemy, mods));
                }
            }

            _nextElite = 0;
            _eliteTimer = Stage.eliteSpawnInterval; // con đầu tiên ra ngay
            OnWaveStarted?.Invoke(CurrentWave, TotalWaves);
            OnFinalWaveStarted?.Invoke();
        }

        private void UpdateFinalWave(float dt)
        {
            if (_nextElite < _eliteQueue.Count)
            {
                _eliteTimer += dt;
                if (_eliteTimer >= Stage.eliteSpawnInterval)
                {
                    _eliteTimer = 0f;
                    var (data, mods) = _eliteQueue[_nextElite++];
                    _spawner.Spawn(data, mods);
                }
                return;
            }

            if (_spawner.ActiveCount > 0)
            {
                _clearedTimer = -1f;
                return;
            }

            // Chỉ đếm khi đang chơi: không để màn tự thắng giữa lúc bảng lên cấp hay bảng hồi sinh đang mở.
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_clearedTimer < 0f) _clearedTimer = 0f;
            // Thời gian thực: hiệu ứng nổ của con cuối vẫn chạy, nhưng không phụ thuộc slow-motion nếu sau này có.
            _clearedTimer += Time.unscaledDeltaTime;
            if (_clearedTimer >= _victoryDelay) EndStage(true);
        }

        private void HandlePlayerDied()
        {
            if (_phase == Phase.Ended) return;

            if (ReviveAvailable)
            {
                GameManager.Instance.BeginRevivePrompt();
                OnReviveOffered?.Invoke();
            }
            else
            {
                EndStage(false);
            }
        }

        /// <summary>Người chơi chọn hồi sinh ở bảng hồi sinh.</summary>
        public void Revive()
        {
            if (_phase == Phase.Ended || _reviveUsed || !_playerHealth.IsDead) return;

            _reviveUsed = true;
            _playerHealth.Revive(_reviveHealthFraction);
            PushEnemiesAway();
            StartCoroutine(ReviveInvulnerability());
            GameManager.Instance.Revive();
        }

        /// <summary>Người chơi chọn bỏ cuộc ở bảng hồi sinh.</summary>
        public void GiveUp() => EndStage(false);

        private void PushEnemiesAway()
        {
            Vector2 center = _playerHealth.transform.position;
            int count = Physics2D.OverlapCircle(center, _revivePushRadius, _enemyFilter, _pushBuffer);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D rb = _pushBuffer[i].attachedRigidbody;
                if (rb == null) continue;

                Vector2 away = rb.position - center;
                if (away.sqrMagnitude < 0.0001f) away = UnityEngine.Random.insideUnitCircle;
                rb.position = center + away.normalized * _revivePushRadius;
            }
        }

        private IEnumerator ReviveInvulnerability()
        {
            _playerHealth.Invulnerable = true;
            yield return new WaitForSeconds(_reviveInvulnerability);
            _playerHealth.Invulnerable = false;
        }

        private void EndStage(bool victory)
        {
            if (_phase == Phase.Ended) return;
            _phase = Phase.Ended;
            _spawner.StopSpawning();

            float hpFraction = victory && _playerHealth.MaxHealth > 0f ? _playerHealth.CurrentHealth / _playerHealth.MaxHealth : 0f;
            float clearTime = _runStats.SurvivalTime;
            bool[] stars = StageRules.EvaluateStars(victory, hpFraction, clearTime, Stage.targetTime);
            var (gold, xp) = StageRules.CalculateRewards(Stage.baseGold, Stage.baseAccountXp, Stage.stageNumber, victory,
                StageRules.Progress(_waveTimeTotal, Stage.PlannedDuration));

            int starCount = 0;
            foreach (bool s in stars) if (s) starCount++;

            Result = new StageResult
            {
                stageId = Stage.id,
                victory = victory,
                starFlags = stars,
                starCount = starCount,
                clearTime = clearTime,
                killCount = _runStats.KillCount,
                hpFraction = hpFraction,
                usedRevive = _reviveUsed,
                gold = gold,
                accountXp = xp,
            };

            GameManager.Instance.EndStage(victory);
            OnStageEnded?.Invoke(Result);
        }

        /// <summary>StressTest dùng để màn không tự chuyển đợt hay kết thúc trong lúc đo.</summary>
        public void PauseProgression(bool paused) => _paused = paused;

        /// <summary>Chỉ dùng khi test (Editor / Development build): nhảy thẳng tới đợt tinh anh.</summary>
        public void DebugSkipToFinalWave()
        {
            if (!Debug.isDebugBuild || _phase != Phase.Waves) return;
            _waveTimeTotal = Stage.PlannedDuration;
            BeginFinalWave();
        }
    }
}
