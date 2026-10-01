using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Loot;
using ArenaSurvival.Stages;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Áp kết quả màn vào hồ sơ: thưởng (qua EconomyService, không tự sửa số vàng), XP tài khoản, sao cao nhất, rồi lưu ngay.
    /// Scene Game không gọi class này — nó chỉ phát StageEvents.OnStageCompleted; GameServices nối sự kiện vào Apply.
    /// </summary>
    public class StageProgressService
    {
        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly Action _save;
        private readonly InventoryService _inventory;
        private readonly AccountService _account;

        /// <summary>Lần áp gần nhất — màn kết quả đọc để hiện số thực nhận.</summary>
        public StageGrant LastGrant { get; private set; }

        /// <param name="inventory">Null được (test chỉ về thưởng/sao): khi đó trang bị trong kết quả bị bỏ qua.</param>
        /// <param name="account">Null được (test cũ): khi đó XP cộng thẳng vào hồ sơ, không có thưởng lên cấp.</param>
        public StageProgressService(PlayerProfile profile, EconomyService economy, Action save, InventoryService inventory = null,
            AccountService account = null)
        {
            _account = account;
            _profile = profile;
            _economy = economy;
            _save = save;
            _inventory = inventory;
        }

        public StageGrant Apply(StageResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.stageId)) return null;

            bool alreadyCleared = IsCleared(result.stageId);
            float multiplier = StageProgressRules.RewardMultiplier(result.victory, alreadyCleared);
            int starsBefore = GetStars(result.stageId);

            var grant = new StageGrant
            {
                stageId = result.stageId,
                gold = StageProgressRules.ApplyMultiplier(result.gold, multiplier),
                accountXp = StageProgressRules.ApplyMultiplier(result.accountXp, multiplier),
                firstClear = result.victory && !alreadyCleared,
                multiplier = multiplier,
                starsBefore = starsBefore,
            };

            if (grant.gold > 0) _economy.Add(CurrencyIds.Gold, grant.gold);
            grant.levelBefore = _account != null ? _account.Level : 0;
            if (_account != null) _account.AddXp(grant.accountXp);
            else _profile.accountXp += grant.accountXp;
            grant.levelAfter = _account != null ? _account.Level : 0;

            // Chỉ màn thắng mới vào danh sách đã qua; sao chỉ tăng — chơi lại kém hơn không làm mất thành tích cũ.
            if (result.victory) _profile.stageStars[result.stageId] = Mathf.Max(starsBefore, result.starCount);
            grant.starsAfter = GetStars(result.stageId);

            ApplyLoot(result.collected, result.victory, grant);
            // bonusLoot chỉ được tạo khi thắng; vẫn kiểm tra victory để kết quả sai lệch không lọt trang bị khi thua.
            ApplyLoot(result.bonusLoot, result.victory, grant);

            LastGrant = grant;
            _save?.Invoke();
            return grant;
        }

        private void ApplyLoot(List<LootDrop> drops, bool victory, StageGrant grant)
        {
            if (drops == null) return;

            foreach (LootDrop drop in drops)
            {
                if (drop.kind == LootKind.Material)
                {
                    if (drop.amount <= 0 || string.IsNullOrEmpty(drop.id)) continue;
                    _economy.Add(drop.id, drop.amount);
                    grant.materials.TryGetValue(drop.id, out int sum);
                    grant.materials[drop.id] = sum + drop.amount;
                    continue;
                }

                // Thua mất trang bị đã nhặt (quyết định 8B): ghi lại để màn kết quả cho người chơi thấy.
                if (!victory)
                {
                    grant.lostItems.Add(drop);
                    continue;
                }

                if (_inventory == null) continue;
                InventoryService.AddOutcome outcome = _inventory.Add(drop.id, drop.rarity, out OwnedItem item);
                if (outcome == InventoryService.AddOutcome.Added) grant.newItemUids.Add(item.uid);
                else if (outcome == InventoryService.AddOutcome.AutoDismantled) grant.autoDismantled++;
            }
        }

        public bool IsCleared(string stageId) => _profile.stageStars.ContainsKey(stageId);

        public int GetStars(string stageId) => _profile.stageStars.TryGetValue(stageId, out int stars) ? stars : 0;

        /// <summary>Mở khóa tuần tự: màn đầu luôn mở, các màn sau mở khi màn ngay trước đã thắng.</summary>
        public bool IsUnlocked(StageCatalog catalog, StageData stage)
        {
            int index = catalog.IndexOf(stage);
            if (index < 0) return false;
            return index == 0 || IsCleared(catalog.stages[index - 1].id);
        }

        /// <summary>Màn nên chơi tiếp: màn chưa thắng đầu tiên; thắng hết thì màn cuối.</summary>
        public StageData NextStage(StageCatalog catalog)
        {
            if (catalog.stages == null || catalog.stages.Length == 0) return null;
            foreach (StageData stage in catalog.stages)
            {
                if (stage != null && !IsCleared(stage.id)) return stage;
            }
            return catalog.stages[catalog.stages.Length - 1];
        }
    }
}
