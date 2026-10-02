using System.IO;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Menu dev (F1): xem hồ sơ người chơi, cộng tiền, mở khóa màn, xóa hồ sơ, và vài lệnh trong lúc chơi để test nhanh.
    /// Chỉ có ở Editor và Development build — bản phát hành không bao giờ tạo object này (plan, checklist trước soft launch:
    /// "Menu cheat đã tắt hoàn toàn ở bản phát hành"). Mọi thay đổi tiền tệ vẫn đi qua EconomyService như game thật.
    /// </summary>
    public class DevMenu : MonoBehaviour
    {
        private const float Width = 300f; // đơn vị trước khi phóng (xem OnGUI)

        private DevMenuView _view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Debug.isDebugBuild) return;
            var go = new GameObject("[DevMenu]");
            DontDestroyOnLoad(go);
            go.AddComponent<DevMenu>();
        }

        private void Awake()
        {
            // OnGUI tạo rác mỗi frame: tách phần vẽ ra component riêng và chỉ bật khi menu đang mở,
            // để lúc đóng menu không ảnh hưởng gì tới hiệu năng (benchmark vẫn 0 B/frame).
            _view = gameObject.AddComponent<DevMenuView>();
            _view.enabled = false;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) _view.enabled = !_view.enabled;
        }

        /// <summary>Phần vẽ IMGUI — chỉ bật khi menu mở.</summary>
        private class DevMenuView : MonoBehaviour
        {
            private Vector2 _scroll;

            private void OnGUI()
            {
                // IMGUI vẽ theo pixel cố định: màn hình càng cao chữ càng nhỏ. Phóng theo chiều cao (chuẩn 540 px)
                // để đọc được cả ở 1080p lẫn trên điện thoại.
                float scale = Mathf.Max(1f, Screen.height / 540f);
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                var area = new Rect(8, 8, Width, Screen.height / scale - 16);
                // Vẽ nền 2 lớp cho đậm: HUD phía sau không lẫn vào chữ.
                GUI.Box(area, GUIContent.none);
                GUI.Box(area, GUIContent.none);
                GUILayout.BeginArea(area, GUI.skin.box);
                // Chỉ cuộn dọc: chữ dài tự xuống dòng, không cần thanh cuộn ngang.
                _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                GUILayout.Label("<b>MENU DEV</b>  (F1 để đóng)", Rich());
                DrawProfile();
                GUILayout.Space(8);
                DrawProfileActions();
                if (FindFirstObjectByType<StageSession>() != null)
                {
                    GUILayout.Space(8);
                    DrawGameActions();
                }
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            private static GUIStyle Rich() => new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };

            private static void DrawProfile()
            {
                if (!GameServices.IsInitialized)
                {
                    GUILayout.Label("Dịch vụ chưa khởi tạo.");
                    return;
                }

                PlayerProfile p = GameServices.Profile.Profile;
                GUILayout.Label("<b>Hồ sơ người chơi</b>", Rich());
                GUILayout.Label($"Tải save: {GameServices.Profile.LastLoadStatus}   ·   version {p.saveVersion}" +
                                (p.tampered ? "   ·   <color=#ff6b5b>ĐÃ BỊ SỬA TAY</color>" : ""), Rich());
                GUILayout.Label($"Vàng: {GameServices.Economy.GetBalance(CurrencyIds.Gold)}   ·   " +
                                $"Kim cương: {GameServices.Economy.GetBalance(CurrencyIds.Gem)}   ·   XP tài khoản: {p.accountXp}");

                StageCatalog catalog = FindCatalog();
                if (catalog != null)
                {
                    foreach (StageData stage in catalog.stages)
                    {
                        if (stage == null) continue;
                        bool unlocked = GameServices.StageProgress.IsUnlocked(catalog, stage);
                        int stars = GameServices.StageProgress.GetStars(stage.id);
                        string state = stars > 0 ? new string('★', stars) + new string('☆', 3 - stars) : unlocked ? "mở" : "khóa";
                        GUILayout.Label($"  Màn {stage.displayName} ({stage.id}): {state}");
                    }
                }

                GUILayout.Label($"<size=10>{GameServices.Profile.SavePath}</size>", Rich());
            }

            private static void DrawProfileActions()
            {
                if (!GameServices.IsInitialized) return;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+1000 vàng")) GameServices.Economy.Add(CurrencyIds.Gold, 1000, EconomyReasons.Dev);
                if (GUILayout.Button("+100 kim cương")) GameServices.Economy.Add(CurrencyIds.Gem, 100, EconomyReasons.Dev);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+50 mỗi nguyên liệu"))
                {
                    GameServices.Economy.Add(CurrencyIds.Leather, 50, EconomyReasons.Dev);
                    GameServices.Economy.Add(CurrencyIds.Ore, 50, EconomyReasons.Dev);
                    GameServices.Economy.Add(CurrencyIds.BossCore, 50, EconomyReasons.Dev);
                }
                if (GUILayout.Button("Nhận 1 trang bị ngẫu nhiên")) GiveRandomItem();
                GUILayout.EndHorizontal();

                if (GUILayout.Button("+100 XP tài khoản"))
                {
                    GameServices.Account.AddXp(100);
                    GameServices.Profile.Save();
                }
                if (GUILayout.Button("+20 mảnh mỗi vũ khí"))
                {
                    foreach (Weapons.WeaponData weapon in GameServices.Inventory.Database.weapons)
                        if (weapon != null) GameServices.Economy.Add(Weapons.WeaponRules.ShardId(weapon.id), 20, EconomyReasons.Dev);
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Mở khóa hết màn")) UnlockAllStages();
                if (GUILayout.Button("Lưu ngay")) GameServices.Profile.Save();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Mở thư mục save"))
                {
                    Application.OpenURL("file:///" + Path.GetDirectoryName(GameServices.Profile.SavePath).Replace('\\', '/'));
                }
                GUI.color = new Color(1f, 0.55f, 0.5f);
                if (GUILayout.Button("Xóa hồ sơ (chơi lại từ đầu)"))
                {
                    GameServices.Profile.ResetToNew();
                    // Nạp lại scene để mọi UI đọc lại hồ sơ mới (số vàng, nút "Chơi 1-1"...).
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                // Cửa hàng giả lập (11A): chọn kết quả giao dịch kế tiếp để thử cả nhánh lỗi/hủy/tắt game giữa chừng.
                GUILayout.Label($"<b>Cửa hàng giả</b>: kết quả = {Shop.FakeStore.Mode}  ·  giao dịch dở dang: {Shop.FakeStore.PendingCount}" +
                                $"  ·  xóa QC: {GameServices.Shop.RemoveAdsOwned}", Rich());
                GUILayout.BeginHorizontal();
                foreach (Shop.FakeStoreMode mode in System.Enum.GetValues(typeof(Shop.FakeStoreMode)))
                    if (GUILayout.Button(mode.ToString())) Shop.FakeStore.Mode = mode;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Giao lại giao dịch dở dang")) GameServices.Iap.Initialize();
                if (GUILayout.Button("Xóa store giả")) Shop.FakeStore.ClearAll();
                if (GUILayout.Button("Đặt lại lượt QC hôm nay")) { GameServices.Profile.Profile.adViews.Clear(); GameServices.Profile.Save(); }
                GUILayout.EndHorizontal();

                // Phase 12: thử nhiệm vụ/quà mà không phải chờ qua ngày. Không dịch đồng hồ (GameClock nhớ mốc lớn nhất nên dịch
                // tới rồi tắt game sẽ làm đồng hồ đứng tới khi giờ thật đuổi kịp) — chỉ xóa mốc ngày đã lưu.
                Missions.MissionService missions = GameServices.Missions;
                GUILayout.Label($"<b>Nhiệm vụ</b>: điểm {missions.Points}  ·  quà đăng nhập đã nhận {GameServices.Profile.Profile.login.count}" +
                                $"  ·  Remote Config: {GameServices.RemoteConfig.Source} ({GameServices.RemoteConfig.AppliedKeys} khóa)" +
                                $"  ·  analytics: {(GameServices.Analytics.Enabled ? GameServices.Analytics.SentCount + " sự kiện" : "chưa đồng ý")}", Rich());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Giả lập sang ngày mới"))
                {
                    PlayerProfile p = GameServices.Profile.Profile;
                    p.daily.day = 0;
                    p.login.lastDay = 0;
                    p.adDay = 0;
                    GameServices.Profile.Save();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                if (GUILayout.Button("Xong hết nhiệm vụ"))
                {
                    foreach (Missions.MissionData m in missions.Catalog.daily)
                        if (m != null) missions.Track(m.metric, m.target);
                    GameServices.Profile.Save();
                }
                if (GUILayout.Button("Nạp lại Remote Config")) GameServices.RemoteConfig.Refresh();
                GUILayout.EndHorizontal();

                // Phase 12B: Editor/bản dev mặc định cục bộ để số liệu thử không lên dashboard; bật để kiểm tra kết nối thật
                // (môi trường "development"). Đọc lúc khởi tạo dịch vụ nên phải mở lại game.
                bool useUgs = PlayerPrefs.GetInt(GameServices.DevUseUgsPref, 0) == 1;
                string ugs = GameServices.UsingUgs
                    ? $"{Services.UgsBootstrap.State} · {Services.UgsBootstrap.Environment} · {Services.UgsBootstrap.PlayerId}" +
                      (Services.UgsBootstrap.ServerOffset.HasValue ? $" · lệch giờ {Services.UgsBootstrap.ServerOffset.Value.TotalSeconds:0}s" : "") +
                      (Services.UgsBootstrap.LastError != null ? $" · <color=#ff6b5b>{Services.UgsBootstrap.LastError}</color>" : "")
                    : "tắt (dùng bản cục bộ)";
                string cloud = GameServices.Cloud != null
                    ? $"{GameServices.Cloud.State}" + (GameServices.Cloud.LastSyncUtc.HasValue ? $" · đồng bộ {GameServices.Cloud.LastSyncUtc.Value.ToLocalTime():HH:mm:ss}" : "")
                    : "-";
                GUILayout.Label($"<b>UGS</b>: {ugs}  ·  Cloud Save: {cloud}", Rich());
                GUILayout.BeginHorizontal();
                bool toggled = GUILayout.Toggle(useUgs, " Dùng UGS thật (mở lại game để áp dụng)");
                if (toggled != useUgs) { PlayerPrefs.SetInt(GameServices.DevUseUgsPref, toggled ? 1 : 0); PlayerPrefs.Save(); }
                if (GameServices.Cloud != null && GUILayout.Button("Tải lên đám mây ngay")) GameServices.Cloud.Flush(Time.realtimeSinceStartup);
                GUILayout.EndHorizontal();
            }

            private static void GiveRandomItem()
            {
                ItemDatabase db = GameServices.Inventory.Database;
                if (db.items == null || db.items.Length == 0) return;
                ItemData data = db.items[Random.Range(0, db.items.Length)];
                // Lưu ngay: Add không tự lưu (vốn dành cho áp thưởng hàng loạt cuối màn).
                GameServices.Inventory.Add(data.id, (Rarity)Random.Range(0, 4), out _);
                GameServices.Profile.Save();
            }

            private static void DrawGameActions()
            {
                var session = FindFirstObjectByType<StageSession>();
                GUILayout.Label($"<b>Trong màn</b>: {session.Stage?.displayName}  ·  đợt {session.CurrentWave}/{session.TotalWaves}", Rich());

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Tới đợt cuối")) session.DebugSkipToFinalWave();
                if (GUILayout.Button("Giết hết quái")) FindFirstObjectByType<WaveSpawner>()?.DespawnAll();
                GUILayout.EndHorizontal();

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Health health = player != null ? player.GetComponent<Health>() : null;
                GUILayout.BeginHorizontal();
                if (health != null)
                {
                    bool invulnerable = GUILayout.Toggle(health.Invulnerable, " Bất tử");
                    if (invulnerable != health.Invulnerable) health.Invulnerable = invulnerable;
                }
                if (player != null && GUILayout.Button("Đầy mana"))
                {
                    var ult = player.GetComponent<Player.PlayerUltimate>();
                    if (ult != null) ult.DebugFillMana();
                }
                if (GUILayout.Button("Thắng màn ngay"))
                {
                    session.DebugSkipToFinalWave();
                    session.DebugWinNow();
                }
                GUILayout.EndHorizontal();
            }

            private static StageCatalog FindCatalog()
            {
                var session = FindFirstObjectByType<StageSession>();
                if (session != null) return session.Catalog;
                var battle = FindFirstObjectByType<UI.Hub.BattleTab>(FindObjectsInactive.Include);
                return battle != null ? battle.Catalog : null;
            }

            private static void UnlockAllStages()
            {
                StageCatalog catalog = FindCatalog();
                if (catalog == null) return;
                // Ghi 1 sao cho màn chưa thắng — tương đương "đã thắng", nên màn kế mở. Không đụng tiền tệ.
                foreach (StageData stage in catalog.stages)
                {
                    if (stage == null || GameServices.StageProgress.IsCleared(stage.id)) continue;
                    GameServices.Profile.Profile.stageStars[stage.id] = 1;
                }
                GameServices.Profile.Save();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
