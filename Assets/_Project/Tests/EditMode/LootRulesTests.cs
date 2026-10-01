using System;
using System.Collections.Generic;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArenaSurvival.Tests
{
    public class LootRulesTests
    {
        private MaterialData _leather;
        private MaterialData _ore;
        private ItemData _ring;
        private LootTable _table;
        private readonly List<LootDrop> _out = new List<LootDrop>();

        [SetUp]
        public void SetUp()
        {
            _leather = ScriptableObject.CreateInstance<MaterialData>(); _leather.id = "leather";
            _ore = ScriptableObject.CreateInstance<MaterialData>(); _ore.id = "ore";
            _ring = ScriptableObject.CreateInstance<ItemData>(); _ring.id = "acc_ring_power";
            _table = ScriptableObject.CreateInstance<LootTable>();
            _out.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_leather);
            Object.DestroyImmediate(_ore);
            Object.DestroyImmediate(_ring);
            Object.DestroyImmediate(_table);
        }

        private static Func<float> Seeded(int seed)
        {
            var rng = new System.Random(seed);
            return () => (float)rng.NextDouble();
        }

        [Test]
        public void PickWeighted_PicksByCumulativeWeight()
        {
            float[] w = { 1f, 3f, 0f, 1f };
            Assert.AreEqual(0, LootRules.PickWeighted(w, 4, 0.0f));
            Assert.AreEqual(1, LootRules.PickWeighted(w, 4, 0.25f));
            Assert.AreEqual(1, LootRules.PickWeighted(w, 4, 0.79f));
            Assert.AreEqual(3, LootRules.PickWeighted(w, 4, 0.81f));
            Assert.AreEqual(3, LootRules.PickWeighted(w, 4, 0.9999f));
            Assert.AreEqual(-1, LootRules.PickWeighted(new float[] { 0f, 0f }, 2, 0.5f), "tổng trọng số 0 → không chọn gì");
        }

        [Test]
        public void Roll_Guaranteed_AlwaysDropsWithinMinMax()
        {
            _table.guaranteed = new[] { new LootEntry { material = _leather, min = 2, max = 4 } };
            _table.rolls = 0;
            Func<float> rng = Seeded(1);
            for (int i = 0; i < 200; i++)
            {
                _out.Clear();
                LootRules.Roll(_table, rng, _out);
                Assert.AreEqual(1, _out.Count);
                Assert.AreEqual(LootKind.Material, _out[0].kind);
                Assert.AreEqual("leather", _out[0].id);
                Assert.That(_out[0].amount, Is.InRange(2, 4));
            }
        }

        [Test]
        public void Roll_Weighted_DistributionMatchesWeights()
        {
            _table.rolls = 1;
            _table.weighted = new[]
            {
                new LootEntry { material = _leather, weight = 3f },
                new LootEntry { material = _ore, weight = 1f },
            };
            _table.nothingWeight = 4f;

            int leather = 0, ore = 0, nothing = 0;
            const int N = 10000;
            Func<float> rng = Seeded(42);
            for (int i = 0; i < N; i++)
            {
                _out.Clear();
                LootRules.Roll(_table, rng, _out);
                if (_out.Count == 0) nothing++;
                else if (_out[0].id == "leather") leather++;
                else ore++;
            }

            Assert.AreEqual(0.375, leather / (double)N, 0.02);
            Assert.AreEqual(0.125, ore / (double)N, 0.02);
            Assert.AreEqual(0.5, nothing / (double)N, 0.02);
        }

        [Test]
        public void Roll_Item_RarityDistributionMatchesWeights()
        {
            _table.rolls = 1;
            _table.weighted = new[] { new LootEntry { item = _ring } };
            _table.rarityWeights = new[] { 0f, 50f, 40f, 10f };

            var counts = new int[4];
            const int N = 10000;
            Func<float> rng = Seeded(7);
            for (int i = 0; i < N; i++)
            {
                _out.Clear();
                LootRules.Roll(_table, rng, _out);
                Assert.AreEqual(LootKind.Item, _out[0].kind);
                Assert.AreEqual(1, _out[0].amount, "trang bị luôn rơi từng món");
                counts[(int)_out[0].rarity]++;
            }

            Assert.AreEqual(0, counts[0]);
            Assert.AreEqual(0.5, counts[1] / (double)N, 0.02);
            Assert.AreEqual(0.4, counts[2] / (double)N, 0.02);
            Assert.AreEqual(0.1, counts[3] / (double)N, 0.02);
        }

        [Test]
        public void Roll_MultipleRolls_AppendsEachResult()
        {
            _table.rolls = 3;
            _table.weighted = new[] { new LootEntry { material = _ore } };
            LootRules.Roll(_table, Seeded(3), _out);
            Assert.AreEqual(3, _out.Count);
        }

        [Test]
        public void Roll_AlsoRoll_RollsSubTablesToo()
        {
            var shards = ScriptableObject.CreateInstance<LootTable>();
            try
            {
                shards.rolls = 0;
                shards.guaranteed = new[] { new LootEntry { material = _ore, min = 3, max = 3 } };
                _table.rolls = 0;
                _table.guaranteed = new[] { new LootEntry { material = _leather } };
                _table.alsoRoll = new[] { shards, null };

                LootRules.Roll(_table, Seeded(1), _out);

                Assert.AreEqual(2, _out.Count);
                Assert.AreEqual("leather", _out[0].id);
                Assert.AreEqual("ore", _out[1].id);
                Assert.AreEqual(3, _out[1].amount);
            }
            finally { Object.DestroyImmediate(shards); }
        }

        [Test]
        public void Roll_SelfReferencingAlsoRoll_DoesNotLoopForever()
        {
            _table.rolls = 0;
            _table.guaranteed = new[] { new LootEntry { material = _leather } };
            _table.alsoRoll = new[] { _table };

            LootRules.Roll(_table, Seeded(1), _out);

            Assert.LessOrEqual(_out.Count, LootRules.MaxAlsoRollDepth + 1, "độ sâu bị chặn");
        }

        [Test]
        public void Roll_NullTableOrEmptyEntry_AddsNothing()
        {
            LootRules.Roll(null, Seeded(1), _out);
            Assert.AreEqual(0, _out.Count);

            _table.guaranteed = new[] { new LootEntry() }; // không gán material lẫn item
            _table.rolls = 0;
            LootRules.Roll(_table, Seeded(1), _out);
            Assert.AreEqual(0, _out.Count);
        }
    }
}
