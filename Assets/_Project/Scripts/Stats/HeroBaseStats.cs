using UnityEngine;

namespace ArenaSurvival.Stats
{
    /// <summary>
    /// Chỉ số gốc của nhân vật, tách khỏi prefab Player để chỉnh bằng dữ liệu (và sau này có nhiều nhân vật).
    /// Trước Phase 8A, máu gốc nằm cứng trong Health của prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Hero Base Stats", fileName = "HeroBaseStats")]
    public class HeroBaseStats : ScriptableObject
    {
        public StatBlock baseStats = new StatBlock { maxHealth = 100f };
    }
}
