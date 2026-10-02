using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;
using LoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace ArenaSurvival.Services
{
    /// <summary>Cloud Save thật (12B): 2 khóa dữ liệu người chơi — "profile" (chuỗi save có chữ ký) và "profile_meta" (tóm tắt JSON).</summary>
    public class UgsCloudStorage : ICloudStorage
    {
        private const string ProfileKey = "profile";
        private const string MetaKey = "profile_meta";

        public bool IsReady => UgsBootstrap.State == UgsBootstrap.Status.Ready;
        public string AccountId => UgsBootstrap.PlayerId;

        public void WhenReady(Action onReady) => UgsBootstrap.WhenReady(onReady);

        public async void Load(Action<bool, CloudSnapshot> onDone)
        {
            try
            {
                Dictionary<string, Item> items = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { ProfileKey, MetaKey }, new LoadOptions());
                if (!items.TryGetValue(ProfileKey, out Item profile))
                {
                    onDone?.Invoke(true, null);
                    return;
                }
                CloudSummary summary = items.TryGetValue(MetaKey, out Item meta)
                    ? JsonConvert.DeserializeObject<CloudSummary>(meta.Value.GetAsString())
                    : new CloudSummary { accountXp = 1 }; // thiếu tóm tắt: coi là có tiến độ để hỏi người chơi thay vì đè
                onDone?.Invoke(true, new CloudSnapshot { envelope = profile.Value.GetAs<string>(), summary = summary });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] Đọc Cloud Save lỗi: {e.Message}");
                onDone?.Invoke(false, null);
            }
        }

        public async void Save(CloudSnapshot snapshot, Action<bool> onDone)
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
                {
                    [ProfileKey] = snapshot.envelope,
                    [MetaKey] = JsonConvert.SerializeObject(snapshot.summary),
                }, new SaveOptions());
                onDone?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] Ghi Cloud Save lỗi: {e.Message}");
                onDone?.Invoke(false);
            }
        }
    }
}
