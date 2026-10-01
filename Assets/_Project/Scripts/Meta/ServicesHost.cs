using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Object ẩn sống xuyên scene, chỉ để nhận sự kiện vòng đời app mà class C# thường không nhận được.
    /// Trên điện thoại, người chơi vuốt tắt app khi đang ở nền thì OnApplicationQuit KHÔNG chạy — phải lưu lúc chuyển nền (pause).
    /// </summary>
    public class ServicesHost : MonoBehaviour
    {
        private static ServicesHost _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        public static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("[GameServices]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ServicesHost>();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) GameServices.Profile?.SaveIfDirty();
        }

        private void OnApplicationQuit() => GameServices.Profile?.SaveIfDirty();
    }
}
