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
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần lấy từ pool là mỗi lần tạo 1 delegate mới (rác GC) — Profiler bắt được đúng chỗ này khi quái đông.
        private System.Action<XpOrb> _releaseOrb;

        private void Awake()
        {
            _orbPool = new ObjectPool<XpOrb>(_orbPrefab, _poolPrewarmCount, transform);
            _releaseOrb = ReturnOrbToPool;
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
            orb.OnReturnToPool = _releaseOrb;
        }

        private void ReturnOrbToPool(XpOrb orb)
        {
            _orbPool.Release(orb);
        }
    }
}
