using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Save;
using ArenaSurvival.Stages;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Điểm truy cập các dịch vụ sống suốt game. Dịch vụ là class C# thường (test được); đây chỉ là nơi tạo và giữ chúng.
    /// Khởi tạo ở scene Boot, hoặc tự khởi tạo trước scene đầu tiên — nên bấm Play thẳng ở scene Game trong Editor vẫn có dịch vụ.
    /// Phase 11–12 thêm IAP, quảng cáo, analytics, remote config vào cùng chỗ này.
    /// </summary>
    public static class GameServices
    {
        public static ProfileService Profile { get; private set; }
        public static EconomyService Economy { get; private set; }
        public static StageProgressService StageProgress { get; private set; }
        public static TalentService Talents { get; private set; }
        public static InventoryService Inventory { get; private set; }
        public static WeaponService Weapons { get; private set; }
        public static AccountService Account { get; private set; }
        public static bool IsInitialized => Profile != null;

        // Gán sẵn 1 lần để gỡ được đúng delegate khi khởi tạo lại.
        private static System.Action<StageResult> _onStageCompleted;

        // Editor có thể tắt "domain reload" khi vào Play mode: khi đó biến static còn nguyên từ lần Play trước
        // (trỏ vào hồ sơ cũ, object đã bị hủy). Xóa sạch trước mọi thứ khác để mỗi lần Play bắt đầu như mở game thật.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (_onStageCompleted != null) StageEvents.OnStageCompleted -= _onStageCompleted;
            Profile = null;
            Economy = null;
            StageProgress = null;
            Talents = null;
            Inventory = null;
            Weapons = null;
            Account = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize() => Initialize();

        /// <summary>Gọi lại nhiều lần không sao: đã khởi tạo thì bỏ qua.</summary>
        public static void Initialize(string savePath = null)
        {
            if (IsInitialized) return;

            Profile = new ProfileService(new SaveService(savePath ?? SaveService.DefaultPath));
            Profile.Load();
            Economy = new EconomyService(Profile.Profile, Profile.MarkDirty);
            Talents = new TalentService(Profile.Profile, Economy, () => Profile.Save());
            ItemDatabase database = LoadItemDatabase();
            Inventory = new InventoryService(Profile.Profile, Economy, database, () => Profile.Save());
            Weapons = new WeaponService(Profile.Profile, Economy, database, () => Profile.Save());
            Account = new AccountService(Profile.Profile, Economy, LoadAccountTable());
            StageProgress = new StageProgressService(Profile.Profile, Economy, () => Profile.Save(), Inventory, Account);

            if (_onStageCompleted == null) _onStageCompleted = result => StageProgress.Apply(result);
            StageEvents.OnStageCompleted -= _onStageCompleted;
            StageEvents.OnStageCompleted += _onStageCompleted;

            ServicesHost.Create();
        }

        private static AccountLevelTable LoadAccountTable()
        {
            var table = Resources.Load<AccountLevelTable>(AccountLevelTable.ResourcePath);
            if (table != null) return table;
            // Thiếu bảng thì mọi tính năng coi như mở từ đầu — game vẫn chơi được, chỉ mất phần mở khóa dần.
            Debug.LogWarning($"GameServices: không tìm thấy Resources/{AccountLevelTable.ResourcePath}; mọi tính năng mở từ đầu.");
            return ScriptableObject.CreateInstance<AccountLevelTable>();
        }

        private static ItemDatabase LoadItemDatabase()
        {
            var database = Resources.Load<ItemDatabase>(ItemDatabase.ResourcePath);
            if (database != null) return database;

            // Thiếu asset thì vẫn chạy được (kho rỗng, mọi món trong save được giữ nguyên nhưng không tính chỉ số).
            Debug.LogWarning($"GameServices: không tìm thấy Resources/{ItemDatabase.ResourcePath}; kho trang bị tạm thời trống.");
            return ScriptableObject.CreateInstance<ItemDatabase>();
        }
    }
}
