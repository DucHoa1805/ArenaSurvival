using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvival.Core
{
    /// <summary>
    /// Object pool generic đơn giản, tự viết tay (không dùng UnityEngine.Pool.ObjectPool)
    /// để hiểu rõ cơ chế bên trong trước khi so sánh với bản có sẵn của Unity.
    ///
    /// Cách hoạt động:
    /// - Get(): nếu còn object rảnh trong pool thì lấy ra dùng lại; nếu hết thì Instantiate mới.
    /// - Release(): không Destroy object, chỉ SetActive(false) và cất lại vào pool để dùng lần sau.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new Queue<T>();

        public ObjectPool(T prefab, int prewarmCount = 0, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            // Prewarm: tạo sẵn 1 số lượng object từ đầu, tránh phải Instantiate ngay lúc gameplay
            // đang căng thẳng nhất (ví dụ wave quái đầu tiên).
            for (int i = 0; i < prewarmCount; i++)
            {
                T instance = CreateNew();
                instance.gameObject.SetActive(false);
                _available.Enqueue(instance);
            }
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance = _available.Count > 0 ? _available.Dequeue() : CreateNew();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }

        private T CreateNew()
        {
            return Object.Instantiate(_prefab, _parent);
        }
    }
}