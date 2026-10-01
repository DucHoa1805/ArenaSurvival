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
        private readonly int _maxSize;

        /// <summary>Tổng số object pool đã tạo (đang dùng + đang rảnh).</summary>
        public int CountAll { get; private set; }

        /// <param name="maxSize">
        /// 0 = không giới hạn. Đặt giới hạn cho thứ chỉ mang tính trang trí (hiệu ứng, số sát thương): lúc đông quái,
        /// cứ mỗi đợt đỉnh mới pool lại Instantiate thêm (đo thấy ~400 lần cấp phát, ~40 KB trong 1 frame với object TMP),
        /// tức giật giữa trận. Có giới hạn + tạo sẵn đủ số đó thì lúc chơi không còn Instantiate nào.
        /// </param>
        public ObjectPool(T prefab, int prewarmCount = 0, Transform parent = null, int maxSize = 0)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;

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
            Activate(instance, position, rotation);
            return instance;
        }

        /// <summary>
        /// Như Get nhưng tôn trọng maxSize: hết object rảnh mà pool đã đủ số tối đa thì trả về false thay vì tạo mới.
        /// Dùng cho thứ bỏ qua được (thêm 1 tia lửa khi đã có 60 cái trên màn hình thì không ai thấy khác).
        /// </summary>
        public bool TryGet(Vector3 position, Quaternion rotation, out T instance)
        {
            if (_available.Count == 0 && _maxSize > 0 && CountAll >= _maxSize)
            {
                instance = null;
                return false;
            }

            instance = Get(position, rotation);
            return true;
        }

        private static void Activate(T instance, Vector3 position, Quaternion rotation)
        {
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
        }

        public void Release(T instance)
        {
            // Object đã tắt nghĩa là đã nằm trong pool rồi. Bỏ qua để 1 object không bị xếp vào
            // hàng đợi 2 lần (ví dụ 1 viên đạn chạm 2 quái trong cùng 1 bước vật lý) — nếu không,
            // 2 lần Get() sau đó sẽ trả về cùng 1 object.
            if (!instance.gameObject.activeSelf) return;

            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }

        private T CreateNew()
        {
            CountAll++;
            return Object.Instantiate(_prefab, _parent);
        }
    }
}