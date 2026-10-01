using System;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>Danh sách màn theo thứ tự chơi. Biết "màn tiếp theo" là gì để màn kết quả không phải đoán.</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Stage Catalog", fileName = "StageCatalog")]
    public class StageCatalog : ScriptableObject
    {
        public StageData[] stages = Array.Empty<StageData>();

        public StageData First => stages != null && stages.Length > 0 ? stages[0] : null;

        public int IndexOf(StageData stage) => stages == null || stage == null ? -1 : Array.IndexOf(stages, stage);

        /// <summary>Màn kế tiếp; null nếu là màn cuối hoặc màn không nằm trong danh sách.</summary>
        public StageData GetNext(StageData stage)
        {
            int index = IndexOf(stage);
            return index >= 0 && index + 1 < stages.Length ? stages[index + 1] : null;
        }
    }
}
