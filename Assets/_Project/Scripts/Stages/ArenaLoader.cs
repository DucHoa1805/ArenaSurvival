using ArenaSurvival.Meta;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Đổi sân theo chương (10B). Scene giữ sẵn 1 instance sân (để nhìn thấy khi sửa scene); lúc chạy, nếu chương của màn dùng sân khác
    /// thì tắt instance đó và tạo sân của chương tại cùng vị trí. Mọi sân cùng lưới và phạm vi ô, nên WaveSpawner/camera không phải đổi.
    /// Game: chương của màn đang chọn. Hub: chương của màn nên chơi tiếp (nền Hub đổi theo tiến độ).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ArenaLoader : MonoBehaviour
    {
        [SerializeField] private StageCatalog _catalog;
        [Tooltip("Instance sân có sẵn trong scene.")]
        [SerializeField] private GameObject _sceneArena;
        [Tooltip("Prefab gốc của instance trên (để biết có cần đổi không).")]
        [SerializeField] private GameObject _sceneArenaSource;
        [Tooltip("Bật ở scene Game: dùng màn đang chọn. Tắt ở Hub: dùng màn nên chơi tiếp.")]
        [SerializeField] private bool _useSelectedStage = true;

        public static bool NeedsSwap(GameObject sceneSource, GameObject chapterArena) => chapterArena != null && chapterArena != sceneSource;

        private void Awake()
        {
            if (_catalog == null) return;
            StageData stage = _useSelectedStage
                ? (StageSelection.Current != null ? StageSelection.Current : _catalog.First)
                : GameServices.IsInitialized ? GameServices.StageProgress.NextStage(_catalog) : _catalog.First;
            ChapterData chapter = _catalog.ChapterOf(stage);
            if (chapter == null || !NeedsSwap(_sceneArenaSource, chapter.arenaPrefab)) return;

            Vector3 position = _sceneArena != null ? _sceneArena.transform.position : Vector3.zero;
            if (_sceneArena != null) _sceneArena.SetActive(false);
            Instantiate(chapter.arenaPrefab, position, Quaternion.identity);
        }
    }
}
