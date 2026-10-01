using System.Collections.Generic;
using System.Linq;
using ArenaSurvival.RunUpgrades;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class RandomPickerTests
    {
        private static readonly string[] Upgrades = { "Damage", "FireRate", "MoveSpeed", "MaxHealth", "Heal" };

        private readonly List<string> _results = new List<string>();
        private readonly List<string> _buffer = new List<string>();

        [Test]
        public void PickDistinct_ReturnsRequestedCount()
        {
            var random = new System.Random(1);
            RandomPicker.PickDistinct(Upgrades, 3, _results, _buffer, random.Next);

            Assert.AreEqual(3, _results.Count);
        }

        [Test]
        public void PickDistinct_NeverReturnsDuplicates()
        {
            // Nhiều seed khác nhau để phủ nhiều cách trộn, không phụ thuộc may rủi của 1 lần chạy.
            for (int seed = 0; seed < 500; seed++)
            {
                var random = new System.Random(seed);
                RandomPicker.PickDistinct(Upgrades, 3, _results, _buffer, random.Next);

                Assert.AreEqual(_results.Count, _results.Distinct().Count(), $"seed {seed}: {string.Join(",", _results)}");
            }
        }

        [Test]
        public void PickDistinct_OnlyReturnsItemsFromSource()
        {
            var random = new System.Random(7);
            RandomPicker.PickDistinct(Upgrades, 3, _results, _buffer, random.Next);

            CollectionAssert.IsSubsetOf(_results, Upgrades);
        }

        [Test]
        public void PickDistinct_CountLargerThanSource_ReturnsWholeSource()
        {
            var random = new System.Random(3);
            RandomPicker.PickDistinct(Upgrades, 10, _results, _buffer, random.Next);

            CollectionAssert.AreEquivalent(Upgrades, _results);
        }

        [Test]
        public void PickDistinct_ZeroOrNegativeCount_ReturnsEmpty()
        {
            var random = new System.Random(3);
            RandomPicker.PickDistinct(Upgrades, 0, _results, _buffer, random.Next);
            Assert.IsEmpty(_results);

            RandomPicker.PickDistinct(Upgrades, -2, _results, _buffer, random.Next);
            Assert.IsEmpty(_results);
        }

        [Test]
        public void PickDistinct_EveryItemCanBeOffered()
        {
            // Nếu thuật toán trộn sai (ví dụ không bao giờ chạm tới phần tử cuối), sẽ có nâng cấp không bao giờ xuất hiện.
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 200; seed++)
            {
                var random = new System.Random(seed);
                RandomPicker.PickDistinct(Upgrades, 3, _results, _buffer, random.Next);
                seen.UnionWith(_results);
            }

            CollectionAssert.AreEquivalent(Upgrades, seen);
        }

        [Test]
        public void PickDistinct_DoesNotModifySource()
        {
            var source = new List<string>(Upgrades);
            var random = new System.Random(11);
            RandomPicker.PickDistinct(source, 3, _results, _buffer, random.Next);

            CollectionAssert.AreEqual(Upgrades, source);
        }
    }
}
