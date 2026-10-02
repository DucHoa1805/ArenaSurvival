using System;
using System.Collections.Generic;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace ArenaSurvival.Services
{
    /// <summary>Remote Config thật (12B): chờ UGS sẵn sàng rồi lấy mọi khóa của môi trường hiện tại thành chuỗi cho Tuning.</summary>
    public class UgsRemoteConfigProvider : IRemoteConfigProvider
    {
        // SDK yêu cầu 2 đối tượng thuộc tính (dùng cho điều kiện nhắm đối tượng); game chưa nhắm theo nhóm nên để rỗng.
        private struct UserAttributes { }
        private struct AppAttributes { }

        public void Fetch(Action<Dictionary<string, string>> onDone) => UgsBootstrap.WhenReady(() => FetchNow(onDone));

        private static async void FetchNow(Action<Dictionary<string, string>> onDone)
        {
            try
            {
                RuntimeConfig config = await Unity.Services.RemoteConfig.RemoteConfigService.Instance
                    .FetchConfigsAsync(new UserAttributes(), new AppAttributes());
                if (config == null || config.origin != ConfigOrigin.Remote)
                {
                    onDone?.Invoke(null);
                    return;
                }
                var values = new Dictionary<string, string>();
                // GetJson trả đúng dạng JSON gốc (số theo InvariantCulture, chuỗi không có ngoặc kép) — GetString đoán ngày tháng nên dễ sai.
                foreach (string key in config.GetKeys()) values[key] = config.GetJson(key, null);
                onDone?.Invoke(values);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] Remote Config lỗi: {e.Message}");
                onDone?.Invoke(null);
            }
        }
    }
}
