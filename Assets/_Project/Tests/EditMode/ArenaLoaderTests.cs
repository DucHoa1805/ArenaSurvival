using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class ArenaLoaderTests
    {
        [Test]
        public void NeedsSwap_OnlyWhenChapterHasDifferentArena()
        {
            var desert = new GameObject("Arena");
            var city = new GameObject("Arena_City");
            try
            {
                Assert.IsFalse(ArenaLoader.NeedsSwap(desert, desert), "cùng sân → giữ sân trong scene");
                Assert.IsFalse(ArenaLoader.NeedsSwap(desert, null), "chương chưa gán sân → giữ sân trong scene");
                Assert.IsTrue(ArenaLoader.NeedsSwap(desert, city));
                Assert.IsTrue(ArenaLoader.NeedsSwap(null, city), "scene không khai báo nguồn → vẫn dùng sân của chương");
            }
            finally
            {
                Object.DestroyImmediate(desert);
                Object.DestroyImmediate(city);
            }
        }
    }
}
