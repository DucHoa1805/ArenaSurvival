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
    /// Tab Chiến đấu: tên chương, danh sách màn (sao, khóa), chi tiết màn đang chọn với lực chiến đề xuất so với hiện tại, nút vào màn.
    /// Chọn sẵn màn nên chơi tiếp (màn chưa thắng đầu tiên) để người chơi chỉ cần bấm 1 nút.
    /// </summary>
    public class BattleTab : MonoBehaviour
    {
        [SerializeField] private StageCatalog _catalog;
        [SerializeField] private HeroBaseStats _heroBase;
        [SerializeField] private TalentCatalog _talents;

        [Header("UI")]
        [SerializeField] private TMP_Text _chapterText;
        [SerializeField] private StageNodeView _nodePrefab;
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private TMP_Text _stageName;
        [SerializeField] private TMP_Text _stageInfo;
        [SerializeField] private TMP_Text _powerText;
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _playLabel;

        public StageCatalog Catalog => _catalog;

        private static readonly Color Enough = new Color(0.45f, 0.85f, 0.5f);
        private static readonly Color NotEnough = new Color(0.95f, 0.45f, 0.38f);

        private readonly List<StageNodeView> _nodes = new List<StageNodeView>();
        private StageData _selected;
        private Action<StageNodeView> _onNodeClicked;

        private void Awake()
        {
            _onNodeClicked = node => Select(node.Stage);
            _playButton.onClick.AddListener(HandlePlay);
        }

        private void OnDestroy() => _playButton.onClick.RemoveListener(HandlePlay);

        private void OnEnable()
        {
            BuildOnce();
            if (_selected == null) _selected = GameServices.StageProgress.NextStage(_catalog);
            Refresh();
        }

        private void BuildOnce()
        {
            if (_nodes.Count > 0 || _catalog == null || _catalog.stages == null) return;
            foreach (StageData stage in _catalog.stages)
            {
                if (stage == null) continue;
                StageNodeView node = Instantiate(_nodePrefab, _nodeContainer);
                node.OnClicked += _onNodeClicked;
                _nodes.Add(node);
            }
            // Phase 10 thêm chương 2–3: tên chương sẽ lấy từ ChapterData.
            if (_chapterText != null) _chapterText.text = "CHƯƠNG 1 · SA MẠC";
        }

        private void Select(StageData stage)
        {
            _selected = stage;
            Refresh();
        }

        private void Refresh()
        {
            StageProgressService progress = GameServices.StageProgress;
            for (int i = 0; i < _nodes.Count; i++)
            {
                StageData stage = _catalog.stages[i];
                _nodes[i].Show(stage, progress.IsUnlocked(_catalog, stage), progress.GetStars(stage.id), stage == _selected);
            }
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
