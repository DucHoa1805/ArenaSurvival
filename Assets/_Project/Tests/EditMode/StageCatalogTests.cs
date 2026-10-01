using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class StageCatalogTests
    {
        private StageCatalog _catalog;
        private StageData _a, _b, _c, _outsider;

        [SetUp]
        public void SetUp()
        {
            _a = ScriptableObject.CreateInstance<StageData>();
            _b = ScriptableObject.CreateInstance<StageData>();
            _c = ScriptableObject.CreateInstance<StageData>();
            _outsider = ScriptableObject.CreateInstance<StageData>();
            _catalog = ScriptableObject.CreateInstance<StageCatalog>();
            _catalog.stages = new[] { _a, _b, _c };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in new Object[] { _a, _b, _c, _outsider, _catalog }) Object.DestroyImmediate(o);
        }

        [Test]
        public void First_IsFirstStage() => Assert.AreSame(_a, _catalog.First);

        [Test]
        public void GetNext_MiddleStage_ReturnsFollowing() => Assert.AreSame(_c, _catalog.GetNext(_b));

        [Test]
        public void GetNext_LastStage_ReturnsNull() => Assert.IsNull(_catalog.GetNext(_c));

        [Test]
        public void GetNext_StageNotInCatalog_ReturnsNull() => Assert.IsNull(_catalog.GetNext(_outsider));

        [Test]
        public void IndexOf_FindsPosition()
        {
            Assert.AreEqual(1, _catalog.IndexOf(_b));
            Assert.AreEqual(-1, _catalog.IndexOf(_outsider));
        }

        [Test]
        public void EmptyCatalog_FirstIsNull()
        {
            _catalog.stages = new StageData[0];
            Assert.IsNull(_catalog.First);
        }
    }
}
