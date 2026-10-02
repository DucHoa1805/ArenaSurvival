using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.Meta;
using ArenaSurvival.RunUpgrades;
using ArenaSurvival.Stages;
using ArenaSurvival.UI;
using ArenaSurvival.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Bot tự chơi cho Development build (thay "chơi thử 30 màn không cheat" — xem spec 10C):
    ///   ArenaSurvival.exe -autoplay -speed 4 -label c123-v1 [-maxRuns 400] -screen-fullscreen 0
    /// Tài khoản mới trên save riêng (autoplay-save.json), vòng lặp Hub (tiêu tiền, chọn màn) ↔ Game (BotPilot lái, tự chọn nâng cấp,
    /// luôn hồi sinh), ghi autoplay-log.csv từng lượt và autoplay-summary.md khi xong (thắng màn cuối / kẹt / hết lượt) rồi thoát.
    /// </summary>
    public class AutoplayRunner : MonoBehaviour
    {
        private const float StageTimeoutSeconds = 900f; // thời gian game: màn kẹt (quái không chết được) thì bỏ, không treo cả lượt chạy
        private const float DeathAfterReviveWindow = 3f;

        private static bool IsAutoplay => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-autoplay") >= 0;

        // Theo dõi từng màn để viết bảng tóm tắt.
        private class StageStats
        {
            public string name;
            public int number;
            public int attempts;
            public int winsAtFirstClear = -1;     // số lượt tới lần thắng đầu
            public int powerAtClear;
            public int recommended;
            public float minutesAtClear;
            public int revives;
            public int deathsAfterRevive;
            public int timeouts;
        }

        private readonly Dictionary<string, StageStats> _stats = new Dictionary<string, StageStats>();
        private readonly List<string> _order = new List<string>();
        private float _speed = 4f;
        private int _maxRuns = 400;
        private string _label = "autoplay";
        private string _logPath, _summaryPath;

        private int _run;
        private float _totalGameSeconds;
        private string _targetId;
        private int _lossesAtTarget;
        private int _lossesSinceFarm;
        private bool _isFarm;
        private string _currentStageId;
        private int _powerBefore;
        private string _stuckStage;
        private bool _finished;

        // Trong màn.
        private StageSession _session;
        private UpgradeManager _upgrades;
        private Health _playerHealth;
        private IReadOnlyList<UpgradeData> _pendingOptions;
        private float _reviveTime = -1f;
        private bool _stageEnded;
        private Action<IReadOnlyList<UpgradeData>> _onOptions;
        private Action<StageResult> _onStageEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void UseBotSave()
        {
            if (!IsAutoplay) return;
            // Chạy trước GameServices.AutoInitialize (BeforeSceneLoad): mỗi lượt chạy là tài khoản mới, save thật không bị chạm.
            string path = AutoplayRules.SavePath(Application.persistentDataPath);
            foreach (string p in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(p)) File.Delete(p);
            GameServices.SavePathOverride = path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (!IsAutoplay) return;
            var go = new GameObject("AutoplayRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoplayRunner>();
        }

        private void Awake()
        {
            _speed = Mathf.Clamp(GetFloatArg("-speed", 4f), 1f, 10f);
            _maxRuns = Mathf.Max(1, (int)GetFloatArg("-maxRuns", 400f));
            _label = GetStringArg("-label", "autoplay");
            _logPath = Path.Combine(Application.persistentDataPath, AutoplayRules.LogFileName);
            _summaryPath = Path.Combine(Application.persistentDataPath, AutoplayRules.SummaryFileName);
            File.WriteAllText(_logPath, "run,stage,result,gameTime,hpPercent,revived,deathAfterRevive,power,recommended,stars,kills,goldGained,totalMinutes,accountLevel,farm,weapon,weaponLevel,stageId,selectedId\n");

            // Cửa sổ mất focus thì build ngừng chạy frame (runInBackground = 0) — bot chạy nền hàng giờ nên phải bật.
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            GameManager.PlayingTimeScale = _speed;
            // Quảng cáo giả lập (11A) xong ngay: bot không ngồi chờ đếm ngược.
            Services.FakeAdProvider.Duration = 0f;
            _onOptions = options => _pendingOptions = options;
            _onStageEnded = HandleStageEnded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Debug.Log($"[Autoplay] bắt đầu '{_label}', tốc độ ×{_speed}, tối đa {_maxRuns} lượt, save {GameServices.SavePathOverride}");
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_finished) return;
            if (scene.name == SceneLoader.MainMenuScene) StartCoroutine(HubRoutine());
            else if (scene.name == SceneLoader.GameScene) StartCoroutine(GameRoutine());
        }

        // ===== Hub =====

        private IEnumerator HubRoutine()
        {
            yield return null;
            yield return null;

            var battle = FindFirstObjectByType<BattleTab>(FindObjectsInactive.Include);
            var talentPanel = FindFirstObjectByType<TalentPanel>(FindObjectsInactive.Include);
            if (battle == null || battle.Catalog == null || talentPanel == null)
            {
                Finish("không tìm thấy BattleTab/TalentPanel trong Hub");
                yield break;
            }
            StageCatalog catalog = battle.Catalog;

            AutoplayHub.SpendAll(talentPanel.Catalog, talentPanel.HeroBase);
            GameServices.Profile.Save();

            StageData last = catalog.stages[catalog.stages.Length - 1];
            if (GameServices.StageProgress.IsCleared(last.id)) { Finish("đã thắng màn cuối"); yield break; }
            if (_run >= _maxRuns) { Finish("hết số lượt tối đa"); yield break; }

            StageData target = GameServices.StageProgress.NextStage(catalog);
            if (target.id != _targetId)
            {
                _targetId = target.id;
                _lossesAtTarget = 0;
                _lossesSinceFarm = 0;
            }

            StageData highestCleared = null;
            foreach (StageData s in catalog.stages) if (s != null && GameServices.StageProgress.IsCleared(s.id)) highestCleared = s;

            StageChoice choice = AutoplayRules.ChooseStage(_lossesAtTarget, _lossesSinceFarm, highestCleared != null);
            if (choice == StageChoice.Stuck)
            {
                _stuckStage = target.displayName;
                Finish($"kẹt ở {target.displayName}");
                yield break;
            }

            _isFarm = choice == StageChoice.Farm;
            StageData stage = _isFarm ? highestCleared : target;
            if (_isFarm) _lossesSinceFarm = 0;

            _currentStageId = stage.id;
            _powerBefore = LoadoutStats.Power(talentPanel.HeroBase, talentPanel.Catalog);
            Track(stage);
            StageSelection.Select(stage);
            SceneLoader.LoadGame();
        }

        private StageStats Track(StageData stage)
        {
            if (!_stats.TryGetValue(stage.id, out StageStats s))
            {
                s = new StageStats { name = stage.displayName, number = stage.stageNumber, recommended = stage.recommendedPower };
                _stats[stage.id] = s;
                _order.Add(stage.id);
            }
            return s;
        }

        // ===== Trong màn =====

        private IEnumerator GameRoutine()
        {
            yield return null;
            _stageEnded = false;
            _pendingOptions = null;
            _reviveTime = -1f;

            _session = FindFirstObjectByType<StageSession>();
            _upgrades = FindFirstObjectByType<UpgradeManager>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (_session == null || player == null)
            {
                Finish("không tìm thấy StageSession/Player trong Game");
                yield break;
            }

            _playerHealth = player.GetComponent<Health>();
            var pilot = player.AddComponent<BotPilot>();
            if (spawner != null) pilot.Configure(spawner.PlayArea);
            if (_upgrades != null) _upgrades.OnOptionsOffered += _onOptions;
            _session.OnStageEnded += _onStageEnded;
        }

        private void Update()
        {
            if (_finished || _session == null || GameManager.Instance == null) return;
            GameState state = GameManager.Instance.CurrentState;

            if (state == GameState.LevelUp && _pendingOptions != null && _upgrades != null)
            {
                // Chọn ở Update chứ không ngay trong event: Select có thể mở lượt chọn kế tiếp, gọi lồng vào chính event đó.
                IReadOnlyList<UpgradeData> options = _pendingOptions;
                _pendingOptions = null;
                _upgrades.Select(PickUpgrade(options));
            }
            else if (state == GameState.RevivePrompt)
            {
                _reviveTime = Time.time;
                _session.Revive();
            }
            else if (state == GameState.Playing && !_stageEnded && Time.timeSinceLevelLoad > StageTimeoutSeconds)
            {
                Track(_session.Stage).timeouts++;
                _session.GiveUp();
            }
        }

        private UpgradeData PickUpgrade(IReadOnlyList<UpgradeData> options)
        {
            float hp = _playerHealth != null && _playerHealth.MaxHealth > 0f ? _playerHealth.CurrentHealth / _playerHealth.MaxHealth : 1f;
            UpgradeData best = options[0];
            int bestRank = int.MaxValue;
            foreach (UpgradeData option in options)
            {
                int rank = AutoplayRules.UpgradeRank(option.type, hp);
                if (rank < bestRank) { bestRank = rank; best = option; }
            }
            return best;
        }

        private void HandleStageEnded(StageResult result)
        {
            if (_stageEnded) return;
            _stageEnded = true;
            if (_upgrades != null) _upgrades.OnOptionsOffered -= _onOptions;
            _session.OnStageEnded -= _onStageEnded;
            // Lấy màn ngay lúc kết thúc: sau khoảng chờ bên dưới, _session có thể đã trỏ sang phiên khác (nhật ký từng ghi nhầm 1-10 thành 2-1).
            StartCoroutine(AfterStage(result, Time.time, _session.Stage));
        }

        private IEnumerator AfterStage(StageResult result, float endTime, StageData stage)
        {
            // Đợi StageProgressService.Apply (cùng frame) và màn kết quả hiện xong, rồi mới đọc phần thưởng và về Hub.
            yield return new WaitForSecondsRealtime(0.5f);

            _run++;
            _totalGameSeconds += result.clearTime + 40f; // + thời gian ngoài màn ước lượng như mô phỏng
            bool deathAfterRevive = !result.victory && result.usedRevive && _reviveTime >= 0f && endTime - _reviveTime < DeathAfterReviveWindow;

            StageStats s = Track(stage);
            s.attempts++;
            if (result.usedRevive) s.revives++;
            if (deathAfterRevive) s.deathsAfterRevive++;

            StageGrant grant = GameServices.StageProgress.LastGrant;
            if (result.victory && s.winsAtFirstClear < 0)
            {
                s.winsAtFirstClear = s.attempts;
                s.powerAtClear = _powerBefore;
                s.minutesAtClear = _totalGameSeconds / 60f;
            }

            if (!_isFarm)
            {
                if (result.victory) { _lossesAtTarget = 0; _lossesSinceFarm = 0; }
                else { _lossesAtTarget++; _lossesSinceFarm++; }
            }

            var ci = CultureInfo.InvariantCulture;
            File.AppendAllText(_logPath, string.Join(",",
                _run, stage.displayName, result.victory ? "win" : "loss", result.clearTime.ToString("0", ci),
                (result.hpFraction * 100f).ToString("0", ci), result.usedRevive ? 1 : 0, deathAfterRevive ? 1 : 0,
                _powerBefore, stage.recommendedPower, result.starCount, result.killCount, grant != null ? grant.gold : 0,
                (_totalGameSeconds / 60f).ToString("0.0", ci), GameServices.Account.Level, _isFarm ? 1 : 0,
                GameServices.Weapons.EquippedId, GameServices.Weapons.GetLevel(GameServices.Weapons.EquippedId),
                stage.id, _currentStageId) + "\n");

            _session = null;
            SceneLoader.LoadMainMenu();
        }

        // ===== Kết thúc =====

        private void Finish(string reason)
        {
            if (_finished) return;
            _finished = true;
            GameManager.PlayingTimeScale = 1f;
            File.WriteAllText(_summaryPath, BuildSummary(reason));
            Debug.Log($"[Autoplay] xong: {reason}");
            Application.Quit();
        }

        private string BuildSummary(string reason)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"# Bot tự chơi — {_label}");
            sb.AppendLine();
            sb.AppendLine($"Kết thúc: {reason}. Tốc độ ×{_speed.ToString(ci)}, {_run} lượt, tổng ~{(_totalGameSeconds / 60f).ToString("0", ci)} phút chơi " +
                          "(thời gian trong màn + 40 s/lượt ngoài màn, như mô phỏng).");
            if (_stuckStage != null) sb.AppendLine($"**Kẹt ở {_stuckStage}.**");
            sb.AppendLine();
            sb.AppendLine("| Màn | Lượt tới khi thắng | Tổng lượt (cả farm) | Lực chiến lúc thắng | Đề xuất | Tỉ lệ | Phút cộng dồn | Hồi sinh | Chết <3 s sau hồi sinh | Quá giờ |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            _order.Sort((a, b) => _stats[a].number.CompareTo(_stats[b].number));
            foreach (string id in _order)
            {
                StageStats s = _stats[id];
                bool cleared = s.winsAtFirstClear > 0;
                string ratio = cleared && s.recommended > 0 ? ((float)s.powerAtClear / s.recommended).ToString("0.00", ci) : "-";
                sb.AppendLine($"| {s.name} | {(cleared ? s.winsAtFirstClear.ToString() : "chưa")} | {s.attempts} | {(cleared ? s.powerAtClear.ToString() : "-")} | " +
                              $"{s.recommended} | {ratio} | {(cleared ? s.minutesAtClear.ToString("0", ci) : "-")} | {s.revives} | {s.deathsAfterRevive} | {s.timeouts} |");
            }
            return sb.ToString();
        }

        private static string GetStringArg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        private static float GetFloatArg(string name, float fallback) =>
            float.TryParse(GetStringArg(name, null), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
    }
}
