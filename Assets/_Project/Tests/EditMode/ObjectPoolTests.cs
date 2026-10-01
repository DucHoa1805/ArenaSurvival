using ArenaSurvival.Core;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class ObjectPoolTests
    {
        private GameObject _prefab;
        private Transform _root; // mọi object pool tạo ra nằm dưới đây để dọn 1 lần

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("PoolTestPrefab");
            _root = new GameObject("PoolTestRoot").transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root.gameObject);
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void Prewarm_CreatesInactiveObjects()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, 3, _root);

            Assert.AreEqual(3, pool.CountAll);
            Assert.AreEqual(3, _root.childCount);
            foreach (Transform child in _root) Assert.IsFalse(child.gameObject.activeSelf);
        }

        [Test]
        public void TryGet_AtMaxSizeWithNoneFree_ReturnsFalseAndCreatesNothing()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, 2, _root, maxSize: 2);
            pool.TryGet(Vector3.zero, Quaternion.identity, out _);
            pool.TryGet(Vector3.zero, Quaternion.identity, out _);

            bool got = pool.TryGet(Vector3.zero, Quaternion.identity, out Transform third);

            Assert.IsFalse(got);
            Assert.IsNull(third);
            Assert.AreEqual(2, pool.CountAll);
        }

        [Test]
        public void TryGet_AfterRelease_ReusesSameObject()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, 1, _root, maxSize: 1);
            pool.TryGet(Vector3.zero, Quaternion.identity, out Transform first);
            pool.Release(first);

            bool got = pool.TryGet(Vector3.one, Quaternion.identity, out Transform again);

            Assert.IsTrue(got);
            Assert.AreSame(first, again);
            Assert.AreEqual(Vector3.one, again.position);
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void TryGet_NoMaxSize_GrowsLikeGet()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, 0, _root);

            for (int i = 0; i < 5; i++) Assert.IsTrue(pool.TryGet(Vector3.zero, Quaternion.identity, out _));

            Assert.AreEqual(5, pool.CountAll);
        }

        [Test]
        public void Release_Twice_DoesNotHandOutSameObjectTwice()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, 0, _root);
            Transform a = pool.Get(Vector3.zero, Quaternion.identity);
            pool.Release(a);
            pool.Release(a); // ví dụ 1 viên đạn chạm 2 quái trong cùng 1 bước vật lý

            Transform first = pool.Get(Vector3.zero, Quaternion.identity);
            Transform second = pool.Get(Vector3.zero, Quaternion.identity);

            Assert.AreNotSame(first, second);
        }
    }
}
