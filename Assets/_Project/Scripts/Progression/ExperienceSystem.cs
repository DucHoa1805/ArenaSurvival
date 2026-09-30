using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Progression
{
    /// <summary>
    /// Giữ cấp độ và XP của người chơi. Chỉ giao tiếp qua GameEvents: nghe OnXpGained,
    /// phát OnXpChanged / OnLevelUp — không biết XP đến từ đâu (orb, nhiệm vụ...) và cũng
    /// không biết ai dùng kết quả (HUD, màn chọn nâng cấp...).
    /// </summary>
    public class ExperienceSystem : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int XpToNextLevel => XpCurve.GetXpRequired(Level);

        private void OnEnable()
        {
            GameEvents.OnXpGained += HandleXpGained;
        }

        private void OnDisable()
        {
            GameEvents.OnXpGained -= HandleXpGained;
        }

        private void Start()
        {
            // Phát giá trị ban đầu để HUD có số liệu ngay, không phải chờ tới lần nhặt XP đầu tiên.
            GameEvents.XpChanged(CurrentXp, XpToNextLevel);
        }

        private void HandleXpGained(int amount)
        {
            if (amount <= 0) return;

            CurrentXp += amount;

            // Dùng while thay vì if: 1 lần nhận XP lớn có thể đủ để lên nhiều cấp liên tiếp.
            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;

                // TODO (Phase 4): bỏ log này khi có màn chọn nâng cấp lắng nghe OnLevelUp.
                Debug.Log($"Level up! Cấp {Level}, cần {XpToNextLevel} XP cho cấp kế.");
                GameEvents.LevelUp(Level);
            }

            GameEvents.XpChanged(CurrentXp, XpToNextLevel);
        }
    }
}
