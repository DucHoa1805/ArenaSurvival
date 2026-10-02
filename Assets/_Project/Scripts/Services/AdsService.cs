using System;
using ArenaSurvival.Meta;

namespace ArenaSurvival.Services
{
    /// <summary>Nhà cung cấp quảng cáo (LevelPlay ở 11B; FakeAdProvider ở 11A). onComplete(true) = xem hết, được thưởng.</summary>
    public interface IAdProvider
    {
        bool IsReady { get; }
        void Show(string placement, Action<bool> onComplete);
        /// <summary>Đồng ý quảng cáo cá nhân hóa (GDPR/CCPA) — gọi trước khi hiện quảng cáo đầu tiên.</summary>
        void SetConsent(bool personalized);
    }

    /// <summary>
    /// Quảng cáo có thưởng: đếm lượt mỗi vị trí theo ngày UTC (AdRules), đã mua Xóa quảng cáo thì trao thưởng ngay không cần xem
    /// (vẫn tính lượt để kinh tế không đổi). Kết quả chỉ báo qua callback — nơi gọi tự trao thưởng của mình.
    /// </summary>
    public class AdsService
    {
        private readonly IAdProvider _provider;
        private readonly PlayerProfile _profile;
        private readonly Action _save;
        private readonly Func<DateTime> _utcNow;
        private bool _showing;

        public event Action OnChanged;
        /// <summary>Được thưởng ở vị trí nào (kể cả khi đã mua Xóa quảng cáo) — nhiệm vụ ngày và analytics.</summary>
        public event Action<string> OnRewarded;
        /// <summary>Người chơi đổi lựa chọn quyền riêng tư — analytics (12B) báo lại cho SDK.</summary>
        public event Action<bool> OnConsentChanged;

        public AdsService(IAdProvider provider, PlayerProfile profile, Action save, Func<DateTime> utcNow = null)
        {
            _provider = provider;
            _profile = profile;
            _save = save;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            if (_profile.consent >= 0) _provider.SetConsent(_profile.consent == 1);
        }

        public bool RemoveAds => _profile.removeAds;

        public int ViewsToday(string placement)
        {
            RollDay();
            return _profile.adViews.TryGetValue(placement, out int n) ? n : 0;
        }

        public int Remaining(string placement) => AdRules.Remaining(ViewsToday(placement), AdPlacements.DailyLimit(placement));

        public bool CanShow(string placement) => !_showing && Remaining(placement) > 0 && (RemoveAds || _provider.IsReady);

        public void Show(string placement, Action<bool> onDone)
        {
            if (!CanShow(placement)) { onDone?.Invoke(false); return; }
            if (RemoveAds)
            {
                Count(placement);
                onDone?.Invoke(true);
                return;
            }

            _showing = true;
            _provider.Show(placement, rewarded =>
            {
                _showing = false;
                if (rewarded) Count(placement);
                onDone?.Invoke(rewarded);
            });
        }

        public void SetConsent(bool personalized)
        {
            _profile.consent = personalized ? 1 : 0;
            _provider.SetConsent(personalized);
            _save?.Invoke();
            OnConsentChanged?.Invoke(personalized);
        }

        private void Count(string placement)
        {
            RollDay();
            _profile.adViews[placement] = (_profile.adViews.TryGetValue(placement, out int n) ? n : 0) + 1;
            _save?.Invoke();
            OnRewarded?.Invoke(placement);
            OnChanged?.Invoke();
        }

        private void RollDay()
        {
            int today = AdRules.DayKey(_utcNow());
            if (!AdRules.IsNewDay(_profile.adDay, today)) return;
            _profile.adDay = today;
            _profile.adViews.Clear();
        }
    }
}
