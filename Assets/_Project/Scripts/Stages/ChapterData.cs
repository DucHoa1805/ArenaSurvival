using System;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// 1 chương (bối cảnh + 10 màn). Tạo bằng công cụ nhập bảng cân bằng (stages.csv), không cần tạo tay.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Chapter Data", fileName = "Chapter_")]
    public class ChapterData : ScriptableObject
    {
        [Tooltip("ID cố định (c1, c2...).")]
        public string id;
        [Min(1)] public int number = 1;
        public string displayName = "Sa mạc";
        public StageData[] stages = Array.Empty<StageData>();
        [Tooltip("Prefab sân (Tilemap) của chương — ArenaLoader tạo khi vào màn/Hub. Trống = dùng sân có sẵn trong scene.")]
        public GameObject arenaPrefab;
    }
}
