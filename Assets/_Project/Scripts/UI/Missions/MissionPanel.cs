using System;
using System.Collections.Generic;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using ArenaSurvival.Missions;
using ArenaSurvival.UI.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Missions
{
    /// <summary>
    /// Tab Nhiệm vụ (Phase 12): quà đăng nhập 7 ngày, thanh điểm ngày + mốc, danh sách nhiệm vụ ngày. Chỉ hiển thị và chuyển lệnh
    /// nhận cho MissionService; vẽ lại khi dịch vụ báo đổi. Ô/dòng tạo 1 lần từ mẫu (danh sách cố định trong ngày).
    /// </summary>
    public class MissionPanel : MonoBehaviour
    {
        [SerializeField] private LoginDayView _dayPrefab;
        [SerializeField] private Transform _dayRow;
        [SerializeField] private MissionRowView _rowPrefab;
        [SerializeField] private Transform _rowList;
        [SerializeField] private MilestoneView[] _milestones;
        [SerializeField] private Image _pointsFill;
        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private TMP_Text _resetText;
        [SerializeField] private ShopResultPopup _result;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private Sprite _gemIcon;

        private readonly List<LoginDayView> _days = new List<LoginDayView>();
        private readonly List<MissionRowView> _rows = new List<MissionRowView>();
        private readonly List<LootDrop> _granted = new List<LootDrop>();
        private Action _onChanged;
        private int _lastResetSecond = -1;

        private static MissionService Service => GameServices.Missions;

        private void Awake()
        {
            _onChanged = Refresh;
            for (int i = 0; i < _milestones.Length; i++)
            {
                int index = i;
                _milestones[i].OnClicked += () => ClaimMilestone(index);
            }
        }

        private void OnEnable()
        {
            if (!GameServices.IsInitialized) return;
            Build();
            Service.OnChanged += _onChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameServices.IsInitialized) Service.OnChanged -= _onChanged;
        }

        private void Update()
        {
            // Đồng hồ làm mới: cập nhật mỗi giây (1 chuỗi/giây), và sang ngày mới khi đang mở tab thì vẽ lại toàn bộ.
            TimeSpan left = Service.UntilReset;
            int second = (int)left.TotalSeconds;
            if (second == _lastResetSecond) return;
            if (second > _lastResetSecond && _lastResetSecond >= 0) Refresh();
            _lastResetSecond = second;
            _resetText.text = $"Làm mới sau {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        private void Build()
        {
            MissionCatalog catalog = Service.Catalog;
            while (_days.Count < Service.LoginDays)
            {
                LoginDayView day = Instantiate(_dayPrefab, _dayRow);
                day.OnClicked += ClaimLogin;
                _days.Add(day);
            }
            int missionCount = catalog.daily != null ? catalog.daily.Length : 0;
            while (_rows.Count < missionCount)
            {
                MissionRowView row = Instantiate(_rowPrefab, _rowList);
                row.OnClaimClicked += ClaimMission;
                _rows.Add(row);
            }
        }

        private void Refresh()
        {
            MissionCatalog catalog = Service.Catalog;

            // Quà đăng nhập: ô < LoginIndex đã nhận trong vòng này; ô LoginIndex là hôm nay nếu còn nhận được, không thì là ngày mai.
            int next = Service.LoginIndex;
            bool canClaim = Service.CanClaimLogin;
            for (int i = 0; i < _days.Count; i++)
            {
                Sprite icon = RewardIcons.Pick(catalog.loginRewards[i], _goldIcon, _gemIcon, out string amount);
                LoginDayView.State state = i < next ? LoginDayView.State.Claimed
                    : i == next && canClaim ? LoginDayView.State.Today : LoginDayView.State.Upcoming;
                _days[i].Bind(i + 1, icon, amount, state);
            }

            int points = Service.Points;
            int max = Mathf.Max(1, catalog.MaxPoints);
            _pointsFill.fillAmount = Mathf.Clamp01(points / (float)max);
            _pointsText.text = $"Điểm hôm nay: {points}/{max}";
            for (int i = 0; i < _milestones.Length; i++)
            {
                bool exists = catalog.milestones != null && i < catalog.milestones.Length;
                _milestones[i].gameObject.SetActive(exists);
                if (!exists) continue;
                MissionMilestone m = catalog.milestones[i];
                Sprite icon = RewardIcons.Pick(m.reward, _goldIcon, _gemIcon, out string amount);
                _milestones[i].Bind(m.points, icon, amount, Service.IsMilestoneClaimed(i), Service.CanClaimMilestone(i));
            }

            // Việc nhận được xếp lên đầu, đã nhận xuống cuối — người chơi thấy ngay việc cần làm.
            int r = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (MissionData mission in catalog.daily)
                {
                    if (mission == null) continue;
                    bool claimed = Service.IsClaimed(mission);
                    int group = Service.CanClaim(mission) ? 0 : claimed ? 2 : 1;
                    if (group != pass) continue;
                    _rows[r].Bind(mission, Service.Progress(mission), claimed);
                    _rows[r].transform.SetSiblingIndex(r);
                    r++;
                }
            }
        }

        private void ClaimLogin()
        {
            int day = Service.LoginIndex + 1;
            _granted.Clear();
            if (Service.TryClaimLogin(_granted)) _result.Show($"Quà đăng nhập ngày {day}", _granted);
        }

        private void ClaimMission(MissionRowView row) => Service.TryClaim(row.Mission);

        private void ClaimMilestone(int index)
        {
            _granted.Clear();
            if (Service.TryClaimMilestone(index, _granted)) _result.Show($"Mốc {Service.Catalog.milestones[index].points} điểm", _granted);
        }
    }
}
