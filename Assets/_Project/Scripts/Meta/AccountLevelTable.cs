using System;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>ID tính năng mở theo level tài khoản. Hằng số tập trung để gõ sai không âm thầm tạo tính năng "mở từ đầu".</summary>
    public static class FeatureIds
    {
        public const string Equipment = "equipment";
        public const string Talents = "talents";
        public const string Shop = "shop";
        public const string Events = "events";
        /// <summary>Nhiệm vụ ngày + quà đăng nhập (Phase 12). Không có trong bảng → mở từ đầu: quà ngày 1 phải nhận được ngay.</summary>
        public const string Missions = "missions";
    }

    [Serializable]
    public class FeatureUnlock
    {
        public string featureId;
        [Min(1)] public int level = 1;
        [Tooltip("Tên hiện ở popup lên cấp và tab bị khóa.")]
        public string displayName;
    }

    /// <summary>
    /// Tính năng mở theo level tài khoản (plan A4). Tính năng không có trong bảng = mở từ đầu. Đặt ở Resources như ItemDatabase
    /// vì AccountService nằm trong GameServices (tự khởi tạo trước mọi scene).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Account Level Table", fileName = "AccountLevelTable")]
    public class AccountLevelTable : ScriptableObject
    {
        public const string ResourcePath = "AccountLevelTable";

        public FeatureUnlock[] unlocks = new FeatureUnlock[0];

        public FeatureUnlock Find(string featureId)
        {
            if (unlocks == null) return null;
            foreach (FeatureUnlock u in unlocks) if (u != null && u.featureId == featureId) return u;
            return null;
        }
    }
}
