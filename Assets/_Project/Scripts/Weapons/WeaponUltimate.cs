using System.Collections;
using System.Collections.Generic;
using ArenaSurvival.Combat;
using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Những gì 1 chiêu cần từ Player (PlayerUltimate tạo 1 lần, dùng lại). Asset chiêu dùng chung nên không giữ trạng thái:
    /// mọi biến tạm nằm trong coroutine Run.
    /// </summary>
    public class UltimateContext
    {
        public Transform Player;
        public PlayerWeapon Weapon;
        public PlayerStats Stats;
        public LayerMask EnemyLayer;
        /// <summary>Tia laser dựng sẵn (tắt khi không dùng).</summary>
        public LineRenderer Beam;

        private readonly List<Collider2D> _hits = new List<Collider2D>(128);
        private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[64];
        private ContactFilter2D _filter;
        private bool _filterReady;

        /// <summary>Sát thương 1 phát của súng đang cầm (đã nhân nâng cấp trong màn) — chiêu tính theo bội số của nó.</summary>
        public float BaseDamage => Weapon.CurrentShotDamage;

        private ContactFilter2D Filter
        {
            get
            {
                if (!_filterReady)
                {
                    _filter = new ContactFilter2D { useLayerMask = true, layerMask = EnemyLayer, useTriggers = true };
                    _filterReady = true;
                }
                return _filter;
            }
        }

        /// <summary>Nổ vùng tại 1 điểm: gây sát thương mọi quái trong bán kính + hiệu ứng nổ có sẵn.</summary>
        public void Explode(Vector2 center, float radius, float damage)
        {
            int count = Physics2D.OverlapCircle(center, radius, Filter, _hits);
            for (int i = 0; i < count; i++)
                if (_hits[i].TryGetComponent(out Health health)) health.TakeDamage(damage);
            Core.GameEvents.Explosion(center, radius);
        }

        /// <summary>Gây sát thương mọi quái nằm trên đoạn thẳng (tia laser).</summary>
        public void DamageAlongRay(Vector2 origin, Vector2 direction, float length, float damage)
        {
            int count = Physics2D.Raycast(origin, direction, Filter, _rayHits, length);
            for (int i = 0; i < count; i++)
            {
                if (_rayHits[i].collider.TryGetComponent(out Health health))
                {
                    health.TakeDamage(damage);
                    Core.GameEvents.ProjectileHit(_rayHits[i].point);
                }
            }
        }
    }

    /// <summary>
    /// Chiêu đặc biệt của 1 vũ khí chính (10E C): dùng khi đầy mana. Kiểu chiêu mới = 1 lớp con; chiêu mới cùng kiểu = 1 asset
    /// (giống BossAttack). Gán vào WeaponData.ultimate.
    /// </summary>
    public abstract class WeaponUltimate : ScriptableObject
    {
        public string displayName = "Chiêu";
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Màu lóe màn hình + chữ khi dùng chiêu.")]
        public Color color = Color.white;

        /// <summary>Thời gian hiệu lực hiện trên nút chiêu (0 = tức thời).</summary>
        public virtual float Duration => 0f;

        public abstract IEnumerator Run(UltimateContext ctx);
    }
}
