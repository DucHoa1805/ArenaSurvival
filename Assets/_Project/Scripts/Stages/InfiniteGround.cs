using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Nền vô tận (10D): tô các chunk 16×16 ô quanh camera và xóa chunk ra xa, nên tilemap luôn nhỏ dù người chơi chạy mãi.
    /// Chọn tile bằng GroundRules (tất định theo ô) → quay lại chỗ cũ thấy y hệt. Mỗi chương 1 prefab với bộ tile riêng.
    /// Chỉ vẽ lúc camera sang chunk mới (vài lần mỗi giây khi chạy), dùng mảng tạo sẵn nên không cấp phát.
    /// </summary>
    public class InfiniteGround : MonoBehaviour
    {
        [Serializable]
        public class Patch
        {
            [Tooltip("Tile của mảng (bãi cỏ, vệt đất, mảng bê tông...).")]
            public TileBase[] tiles = new TileBase[0];
            [Tooltip("Tần số nhiễu: nhỏ = mảng to.")]
            public float scale = 0.08f;
            [Range(0f, 1f)] public float threshold = 0.65f;
            [Tooltip(">0: mảng là khối chữ nhật theo lưới (cạnh thẳng — ô phố, sân bê tông) thay vì nhiễu. threshold = tỉ lệ khối được phủ.")]
            public int blockSize;
            [Tooltip("Lề trong mỗi khối (ô) để các mảng cạnh nhau tách rời.")]
            public int blockMargin = 1;
        }

        [SerializeField] private Tilemap _ground;
        [SerializeField] private Tilemap _details;
        [SerializeField] private Tilemap _foliage;
        [SerializeField] private int _seed = 1;

        [Header("Tiles")]
        [SerializeField] private TileBase[] _baseTiles = new TileBase[0];
        [Tooltip("Mảng phủ lên nền chính theo nhiễu; phần tử sau đè phần tử trước.")]
        [SerializeField] private Patch[] _patches = new Patch[0];
        [SerializeField] private TileBase[] _decorTiles = new TileBase[0];
        [Range(0f, 0.2f)] [SerializeField] private float _decorDensity = 0.03f;
        [SerializeField] private TileBase[] _foliageTiles = new TileBase[0];
        [Range(0f, 0.2f)] [SerializeField] private float _foliageDensity = 0.012f;

        [Header("Chunks")]
        [SerializeField] private int _chunkSize = 16;
        [Tooltip("Số chunk mỗi phía quanh chunk của camera (2 = lưới 5×5, đủ phủ màn hình ngang 16:9 khi zoom 5).")]
        [SerializeField] private int _radius = 2;

        private Camera _camera;
        private Vector2Int _centerChunk = new Vector2Int(int.MinValue, int.MinValue);
        private readonly HashSet<Vector2Int> _painted = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _toRemove = new List<Vector2Int>(32);
        private TileBase[] _groundBuffer, _detailBuffer, _foliageBuffer, _emptyBuffer;

        private void Awake()
        {
            int n = _chunkSize * _chunkSize;
            _groundBuffer = new TileBase[n];
            _detailBuffer = new TileBase[n];
            _foliageBuffer = new TileBase[n];
            _emptyBuffer = new TileBase[n];
            // Prefab có thể còn ô vẽ sẵn để xem trong Editor: xóa để toàn bộ nền đi qua đường tô chunk.
            if (_ground != null) _ground.ClearAllTiles();
            if (_details != null) _details.ClearAllTiles();
            if (_foliage != null) _foliage.ClearAllTiles();
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            Vector3Int cell = _ground.WorldToCell(_camera.transform.position);
            Vector2Int center = GroundRules.ChunkOf(cell.x, cell.y, _chunkSize);
            if (center == _centerChunk) return;
            _centerChunk = center;
            Refresh(center);
        }

        private void Refresh(Vector2Int center)
        {
            _toRemove.Clear();
            foreach (Vector2Int chunk in _painted)
                if (!GroundRules.InRange(chunk, center, _radius + 1)) _toRemove.Add(chunk);
            foreach (Vector2Int chunk in _toRemove)
            {
                SetChunk(chunk, _emptyBuffer, _emptyBuffer, _emptyBuffer);
                _painted.Remove(chunk);
            }

            for (int cx = center.x - _radius; cx <= center.x + _radius; cx++)
            for (int cy = center.y - _radius; cy <= center.y + _radius; cy++)
            {
                var chunk = new Vector2Int(cx, cy);
                if (_painted.Contains(chunk)) continue;
                Fill(chunk);
                SetChunk(chunk, _groundBuffer, _detailBuffer, _foliageBuffer);
                _painted.Add(chunk);
            }
        }

        private void Fill(Vector2Int chunk)
        {
            int ox = chunk.x * _chunkSize, oy = chunk.y * _chunkSize;
            for (int j = 0; j < _chunkSize; j++)
            for (int i = 0; i < _chunkSize; i++)
            {
                int x = ox + i, y = oy + j, k = j * _chunkSize + i;
                _groundBuffer[k] = GroundAt(x, y);
                _detailBuffer[k] = GroundRules.Scatter(x, y, _seed + 101, _decorDensity) ? PickFrom(_decorTiles, x, y, _seed + 102) : null;
                _foliageBuffer[k] = GroundRules.Scatter(x, y, _seed + 201, _foliageDensity) ? PickFrom(_foliageTiles, x, y, _seed + 202) : null;
            }
        }

        private TileBase GroundAt(int x, int y)
        {
            TileBase tile = PickFrom(_baseTiles, x, y, _seed);
            for (int p = 0; p < _patches.Length; p++)
            {
                Patch patch = _patches[p];
                if (patch.tiles.Length > 0 && InPatch(patch, x, y, _seed + 11 * (p + 1)))
                    tile = PickFrom(patch.tiles, x, y, _seed + 13 * (p + 1));
            }
            return tile;
        }

        private static bool InPatch(Patch patch, int x, int y, int seed)
        {
            if (patch.blockSize <= 0) return GroundRules.Noise(x, y, patch.scale, seed) > patch.threshold;
            return GroundRules.InBlockPatch(x, y, patch.blockSize, patch.blockMargin, patch.threshold, seed);
        }

        private static TileBase PickFrom(TileBase[] tiles, int x, int y, int seed)
        {
            int i = GroundRules.Pick(x, y, seed, tiles.Length);
            return i >= 0 ? tiles[i] : null;
        }

        private void SetChunk(Vector2Int chunk, TileBase[] ground, TileBase[] details, TileBase[] foliage)
        {
            var bounds = new BoundsInt(chunk.x * _chunkSize, chunk.y * _chunkSize, 0, _chunkSize, _chunkSize, 1);
            if (_ground != null) _ground.SetTilesBlock(bounds, ground);
            if (_details != null) _details.SetTilesBlock(bounds, details);
            if (_foliage != null) _foliage.SetTilesBlock(bounds, foliage);
        }
    }
}
