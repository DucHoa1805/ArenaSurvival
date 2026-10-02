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
        public static ArenaSurvival.Shop.ShopService Shop { get; private set; }
        public static ArenaSurvival.Shop.IAPService Iap { get; private set; }
        public static ArenaSurvival.Services.AdsService Ads { get; private set; }
        public static ArenaSurvival.Services.GameClock Clock { get; private set; }
        public static ArenaSurvival.Services.AnalyticsService Analytics { get; private set; }
        public static ArenaSurvival.Services.RemoteConfigService RemoteConfig { get; private set; }
        public static ArenaSurvival.Missions.MissionService Missions { get; private set; }
        /// <summary>Null khi không dùng UGS (Editor/bản dev chưa bật công tắc).</summary>
        public static ArenaSurvival.Services.CloudSyncService Cloud { get; private set; }
        /// <summary>Phiên này dùng Unity Gaming Services thật (bản phát hành luôn bật; Editor/dev bật bằng DevMenu).</summary>
        public static bool UsingUgs { get; private set; }

        /// <summary>PlayerPrefs của công tắc DevMenu "Dùng UGS thật" — đọc lúc khởi tạo nên đổi xong phải mở lại game.</summary>
        public const string DevUseUgsPref = "dev_use_ugs";
        private const string CloudSyncPrefPrefix = "cloud_sync_";
        public static bool IsInitialized => Profile != null;

        /// <summary>
        /// Đường dẫn save thay thế cho lần tự khởi tạo (bot tự chơi dùng save riêng). Đặt ở AfterAssembliesLoaded — chạy sau
        /// ResetStatics và trước AutoInitialize (BeforeSceneLoad).
        /// </summary>
        public static string SavePathOverride;

        // Gán sẵn 1 lần để gỡ được đúng delegate khi khởi tạo lại.
        private static System.Action<StageResult> _onStageCompleted;
        private static ArenaSurvival.Services.AnalyticsTracker _tracker;

        // Editor có thể tắt "domain reload" khi vào Play mode: khi đó biến static còn nguyên từ lần Play trước
        // (trỏ vào hồ sơ cũ, object đã bị hủy). Xóa sạch trước mọi thứ khác để mỗi lần Play bắt đầu như mở game thật.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (_onStageCompleted != null) StageEvents.OnStageCompleted -= _onStageCompleted;
            _tracker?.Disconnect();
            _tracker = null;
            Clock = null;
            Analytics = null;
            RemoteConfig = null;
            Missions = null;
            Cloud = null;
            UsingUgs = false;
            Profile = null;
            SavePathOverride = null;
            Economy = null;
            StageProgress = null;
            Talents = null;
            Inventory = null;
            Weapons = null;
            Account = null;
            Shop = null;
            Iap = null;
            Ads = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize() => Initialize(SavePathOverride);

        /// <summary>Gọi lại nhiều lần không sao: đã khởi tạo thì bỏ qua.</summary>
        public static void Initialize(string savePath = null)
        {
            if (IsInitialized) return;

            string path = savePath ?? SaveService.DefaultPath;
            Profile = new ProfileService(new SaveService(path));
            Profile.Load();
            bool dev = Application.isEditor || Debug.isDebugBuild;
            string folder = System.IO.Path.GetDirectoryName(path);
            // Bot tự chơi dùng save riêng: không bao giờ đẩy lên mây hay gửi analytics thật.
            UsingUgs = savePath == null && (!dev || PlayerPrefs.GetInt(DevUseUgsPref, 0) == 1);

            // Remote Config trước mọi dịch vụ đọc Tuning (giới hạn quảng cáo, giá hồi sinh...). Bản dev không bật UGS: file cạnh save.
            ArenaSurvival.Services.IRemoteConfigProvider remote = UsingUgs
                ? new ArenaSurvival.Services.UgsRemoteConfigProvider()
                : dev
                    ? new ArenaSurvival.Services.FileRemoteConfigProvider(System.IO.Path.Combine(folder, "remote-config.json"))
                    : (ArenaSurvival.Services.IRemoteConfigProvider)new ArenaSurvival.Services.NullRemoteConfigProvider();
            RemoteConfig = new ArenaSurvival.Services.RemoteConfigService(remote, System.IO.Path.Combine(folder, "remote-cache.json"));
            RemoteConfig.Initialize();

            PlayerProfile profile = Profile.Profile;
            Clock = new ArenaSurvival.Services.GameClock(() => profile.lastSeenUtcTicks, ticks => profile.lastSeenUtcTicks = ticks);
            // Môi trường riêng cho Editor/bản dev: số liệu thử không lẫn vào dashboard production.
            if (UsingUgs) ArenaSurvival.Services.UgsBootstrap.Start(dev ? "development" : "production", Clock);
            Economy = new EconomyService(Profile.Profile, Profile.MarkDirty);
            Talents = new TalentService(Profile.Profile, Economy, () => Profile.Save());
            ItemDatabase database = LoadItemDatabase();
            Inventory = new InventoryService(Profile.Profile, Economy, database, () => Profile.Save());
            Weapons = new WeaponService(Profile.Profile, Economy, database, () => Profile.Save());
            Account = new AccountService(Profile.Profile, Economy, LoadAccountTable());
            StageProgress = new StageProgressService(Profile.Profile, Economy, () => Profile.Save(), Inventory, Account);
            Shop = new ArenaSurvival.Shop.ShopService(Profile.Profile, Economy, Inventory, LoadShopCatalog(), () => Profile.Save());
            // 11A: store + quảng cáo giả lập trong Editor/bản dev; bản phát hành chưa có SDK thì báo "không khả dụng" (11B gắn SDK thật).
            ArenaSurvival.Shop.IStore store = dev ? new ArenaSurvival.Shop.FakeStore() : new ArenaSurvival.Shop.UnavailableStore();
            Iap = new ArenaSurvival.Shop.IAPService(store, new ArenaSurvival.Shop.FakeReceiptValidator(), Shop, Profile.Profile, () => Profile.Save());
            ArenaSurvival.Services.IAdProvider ads = dev ? ArenaSurvival.Services.FakeAdProvider.Create() : (ArenaSurvival.Services.IAdProvider)new ArenaSurvival.Services.UnavailableAdProvider();
            Ads = new ArenaSurvival.Services.AdsService(ads, Profile.Profile, () => Profile.Save(), () => Clock.UtcNow);
            Missions = new ArenaSurvival.Missions.MissionService(Profile.Profile, Economy, Shop, LoadMissionCatalog(), Clock,
                () => Profile.Save(), Profile.MarkDirty);
            Missions.Connect(Ads);

            ArenaSurvival.Services.IAnalyticsSink sink;
            if (UsingUgs)
            {
                var ugsSink = new ArenaSurvival.Services.UgsAnalyticsSink(Profile.Profile.consent == 1);
                Ads.OnConsentChanged += ugsSink.SetConsent;
                sink = ugsSink;
            }
            else if (dev) sink = new ArenaSurvival.Services.LocalAnalyticsSink(System.IO.Path.Combine(folder, "analytics-log.jsonl"));
            else sink = new ArenaSurvival.Services.NullAnalyticsSink();
            Analytics = new ArenaSurvival.Services.AnalyticsService(sink, Profile.Profile);
            _tracker = new ArenaSurvival.Services.AnalyticsTracker(Analytics, Profile.Profile, Economy, Account, Weapons, Shop, Ads, Missions);

            if (_onStageCompleted == null) _onStageCompleted = result =>
            {
                StageProgress.Apply(result);
                // Sau khi áp thưởng (đã lưu): đếm nhiệm vụ rồi lưu thêm 1 lần để tiến độ không mất nếu tắt game ở màn kết quả.
                Missions.TrackStage(result);
                Profile.SaveIfDirty();
            };
            StageEvents.OnStageCompleted -= _onStageCompleted;
            StageEvents.OnStageCompleted += _onStageCompleted;

            ServicesHost.Create();
            // Sau khi mọi dịch vụ sẵn sàng: store có thể giao ngay giao dịch dở dang từ lần chạy trước.
            Iap.Initialize();
            _tracker.SessionStart(Application.version);

            if (UsingUgs)
            {
                // "Bản mây máy này đồng bộ lần cuối" theo từng tài khoản UGS: trạng thái của lần cài, không thuộc hồ sơ.
                Cloud = new ArenaSurvival.Services.CloudSyncService(Profile, new ArenaSurvival.Services.UgsCloudStorage(), Clock,
                    id => PlayerPrefs.GetString(CloudSyncPrefPrefix + id, ""),
                    (id, sync) => { PlayerPrefs.SetString(CloudSyncPrefPrefix + id, sync ?? ""); PlayerPrefs.Save(); });
                Cloud.Start();
            }
        }

        private static ArenaSurvival.Missions.MissionCatalog LoadMissionCatalog()
        {
            var catalog = Resources.Load<ArenaSurvival.Missions.MissionCatalog>(ArenaSurvival.Missions.MissionCatalog.ResourcePath);
            if (catalog != null) return catalog;
            Debug.LogWarning($"GameServices: không tìm thấy Resources/{ArenaSurvival.Missions.MissionCatalog.ResourcePath}; không có nhiệm vụ.");
            return ScriptableObject.CreateInstance<ArenaSurvival.Missions.MissionCatalog>();
        }

        private static ArenaSurvival.Shop.ShopCatalog LoadShopCatalog()
        {
            var catalog = Resources.Load<ArenaSurvival.Shop.ShopCatalog>(ArenaSurvival.Shop.ShopCatalog.ResourcePath);
            if (catalog != null) return catalog;
            Debug.LogWarning($"GameServices: không tìm thấy Resources/{ArenaSurvival.Shop.ShopCatalog.ResourcePath}; cửa hàng trống.");
            return ScriptableObject.CreateInstance<ArenaSurvival.Shop.ShopCatalog>();
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
