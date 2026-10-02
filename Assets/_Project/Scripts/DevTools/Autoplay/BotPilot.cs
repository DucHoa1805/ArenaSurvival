using System.Collections.Generic;
using ArenaSurvival.Enemies;
using ArenaSurvival.Player;
using ArenaSurvival.Weapons;
using UnityEngine;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Lái Player thay bàn phím khi bot tự chơi: ~10 lần/giây (thời gian game) quét mối nguy quanh mình rồi tính hướng bằng BotSteering.
    /// Súng vẫn tự ngắm như bình thường nên bot chỉ cần đi. Chỉ AutoplayRunner gắn component này (build dev).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class BotPilot : MonoBehaviour
    {
        private const float ThinkInterval = 0.1f;
        private const float EnemyScan = 6f;
        private const float BeamScan = 15f;
        private const float BulletScan = 3.5f;
        private const float PickupScan = 6f;

        private PlayerController _controller;
        private PlayerWeapon _weapon;
        private Rect _arena = new Rect(-9.1f, -9.1f, 18.2f, 18.2f);
        private readonly List<Threat> _threats = new List<Threat>(256);
        private readonly List<Collider2D> _hits = new List<Collider2D>(256);
        private ContactFilter2D _enemyFilter, _bulletFilter, _pickupFilter;
        private float _timer;
        private Vector2 _direction;

        public void Configure(Rect arena) => _arena = arena;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _weapon = GetComponent<PlayerWeapon>();
            _enemyFilter = Filter("Enemy");
            _bulletFilter = Filter("EnemyProjectile");
            _pickupFilter = Filter("Pickup");
        }

        private static ContactFilter2D Filter(string layer)
        {
            var f = new ContactFilter2D { useTriggers = true };
            f.SetLayerMask(LayerMask.GetMask(layer));
            return f;
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.MoveOverride = null;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = ThinkInterval;
                Think();
            }
            _controller.MoveOverride = _direction;
        }

        private void Think()
        {
            Vector2 self = transform.position;
            _threats.Clear();

            // Súng chỉ tự ngắm quái trong tầm của nó: né xa hơn tầm thì không bao giờ bắn trúng (Shotgun tầm 3.5 — bot từng kẹt
            // cả màn vì luôn đứng ngoài 5). Khoảng né theo tầm súng đang cầm.
            float range = _weapon != null && _weapon.Stats.range > 0f ? _weapon.Stats.range : 6f;
            SteeringSettings settings = SteeringSettings.Default;
            settings.enemyRange = Mathf.Clamp(range * 0.8f, 2.2f, 5f);
            Vector2? nearestEnemy = null;
            float nearestSqr = float.MaxValue;

            int count = Physics2D.OverlapCircle(self, BeamScan, _enemyFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                Enemy enemy = _hits[i].GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector2 pos = enemy.transform.position;

                // Tia ngắm Sniper đang bật: né ngang tia (kể cả khi Sniper ở xa).
                LineRenderer beam = enemy.GetComponentInChildren<LineRenderer>();
                if (beam != null && beam.enabled)
                {
                    Vector2 a = beam.GetPosition(0), b = beam.GetPosition(1);
                    _threats.Add(new Threat { kind = ThreatKind.Beam, position = a, vector = (b - a).normalized, radius = (b - a).magnitude, weight = 1f });
                }

                float sqr = (pos - self).sqrMagnitude;
                if (sqr < nearestSqr) { nearestSqr = sqr; nearestEnemy = pos; }

                // Kẻ lao (Charger, boss) lao thẳng vào chỗ mình đứng: coi đường từ nó tới mình như 1 tia để bước ngang khỏi đường lao.
                bool charger = enemy.Data != null && enemy.Data.behaviour is ChargerBehaviour;
                bool boss = enemy.Data != null && enemy.Data.isBoss;
                float dist = Mathf.Sqrt(sqr);
                if ((charger && dist < 7f) || (boss && dist < 10f))
                {
                    _threats.Add(new Threat { kind = ThreatKind.Beam, position = pos, vector = (self - pos) / Mathf.Max(dist, 0.01f), radius = dist + 2f, weight = 1f });
                }

                if (sqr > EnemyScan * EnemyScan) continue;
                float weight = 1f;
                if (boss) weight = 3f;
                else if (charger || (enemy.Data != null && enemy.Data.behaviour is KamikazeBehaviour)) weight = 2f;
                _threats.Add(new Threat { kind = ThreatKind.Enemy, position = pos, weight = weight });
            }

            count = Physics2D.OverlapCircle(self, BulletScan, _bulletFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D rb = _hits[i].attachedRigidbody;
                if (rb == null) continue;
                _threats.Add(new Threat { kind = ThreatKind.Bullet, position = rb.position, vector = rb.linearVelocity, weight = 1f });
            }

            // Ít quả pháo cùng lúc; FindObjectsByType có cấp phát nhưng bot chỉ chạy ở build dev, 10 lần/giây.
            foreach (ArtilleryShell shell in FindObjectsByType<ArtilleryShell>(FindObjectsSortMode.None))
            {
                _threats.Add(new Threat { kind = ThreatKind.Blast, position = shell.transform.position, radius = shell.transform.localScale.x * 0.5f, weight = 1f });
            }

            Vector2? attractor = null;
            count = Physics2D.OverlapCircle(self, PickupScan, _pickupFilter, _hits);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = _hits[i].transform.position;
                float d = (p - self).sqrMagnitude;
                if (d < best) { best = d; attractor = p; }
            }

            // Không có gì để nhặt và không quái nào trong tầm súng: tiến lại quái gần nhất (lúc an toàn), nếu không màn không bao giờ hết quái.
            if (attractor == null && nearestEnemy.HasValue && nearestSqr > range * range * 0.8f) attractor = nearestEnemy;

            _direction = BotSteering.Compute(self, _threats, _threats.Count, _arena, attractor, settings, _direction);
        }
    }
}
