using System;
using System.Collections.Generic;
using ArenaSurvival.Core;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using ArenaSurvival.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>
    /// Tab Chiến đấu: chương đang xem (◀ ▶ đổi chương), lưới màn của chương (sao, khóa, dấu boss), chi tiết màn đang chọn với
    /// lực chiến đề xuất so với hiện tại, nút vào màn. Mở sẵn chương + màn nên chơi tiếp để người chơi chỉ cần bấm 1 nút.
    /// </summary>
    public class BattleTab : MonoBehaviour
    {
        [SerializeField] private StageCatalog _catalog;
        [SerializeField] private HeroBaseStats _heroBase;
        [SerializeField] private TalentCatalog _talents;

        [Header("UI")]
        [SerializeField] private TMP_Text _chapterText;
        [SerializeField] private Button _prevChapter;
        [SerializeField] private Button _nextChapter;
        [SerializeField] private StageNodeView _nodePrefab;
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private TMP_Text _stageName;
        [SerializeField] private TMP_Text _stageInfo;
        [SerializeField] private TMP_Text _powerText;
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _playLabel;

        private static readonly Color Enough = new Color(0.45f, 0.85f, 0.5f);
        private static readonly Color NotEnough = new Color(0.95f, 0.45f, 0.38f);

        private readonly List<StageNodeView> _nodes = new List<StageNodeView>();
        private StageData _selected;
        private int _chapterIndex = -1;
        private Action<StageNodeView> _onNodeClicked;

        public StageCatalog Catalog => _catalog;

        private void Awake()
        {
            _onNodeClicked = node => Select(node.Stage);
            _playButton.onClick.AddListener(HandlePlay);
            if (_prevChapter != null) _prevChapter.onClick.AddListener(PrevChapter);
            if (_nextChapter != null) _nextChapter.onClick.AddListener(NextChapter);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(HandlePlay);
            if (_prevChapter != null) _prevChapter.onClick.RemoveListener(PrevChapter);
            if (_nextChapter != null) _nextChapter.onClick.RemoveListener(NextChapter);
        }

        private void OnEnable()
        {
            if (_selected == null)
            {
                _selected = GameServices.StageProgress.NextStage(_catalog);
                _chapterIndex = Mathf.Max(0, _catalog.IndexOfChapter(_catalog.ChapterOf(_selected)));
            }
            Refresh();
        }

        private ChapterData CurrentChapter =>
            _catalog.chapters != null && _chapterIndex >= 0 && _chapterIndex < _catalog.chapters.Length ? _catalog.chapters[_chapterIndex] : null;

        /// <summary>Màn của chương đang xem; catalog chưa chia chương (dữ liệu cũ) thì coi cả danh sách là 1 chương.</summary>
        private StageData[] CurrentStages => CurrentChapter != null ? CurrentChapter.stages : _catalog.stages;

        private void Select(StageData stage)
        {
            _selected = stage;
            Refresh();
        }

        private void PrevChapter() => ShowChapter(_chapterIndex - 1);
        private void NextChapter() => ShowChapter(_chapterIndex + 1);

        private void ShowChapter(int index)
        {
            if (_catalog.chapters == null || index < 0 || index >= _catalog.chapters.Length) return;
            _chapterIndex = index;
            // Đổi chương: chọn màn nên chơi tiếp nếu nó thuộc chương này, không thì màn đầu chương.
            StageData next = GameServices.StageProgress.NextStage(_catalog);
            _selected = Array.IndexOf(CurrentStages, next) >= 0 ? next : CurrentStages[0];
            Refresh();
        }

        private void Refresh()
        {
            StageData[] stages = CurrentStages ?? Array.Empty<StageData>();
            ChapterData chapter = CurrentChapter;
            // Ô màn tạo thêm khi thiếu, ẩn khi thừa — các chương có số màn khác nhau dùng chung 1 lưới.
            while (_nodes.Count < stages.Length)
            {
                StageNodeView node = Instantiate(_nodePrefab, _nodeContainer);
                node.OnClicked += _onNodeClicked;
                _nodes.Add(node);
            }

            StageProgressService progress = GameServices.StageProgress;
            for (int i = 0; i < _nodes.Count; i++)
            {
                bool used = i < stages.Length && stages[i] != null;
                _nodes[i].gameObject.SetActive(used);
                if (!used) continue;
                StageData stage = stages[i];
                bool chapterBoss = stage.boss != null && i == stages.Length - 1;
                _nodes[i].Show(stage, progress.IsUnlocked(_catalog, stage), progress.GetStars(stage.id), stage == _selected, chapterBoss);
            }

            int chapters = _catalog.chapters != null ? _catalog.chapters.Length : 0;
            if (_chapterText != null)
                _chapterText.text = chapter != null ? $"CHƯƠNG {chapter.number} · {chapter.displayName.ToUpperInvariant()}" : "CHƯƠNG 1";
            if (_prevChapter != null) _prevChapter.gameObject.SetActive(chapters > 1 && _chapterIndex > 0);
            if (_nextChapter != null) _nextChapter.gameObject.SetActive(chapters > 1 && _chapterIndex < chapters - 1);
            if (_selected == null) return;

            bool unlocked = progress.IsUnlocked(_catalog, _selected);
            _stageName.text = $"Màn {_selected.displayName}";
            int waves = _selected.waves != null ? _selected.waves.Length : 0;
            _stageInfo.text = _selected.boss != null
                ? $"{waves} đợt quái + boss {_selected.boss.displayName}"
                : $"{waves} đợt quái + đợt tinh anh";

            int power = LoadoutStats.Power(_heroBase, _talents);
            _powerText.text = $"Lực chiến {power} / đề xuất {_selected.recommendedPower}";
            _powerText.color = power >= _selected.recommendedPower ? Enough : NotEnough;

            _playButton.interactable = unlocked;
            _playLabel.text = unlocked ? "CHIẾN ĐẤU" : "CHƯA MỞ";
        }

        private void HandlePlay()
        {
            if (_selected == null || !GameServices.StageProgress.IsUnlocked(_catalog, _selected)) return;
            // Nạp scene là bất đồng bộ: khóa nút để bấm 2 lần không nạp scene Game 2 lần.
            _playButton.interactable = false;
            StageSelection.Select(_selected);
            SceneLoader.LoadGame();
        }
    }
}
