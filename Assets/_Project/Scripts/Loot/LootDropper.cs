using System;
using System.Collections.Generic;
using ArenaSurvival.Core;
using ArenaSurvival.Enemies;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using ArenaSurvival.Vfx;
using UnityEngine;

namespace ArenaSurvival.Loot
{
    /// <summary>
    /// Rơi đồ trong màn: quái chết → quay bảng rơi đồ của con đó → thả pickup. Ghi lại đồ đã nhặt (Collected) để StageSession
    /// đưa vào StageResult. KHÔNG ghi vào hồ sơ người chơi — giữ gì/mất gì do tầng meta quyết định lúc màn kết thúc.
    /// </summary>
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private LootPickup _pickupPrefab;
        [Tooltip("Không bắt buộc: chữ nổi khi nhặt (\"+2 Da\", tên trang bị).")]
        [SerializeField] private FloatingText _labelPrefab;

        [Header("Rơi")]
        [Tooltip("Nhiều món rơi cùng lúc thì toả ra quanh chỗ chết trong bán kính này, không chồng lên nhau.")]
        [SerializeField] private float _scatterRadius = 0.6f;
        [SerializeField] private float _labelOffsetY = 0.5f;

        [Header("Pooling")]
        [SerializeField] private int _prewarmPickups = 12;
        [SerializeField] private int _maxLabels = 8;

        /// <summary>Nguồn ngẫu nhiên cho LootRules trong màn (gán sẵn 1 lần, không tạo delegate mỗi lần quay).</summary>
        public static readonly Func<float> UnityRandom = () => UnityEngine.Random.value;

        private static readonly Color MaterialRing = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color MaterialLabel = new Color(0.95f, 0.92f, 0.85f);

        private ObjectPool<LootPickup> _pickupPool;
        private ObjectPool<FloatingText> _labelPool;
        private Action<Enemy> _onEnemyKilled;
        private Action<LootPickup> _onCollected;
        private Action<LootPickup> _releasePickup;
        private Action<FloatingText> _releaseLabel;

        private readonly List<LootDrop> _collected = new List<LootDrop>();
        private readonly List<LootDrop> _rollBuffer = new List<LootDrop>(8);
        private readonly List<LootPickup> _active = new List<LootPickup>(32);

        public IReadOnlyList<LootDrop> Collected => _collected;

        private void Awake()
        {
            _pickupPool = new ObjectPool<LootPickup>(_pickupPrefab, _prewarmPickups, transform);
            if (_labelPrefab != null) _labelPool = new ObjectPool<FloatingText>(_labelPrefab, _maxLabels, transform, _maxLabels);
            _onEnemyKilled = HandleEnemyKilled;
            _onCollected = HandleCollected;
            _releasePickup = ReleasePickup;
            if (_labelPool != null) _releaseLabel = _labelPool.Release;
        }

        private void OnEnable()
        {
            if (_spawner != null) _spawner.OnEnemyKilled += _onEnemyKilled;
        }

        private void OnDisable()
        {
            if (_spawner != null) _spawner.OnEnemyKilled -= _onEnemyKilled;
        }

        private void HandleEnemyKilled(Enemy enemy)
        {
            // Bảng riêng (tinh anh, boss) thắng bảng mặc định của loại quái.
            EnemyModifiers mods = enemy.Modifiers;
            LootTable table = mods.loot != null ? mods.loot : enemy.Data != null ? enemy.Data.loot : null;
            float chance = mods.loot != null ? mods.lootChance : enemy.Data != null ? enemy.Data.lootChance : 0f;
            if (table == null || chance <= 0f) return;
            if (chance < 1f && UnityEngine.Random.value >= chance) return;

            _rollBuffer.Clear();
            LootRules.Roll(table, UnityRandom, _rollBuffer);
            Vector2 origin = enemy.transform.position;
            for (int i = 0; i < _rollBuffer.Count; i++) Spawn(_rollBuffer[i], origin);
        }

        private void Spawn(LootDrop drop, Vector2 origin)
        {
            Vector2 position = origin + (_rollBuffer.Count > 1 ? UnityEngine.Random.insideUnitCircle * _scatterRadius : Vector2.zero);
            LootPickup pickup = _pickupPool.Get(position, Quaternion.identity);
            pickup.Setup(drop, IconOf(drop), drop.kind == LootKind.Item ? RarityColors.Get(drop.rarity) : MaterialRing);
            pickup.OnCollected = _onCollected;
            pickup.OnReturnToPool = _releasePickup;
            _active.Add(pickup);
        }

        private void HandleCollected(LootPickup pickup)
        {
            if (!pickup.gameObject.activeSelf) return;

            LootDrop drop = pickup.Drop;
            _collected.Add(drop);
            Vector3 position = pickup.transform.position;
            ShowLabel(drop, position);
            GameEvents.LootPicked(drop.kind == LootKind.Item, position);
            ReleasePickup(pickup);
        }

        /// <summary>Thắng màn: đồ còn trên sân coi như đã nhặt hết (người chơi không phải chạy đi gom sau trận).</summary>
        public void CollectAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                LootPickup pickup = _active[i];
                _collected.Add(pickup.Drop);
                ReleasePickup(pickup);
            }
        }

        private void ReleasePickup(LootPickup pickup)
        {
            int index = _active.IndexOf(pickup);
            if (index >= 0)
            {
                int last = _active.Count - 1;
                _active[index] = _active[last];
                _active.RemoveAt(last);
            }
            _pickupPool.Release(pickup);
        }

        private void ShowLabel(LootDrop drop, Vector3 position)
        {
            if (_labelPool == null || !_labelPool.TryGet(position, Quaternion.identity, out FloatingText label)) return;

            label.OnReturnToPool = _releaseLabel;
            Vector3 at = position + Vector3.up * _labelOffsetY;
            // Chuỗi chỉ tạo lúc nhặt đồ (vài lần mỗi màn), không phải mỗi frame.
            if (drop.kind == LootKind.Item) label.Show(NameOf(drop), at, RarityColors.Get(drop.rarity));
            else label.Show($"+{drop.amount} {NameOf(drop)}", at, MaterialLabel);
        }

        private static ItemDatabase Database => GameServices.Inventory != null ? GameServices.Inventory.Database : null;

        private static Sprite IconOf(LootDrop drop)
        {
            ItemDatabase db = Database;
            if (db == null) return null;
            if (drop.kind == LootKind.Item) return db.GetItem(drop.id)?.icon;
            return db.GetMaterial(drop.id)?.icon;
        }

        private static string NameOf(LootDrop drop)
        {
            ItemDatabase db = Database;
            string name = null;
            if (db != null) name = drop.kind == LootKind.Item ? db.GetItem(drop.id)?.displayName : db.GetMaterial(drop.id)?.displayName;
            return string.IsNullOrEmpty(name) ? drop.id : name;
        }
    }
}
