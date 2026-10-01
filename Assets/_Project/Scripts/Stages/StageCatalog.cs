using System;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Danh sách màn theo thứ tự chơi + chia chương. Giữ cả danh sách phẳng (`stages`) lẫn danh sách chương (`chapters`):
    /// mở khóa tuần tự, "màn tiếp theo", benchmark chỉ cần danh sách phẳng; Hub cần chương. Công cụ nhập bảng ghi cả hai.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Stage Catalog", fileName = "StageCatalog")]
    public class StageCatalog : ScriptableObject
    {
        public ChapterData[] chapters = Array.Empty<ChapterData>();
        public StageData[] stages = Array.Empty<StageData>();

        public StageData First => stages != null && stages.Length > 0 ? stages[0] : null;

        public int IndexOf(StageData stage) => stages == null || stage == null ? -1 : Array.IndexOf(stages, stage);

        /// <summary>Màn kế tiếp (đi xuyên qua các chương); null nếu là màn cuối hoặc màn không nằm trong danh sách.</summary>
        public StageData GetNext(StageData stage)
        {
            int index = IndexOf(stage);
            return index >= 0 && index + 1 < stages.Length ? stages[index + 1] : null;
        }

        public ChapterData ChapterOf(StageData stage)
        {
            if (stage == null || chapters == null) return null;
            foreach (ChapterData chapter in chapters)
            {
                if (chapter != null && chapter.stages != null && Array.IndexOf(chapter.stages, stage) >= 0) return chapter;
            }
            return null;
        }

        public int IndexOfChapter(ChapterData chapter) => chapters == null || chapter == null ? -1 : Array.IndexOf(chapters, chapter);
    }
}
