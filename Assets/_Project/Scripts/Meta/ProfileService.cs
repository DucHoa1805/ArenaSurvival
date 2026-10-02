using ArenaSurvival.Save;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>Giữ hồ sơ đang dùng và quyết định khi nào ghi xuống đĩa (SaveService lo cách ghi an toàn).</summary>
    public class ProfileService
    {
        private readonly SaveService _saveService;
        private bool _dirty;

        public PlayerProfile Profile { get; private set; }
        public SaveLoadStatus LastLoadStatus { get; private set; }
        public string SavePath => _saveService.FilePath;

        /// <summary>Sau mỗi lần ghi đĩa thành công — Cloud Save (12B) đánh dấu cần tải lên.</summary>
        public event System.Action OnSaved;

        public ProfileService(SaveService saveService)
        {
            _saveService = saveService;
        }

        public void Load()
        {
            SaveLoadResult result = _saveService.Load();
            Profile = result.Profile;
            LastLoadStatus = result.Status;
            Debug.Log($"[ProfileService] Tải hồ sơ: {result.Status}.");

            // Vừa migrate hoặc khôi phục: ghi lại ngay ở định dạng mới, để file chính trở lại hợp lệ.
            if (result.Status == SaveLoadStatus.Migrated || result.Status == SaveLoadStatus.RestoredFromBackup) Save();
        }

        public void MarkDirty() => _dirty = true;

        /// <summary>
        /// Về trạng thái mới chơi (menu dev). Xóa TẠI CHỖ chứ không tạo PlayerProfile mới: EconomyService và StageProgressService
        /// đang giữ tham chiếu tới object này, tạo mới thì chúng vẫn trỏ vào hồ sơ cũ.
        /// </summary>
        public void ResetToNew()
        {
            Profile.saveVersion = PlayerProfile.CurrentVersion;
            Profile.accountXp = 0;
            Profile.currencies.Clear();
            Profile.stageStars.Clear();
            Profile.talents.Clear();
            Profile.items.Clear();
            Profile.equipped.Clear();
            Profile.nextItemUid = 1;
            Profile.weapons.Clear();
            WeaponService.EnsureDefaults(Profile);
            Profile.processedTransactions.Clear();
            Profile.purchaseCounts.Clear();
            Profile.removeAds = false;
            Profile.adViews.Clear();
            Profile.adDay = 0;
            Profile.consent = -1;
            // lastSeenUtcTicks giữ nguyên: xóa hồ sơ không được là cách lách chống chỉnh giờ.
            Profile.daily = new DailyMissionState();
            Profile.login = new LoginRewardState();
            Profile.tampered = false;
            Save();
        }

        public bool Save()
        {
            _dirty = false;
            bool ok = _saveService.Save(Profile);
            if (ok) OnSaved?.Invoke();
            return ok;
        }

        /// <summary>
        /// Thay toàn bộ dữ liệu bằng hồ sơ khác (lấy bản Cloud Save) nhưng giữ NGUYÊN object Profile: các dịch vụ đang giữ tham chiếu tới nó.
        /// Collection con được thay mới (không gộp) để không sót món của bản cũ. Không tự lưu — nơi gọi lưu sau khi gộp xong.
        /// </summary>
        public void ReplaceWith(PlayerProfile other)
        {
            if (other == null || ReferenceEquals(other, Profile)) return;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(other);
            var settings = new Newtonsoft.Json.JsonSerializerSettings { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace };
            Newtonsoft.Json.JsonConvert.PopulateObject(json, Profile, settings);
            _dirty = true;
        }

        /// <summary>Gọi khi app chuyển nền / thoát: chỉ ghi nếu có thay đổi.</summary>
        public void SaveIfDirty()
        {
            if (_dirty) Save();
        }
    }
}
