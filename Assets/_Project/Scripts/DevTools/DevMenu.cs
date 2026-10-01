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
                if (GUILayout.Button("+1000 vàng")) GameServices.Economy.Add(CurrencyIds.Gold, 1000);
                if (GUILayout.Button("+100 kim cương")) GameServices.Economy.Add(CurrencyIds.Gem, 100);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+50 mỗi nguyên liệu"))
                {
                    GameServices.Economy.Add(CurrencyIds.Leather, 50);
                    GameServices.Economy.Add(CurrencyIds.Ore, 50);
                    GameServices.Economy.Add(CurrencyIds.BossCore, 50);
                }
                if (GUILayout.Button("Nhận 1 trang bị ngẫu nhiên")) GiveRandomItem();
                GUILayout.EndHorizontal();

                if (GUILayout.Button("+20 mảnh mỗi vũ khí"))
                {
                    foreach (Weapons.WeaponData weapon in GameServices.Inventory.Database.weapons)
                        if (weapon != null) GameServices.Economy.Add(Weapons.WeaponRules.ShardId(weapon.id), 20);
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
                var menu = FindFirstObjectByType<UI.MainMenu>();
                return menu != null ? menu.Catalog : null;
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
