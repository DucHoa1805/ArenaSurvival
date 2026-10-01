using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class ChapterTests
    {
        private StageCatalog _catalog;
        private ChapterData _c1, _c2;
        private StageData _a, _b, _c;

        [SetUp]
        public void SetUp()
        {
            _a = ScriptableObject.CreateInstance<StageData>(); _a.id = "c1_s01";
            _b = ScriptableObject.CreateInstance<StageData>(); _b.id = "c1_s02";
            _c = ScriptableObject.CreateInstance<StageData>(); _c.id = "c2_s01";
            _c1 = ScriptableObject.CreateInstance<ChapterData>(); _c1.id = "c1"; _c1.stages = new[] { _a, _b };
            _c2 = ScriptableObject.CreateInstance<ChapterData>(); _c2.id = "c2"; _c2.stages = new[] { _c };
            _catalog = ScriptableObject.CreateInstance<StageCatalog>();
            _catalog.chapters = new[] { _c1, _c2 };
            _catalog.stages = new[] { _a, _b, _c };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in new Object[] { _a, _b, _c, _c1, _c2, _catalog }) Object.DestroyImmediate(o);
        }

        [Test]
        public void ChapterOf_FindsOwningChapter()
        {
            Assert.AreSame(_c1, _catalog.ChapterOf(_b));
            Assert.AreSame(_c2, _catalog.ChapterOf(_c));
            Assert.IsNull(_catalog.ChapterOf(null));
        }

        [Test]
        public void ChapterIndex_AndNextAcrossChapters()
        {
            Assert.AreEqual(1, _catalog.IndexOfChapter(_c2));
            Assert.AreSame(_c, _catalog.GetNext(_b), "màn cuối chương 1 → màn đầu chương 2");
        }

        [Test]
        public void ChapterOf_NoChapters_ReturnsNull()
        {
            _catalog.chapters = new ChapterData[0];
            Assert.IsNull(_catalog.ChapterOf(_a));
        }
    }
}
