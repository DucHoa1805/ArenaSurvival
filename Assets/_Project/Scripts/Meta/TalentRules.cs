using UnityEngine;

namespace ArenaSurvival.Meta
{
    public static class TalentRules
    {
        /// <summary>Giá để lên cấp kế: baseCost × growth^currentLevel (plan D1), làm tròn xuống.</summary>
        public static int Cost(int baseCost, float growth, int currentLevel) =>
            Mathf.FloorToInt(baseCost * Mathf.Pow(growth, Mathf.Max(0, currentLevel)) + 0.0001f);

        public static bool IsMaxed(int level, int maxLevel) => level >= maxLevel;
    }
}
