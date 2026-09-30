using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Progression
{
    /// <summary>
    /// Thả 1 XP orb tại chỗ quái chết. Chỉ lắng nghe GameEvents.OnEnemyKilled nên Enemy không cần
    /// biết orb tồn tại — muốn đổi phần thưởng (rơi vàng, rơi máu...) thì thêm listener khác, không sửa Enemy.
    /// </summary>
    public class XpOrbSpawner : MonoBehaviour
    {
        [SerializeField] private XpOrb _orbPrefab;

        [Header("Pooling")]
        [SerializeField] private int _poolPrewarmCount = 30;

        private ObjectPool<XpOrb> _orbPool;

        private void Awake()
        {
            _orbPool = new ObjectPool<XpOrb>(_orbPrefab, _poolPrewarmCount, transform);
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(int xpReward, Vector3 position)
        {
            if (xpReward <= 0) return;

            XpOrb orb = _orbPool.Get(position, Quaternion.identity);
            orb.Initialize(xpReward);
            orb.OnReturnToPool = ReturnOrbToPool;
        }

        private void ReturnOrbToPool(XpOrb orb)
        {
            _orbPool.Release(orb);
        }
    }
}
