using System.Collections.Generic;
using System.IO;
using ArenaSurvival.Enemies;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Stages;
using UnityEditor;
using UnityEngine;

namespace ArenaSurvival.EditorTools
{
    /// <summary>Đường dẫn và tra cứu asset dùng chung cho công cụ nhập/xuất bảng cân bằng.</summary>
    internal static class BalanceAssets
    {
        public const string BalanceDir = "Docs/Balance";
        public const string ArtRoot = "Assets/_Project/Art/";
        public const string EnemyDir = "Assets/_Project/Data/Enemies";
        public const string WaveDir = "Assets/_Project/Data/Stages/Waves";
        public const string StageDir = "Assets/_Project/Data/Stages";
        public const string CatalogPath = "Assets/_Project/Data/Stages/StageCatalog_Main.asset";
        public const string ItemDatabasePath = "Assets/_Project/Resources/ItemDatabase.asset";

        public static string FullBalancePath(string file) => Path.Combine(Path.GetDirectoryName(Application.dataPath), BalanceDir, file);

        /// <summary>Mọi asset kiểu T trong thư mục Data, khóa theo tên asset (id trong CSV).</summary>
        public static Dictionary<string, T> ByName<T>(string folder = "Assets/_Project/Data") where T : Object
        {
            var map = new Dictionary<string, T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) map[asset.name] = asset;
            }
            return map;
        }

        /// <summary>Màn theo StageData.id (không theo tên asset: id là thứ save đang lưu).</summary>
        public static Dictionary<string, StageData> StagesById()
        {
            var map = new Dictionary<string, StageData>();
            foreach (StageData s in ByName<StageData>().Values) if (!string.IsNullOrEmpty(s.id)) map[s.id] = s;
            return map;
        }

        /// <summary>"Kenney-TopDownShooter/PNG/Hitman 1/hitman1_gun.png" (sprite đơn) hoặc "...png#tên_sprite_con".</summary>
        public static Sprite LoadSprite(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return null;
            string path = relative, sub = null;
            int hash = relative.IndexOf('#');
            if (hash >= 0) { path = relative.Substring(0, hash); sub = relative.Substring(hash + 1); }
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ArtRoot + path))
                if (o is Sprite s && (sub == null || s.name == sub)) return s;
            return null;
        }

        public static string SpritePath(Sprite sprite)
        {
            if (sprite == null) return "";
            string path = AssetDatabase.GetAssetPath(sprite);
            if (path.StartsWith(ArtRoot)) path = path.Substring(ArtRoot.Length);
            // File có nhiều sprite con: ghi rõ tên con để nhập lại đúng sprite.
            int count = 0;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(sprite))) if (o is Sprite) count++;
            return count > 1 ? path + "#" + sprite.name : path;
        }

        public static MaterialData ShardOf(string weaponId)
        {
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            return db != null ? db.GetMaterial(Weapons.WeaponRules.ShardId(weaponId)) : null;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Dictionary<string, LootTable> LootTables() => ByName<LootTable>();
        public static Dictionary<string, EnemyBehaviour> Behaviours() => ByName<EnemyBehaviour>();
    }
}
