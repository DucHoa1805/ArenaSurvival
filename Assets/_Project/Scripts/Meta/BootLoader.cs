using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Scene Boot (build index 0): khởi tạo dịch vụ, tải save, rồi mới vào menu. Gom mọi việc khởi động vào một chỗ — Phase 11–12
    /// thêm đăng nhập, remote config, IAP vào đây — thay vì rải ở scene đầu tiên người chơi nhìn thấy.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            // GameServices đã tự khởi tạo trước scene đầu tiên; gọi lại để Boot không phụ thuộc vào cơ chế tự động đó.
            GameServices.Initialize();
            SceneLoader.LoadFirstScene();
        }
    }
}
