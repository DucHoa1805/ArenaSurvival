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
            Profile.tampered = false;
            Save();
        }

        public bool Save()
        {
            _dirty = false;
            return _saveService.Save(Profile);
        }

        /// <summary>Gọi khi app chuyển nền / thoát: chỉ ghi nếu có thay đổi.</summary>
        public void SaveIfDirty()
        {
            if (_dirty) Save();
        }
    }
}
