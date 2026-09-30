# Kế hoạch dự án Unity: 2D Arena Survival

> Mục tiêu: vừa làm vừa học, tạo ra một dự án nhỏ nhưng đủ để chứng minh các kỹ năng Unity/C# khi đi phỏng vấn.
> Thời lượng dự kiến: **6 tuần** (khoảng 10–15 giờ/tuần). Có thể kéo giãn nếu bạn học song song việc khác.

---

## PHẦN A. PHÂN TÍCH TỔNG QUAN

### A1. Bài toán và mục tiêu

| Câu hỏi | Trả lời |
|---|---|
| Làm để làm gì? | Có 1 sản phẩm chạy được để đưa vào CV/portfolio và trả lời phỏng vấn bằng ví dụ thật |
| Nhà tuyển dụng muốn thấy gì? | Code sạch, hiểu vòng đời Unity, biết tối ưu, biết thiết kế hệ thống, biết hoàn thiện sản phẩm |
| Rủi ro lớn nhất? | Làm quá to, không xong; hoặc xong nhưng code là một mớ script dài, khó giải thích |
| Nguyên tắc chủ đạo | **Nhỏ nhưng hoàn chỉnh** hơn là lớn nhưng dở dang |

### A2. Ý tưởng game

**2D Top-down Arena Survival** (mô phỏng thu nhỏ kiểu Vampire Survivors).

- Người chơi điều khiển nhân vật trong một đấu trường.
- Vũ khí **tự động bắn** vào kẻ địch gần nhất.
- Quái spawn theo từng wave, độ khó tăng dần.
- Quái chết rơi **XP**, đủ XP thì **lên cấp** và chọn 1 trong 3 nâng cấp.
- Thua khi HP về 0, lưu điểm cao nhất.

**Core loop (vòng lặp chính):**

```
Di chuyển/né  →  Vũ khí tự bắn  →  Hạ quái  →  Nhặt XP
      ↑                                              ↓
  Sống sót lâu hơn  ←  Chọn nâng cấp  ←  Lên cấp
```

### A3. Vì sao chọn thể loại này

- **2D**: bớt gánh nặng về model, animation 3D, ánh sáng; tập trung vào lập trình.
- **Top-down, không cần level design phức tạp**: 1 màn duy nhất là đủ.
- **Tự bắn**: bỏ được nhiều logic aim/input phức tạp, nhưng vẫn cần pooling, targeting, va chạm.
- **Bản chất data-driven** (quái, vũ khí, nâng cấp): rất hợp để show ScriptableObject.
- **Nhiều đối tượng cùng lúc**: tạo ra vấn đề hiệu năng thật để bạn dùng Profiler.

### A4. Phạm vi (MoSCoW)

| Mức | Nội dung |
|---|---|
| **Must have** | Di chuyển, bắn tự động, 2–3 loại quái, spawner theo wave, HP, XP + lên cấp + chọn nâng cấp, HUD, menu chính, game over, lưu điểm cao, object pooling, ScriptableObject, event system, game state |
| **Should have** | Âm thanh, particle hiệu ứng, animation cơ bản, pause menu, 2–3 loại vũ khí, unit test cho logic thuần |
| **Could have** | Boss, gamepad, camera shake, cài đặt âm lượng, WebGL build |
| **Won't have** (lần này) | Multiplayer, shop, nhiều màn chơi, save cloud, procedural generation |

> Quy tắc: chỉ chuyển sang mục "Should" khi toàn bộ "Must" đã chạy ổn.

### A5. Công nghệ sử dụng

| Hạng mục | Lựa chọn | Lý do |
|---|---|---|
| Engine | Unity 6 LTS (hoặc bản LTS mới nhất) | Ổn định, phổ biến ở công ty |
| Template | 2D (URP) | Pipeline hiện đại, đủ nhẹ |
| Input | Input System package | Thay thế hệ thống input cũ, được hỏi nhiều |
| IDE | Visual Studio / Rider / VS Code | Tùy quen tay |
| Version control | Git + GitHub | Nhà tuyển dụng sẽ mở repo |
| Art | Asset miễn phí (Kenney.nl, itch.io) | Không mất thời gian vẽ |
| Audio | Freesound, Kenney, Pixabay | Nhớ ghi nguồn/giấy phép |

---

## PHẦN B. KIẾN TRÚC TỔNG THỂ

### B1. Các hệ thống chính

```
┌────────────────────────────────────────────────────────┐
│                      GameManager                       │
│         (State machine: Menu/Playing/Paused/Over)      │
└───────────────┬────────────────────────────────────────┘
                │ phát sự kiện (GameEvents)
   ┌────────────┼─────────────┬───────────────┬──────────┐
   ▼            ▼             ▼               ▼          ▼
Player      EnemySpawner   Progression      UI/HUD     Audio
(move,      (wave, pool)   (XP, level,     (HP, XP,   (SFX,
 weapon)                    upgrades)       menus)     music)
   │            │             │
   └─ Health ───┴─ Health     └─ dùng UpgradeData (ScriptableObject)
      (dùng chung cho Player và Enemy)
```

**Nguyên tắc thiết kế:**

1. **Mỗi script một trách nhiệm** (Single Responsibility).
2. **Hệ thống giao tiếp qua event**, không gọi trực tiếp nhau, để giảm coupling.
3. **Dữ liệu tách khỏi logic** bằng ScriptableObject.
4. **Không dùng `Find`/`GetComponent` trong `Update`**: cache tham chiếu từ `Awake`/`Start`.
5. **Object nào sinh ra hàng loạt thì phải pool**.

### B2. Cấu trúc thư mục

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/          # GameManager, GameState, GameEvents, ObjectPool
│   │   ├── Player/        # PlayerController, PlayerWeapon
│   │   ├── Enemies/       # Enemy, EnemySpawner, EnemyData
│   │   ├── Combat/        # Health, Projectile, DamageInfo
│   │   ├── Progression/   # ExperienceSystem, UpgradeData, UpgradeManager
│   │   ├── UI/            # HudController, LevelUpPanel, MainMenu
│   │   ├── Save/          # SaveSystem, SaveData
│   │   └── Audio/         # AudioManager
│   ├── Data/              # ScriptableObject assets (Enemies, Weapons, Upgrades)
│   ├── Prefabs/
│   ├── Scenes/            # MainMenu, Game
│   ├── Art/  Audio/  Animations/
│   └── Tests/             # EditMode tests
└── Settings/
```

### B3. Quy ước code

- Namespace theo thư mục: `ArenaSurvival.Combat`, `ArenaSurvival.Core`...
- Field private dùng `[SerializeField] private`, không public bừa bãi.
- Tên: `PascalCase` cho class/method, `_camelCase` cho field private.
- Không để logic gameplay trong UI script và ngược lại.
- Comment giải thích **vì sao**, không giải thích **cái gì**.

---

## PHẦN C. LỘ TRÌNH CHI TIẾT THEO GIAI ĐOẠN

### Tổng quan tiến độ

| Giai đoạn | Thời gian | Nội dung chính | Sản phẩm cuối giai đoạn |
|---|---|---|---|
| Phase 0 | 2–3 ngày | Chuẩn bị môi trường, thiết kế | Repo + project trống + design doc |
| Phase 1 | Tuần 1 | Nền tảng Unity, Player | Nhân vật di chuyển, camera theo |
| Phase 2 | Tuần 2 | Combat cốt lõi | Bắn, quái đuổi, va chạm, pooling |
| Phase 3 | Tuần 3 | Kiến trúc và dữ liệu | ScriptableObject, event, state machine |
| Phase 4 | Tuần 4 | Tiến trình và UI | XP, lên cấp, nâng cấp, HUD, menu |
| Phase 5 | Tuần 5 | Hoàn thiện và lưu trữ | Âm thanh, VFX, save, độ khó |
| Phase 6 | Tuần 6 | Tối ưu, build, portfolio | Bản build, README, video demo |

---

### PHASE 0: Chuẩn bị (2–3 ngày)

**Mục tiêu:** có nền móng sạch để không phải sửa lại sau.

**Việc cần làm**

- [ ] Cài Unity Hub, Unity 6 LTS, kèm module Windows/Android/WebGL nếu cần.
- [ ] Tạo project từ template 2D (URP).
- [ ] Tạo repo GitHub, thêm `.gitignore` chuẩn cho Unity, thiết lập `Force Text` cho serialization.
- [ ] Tạo cấu trúc thư mục ở mục B2.
- [ ] Viết **design doc 1 trang**: ý tưởng, core loop, danh sách tính năng Must/Should.
- [ ] Chọn và tải bộ asset (sprite nhân vật, quái, đạn, nền, UI).
- [ ] Cài package: Input System, Cinemachine (tùy chọn), TextMeshPro.

**Kiến thức cần nắm**
- Unity Editor: Scene, Hierarchy, Inspector, Project, Game view.
- GameObject – Component – Prefab là gì.

**Tiêu chí hoàn thành:** mở project lên chạy được scene trống, đã commit đầu tiên lên GitHub.

---

### PHASE 1: Nền tảng và Player (Tuần 1)

**Mục tiêu:** hiểu vòng đời Unity, làm được nhân vật điều khiển mượt.

**Việc cần làm**

- [ ] Dựng scene `Game`: nền, camera 2D (orthographic).
- [ ] Tạo prefab Player: SpriteRenderer, Rigidbody2D, Collider2D.
- [ ] Cấu hình **Input Actions** (Move), nhận input qua `InputAction` hoặc `PlayerInput`.
- [ ] Viết `PlayerController`: di chuyển bằng `Rigidbody2D` trong `FixedUpdate`.
- [ ] Lật sprite theo hướng di chuyển.
- [ ] Camera đi theo mượt (Cinemachine hoặc tự viết `LateUpdate` + `Lerp`).
- [ ] Giới hạn đấu trường bằng tường (collider) hoặc clamp vị trí.

**Kiến thức cần nắm**
- Vòng đời: `Awake` → `OnEnable` → `Start` → `Update`/`FixedUpdate`/`LateUpdate` → `OnDisable` → `OnDestroy`.
- `Time.deltaTime` vs `Time.fixedDeltaTime`.
- Vì sao vật lý phải làm trong `FixedUpdate`.
- Rigidbody2D: Dynamic/Kinematic/Static, `linearVelocity` (hoặc `MovePosition`).

**Đoạn code minh họa**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private InputActionReference _moveAction;

        private Rigidbody2D _rb;
        private Vector2 _input;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();
        private void OnEnable() => _moveAction.action.Enable();
        private void OnDisable() => _moveAction.action.Disable();

        private void Update() => _input = _moveAction.action.ReadValue<Vector2>();

        private void FixedUpdate()
        {
            _rb.MovePosition(_rb.position + _input.normalized * (_moveSpeed * Time.fixedDeltaTime));
        }
    }
}
```

**Câu hỏi phỏng vấn liên quan**
- `Update` khác `FixedUpdate` khác `LateUpdate` thế nào?
- Vì sao dùng `Time.deltaTime`?
- Input System mới khác input cũ ở điểm nào?

**Tiêu chí hoàn thành:** di chuyển 8 hướng mượt, không xuyên tường, camera theo ổn.

---

### PHASE 2: Combat cốt lõi (Tuần 2)

**Mục tiêu:** có vòng lặp chơi đầu tiên (bắn – giết – bị đánh) và tự cài object pooling.

**Việc cần làm**

- [ ] `Health`: máu hiện tại/tối đa, `TakeDamage`, sự kiện `OnDied`, `OnDamaged`. **Dùng chung cho Player và Enemy.**
- [ ] `Projectile`: bay thẳng, gây sát thương khi chạm, tự hủy sau thời gian/quá xa.
- [ ] `PlayerWeapon`: tìm kẻ địch gần nhất trong tầm, bắn theo tốc độ bắn (cooldown).
- [ ] `Enemy`: đuổi theo Player, gây sát thương khi chạm.
- [ ] `EnemySpawner`: spawn quanh rìa camera theo chu kỳ.
- [ ] **Bước 1:** làm bằng `Instantiate`/`Destroy` để thấy vấn đề.
- [ ] **Bước 2:** tự viết `ObjectPool` đơn giản.
- [ ] **Bước 3:** thay bằng `UnityEngine.Pool.ObjectPool<T>` và so sánh.
- [ ] Cấu hình **Layer + Collision Matrix**: Player, Enemy, PlayerProjectile, Pickup.

**Kiến thức cần nắm**
- Collider vs Trigger; `OnCollisionEnter2D` vs `OnTriggerEnter2D`.
- Vì sao `Instantiate`/`Destroy` liên tục gây **GC spike**.
- Object Pooling: Get / Release, reset trạng thái khi lấy ra.
- `Physics2D.OverlapCircleAll` / `OverlapCircleNonAlloc` để tìm mục tiêu, tránh cấp phát.

**Đoạn code minh họa (pool bằng API có sẵn)**

```csharp
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile _prefab;
    private ObjectPool<Projectile> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<Projectile>(
            createFunc: () => Instantiate(_prefab, transform),
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            defaultCapacity: 50, maxSize: 300);
    }

    public Projectile Get() => _pool.Get();
    public void Release(Projectile p) => _pool.Release(p);
}
```

**Câu hỏi phỏng vấn liên quan**
- Object pooling giải quyết vấn đề gì? Khi nào không nên dùng?
- Garbage Collection ảnh hưởng game thế nào?
- Layer Collision Matrix để làm gì?

**Tiêu chí hoàn thành:** chơi được vài phút, 100+ quái và đạn cùng lúc không giật, không còn `Instantiate` trong lúc chơi.

---

### PHASE 3: Kiến trúc và dữ liệu (Tuần 3)

**Mục tiêu:** biến prototype thành code có cấu trúc. **Đây là giai đoạn tạo khác biệt lớn nhất khi phỏng vấn.**

**Việc cần làm**

- [ ] **ScriptableObject**:
  - `EnemyData` (máu, tốc độ, sát thương, XP rơi, sprite).
  - `WeaponData` (sát thương, tốc độ bắn, tầm, tốc độ đạn).
  - `UpgradeData` (tên, mô tả, icon, loại, giá trị).
  - `WaveData` (danh sách quái, số lượng, thời gian).
- [ ] **Event system**: dùng C# `event`/`Action` hoặc ScriptableObject Event Channel.
- [ ] **GameManager + State Machine**: `Menu → Playing → Paused → GameOver`; dùng `Time.timeScale` để pause.
- [ ] **Singleton có kiểm soát** (chỉ cho GameManager/AudioManager) và biết nhược điểm của nó.
- [ ] Refactor: rà soát script nào đang biết quá nhiều về script khác.
- [ ] Thêm **assembly definition** (`.asmdef`) cho Core/Gameplay (tùy chọn nhưng ăn điểm).

**Kiến thức cần nắm**
- ScriptableObject là gì, khác MonoBehaviour ở đâu, khi nào dùng.
- Observer pattern, coupling/cohesion.
- State pattern, Singleton (ưu/nhược), Dependency Injection ở mức khái niệm.
- **Bẫy event:** luôn hủy đăng ký ở `OnDisable`/`OnDestroy` để tránh rò rỉ và lỗi tham chiếu.

**Đoạn code minh họa**

```csharp
// Dữ liệu tách khỏi logic
[CreateAssetMenu(menuName = "ArenaSurvival/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string displayName;
    public float maxHealth = 10f;
    public float moveSpeed = 2f;
    public float contactDamage = 5f;
    public int xpReward = 1;
    public Sprite sprite;
}

// Event tĩnh đơn giản
public static class GameEvents
{
    public static event System.Action<int> OnXpGained;
    public static event System.Action<int> OnLevelUp;
    public static event System.Action OnPlayerDied;

    public static void XpGained(int amount) => OnXpGained?.Invoke(amount);
    public static void LevelUp(int level) => OnLevelUp?.Invoke(level);
    public static void PlayerDied() => OnPlayerDied?.Invoke();
}

// State machine gọn bằng enum
public enum GameState { Menu, Playing, Paused, GameOver }
```

**Câu hỏi phỏng vấn liên quan**
- ScriptableObject dùng để làm gì? Cho ví dụ trong dự án của bạn.
- Bạn giảm phụ thuộc giữa các hệ thống thế nào?
- Singleton có vấn đề gì? Bạn dùng ở đâu, tránh ở đâu?
- Rò rỉ event xảy ra khi nào?

**Tiêu chí hoàn thành:** thêm một loại quái mới chỉ bằng cách tạo asset (không sửa code), các hệ thống không tham chiếu chéo trực tiếp.

---

### PHASE 4: Tiến trình và UI (Tuần 4)

**Mục tiêu:** có vòng lặp tiến triển và giao diện hoàn chỉnh.

**Việc cần làm**

- [ ] `Pickup XP`: quái chết rơi orb, orb bị hút về Player khi lại gần.
- [ ] `ExperienceSystem`: tính XP cần cho cấp tiếp theo (công thức tăng dần), phát `OnLevelUp`.
- [ ] `UpgradeManager`: khi lên cấp, **pause game**, chọn ngẫu nhiên 3 nâng cấp, áp dụng lựa chọn.
- [ ] Nâng cấp mẫu: +sát thương, +tốc độ bắn, +tốc độ chạy, +máu tối đa, hồi máu.
- [ ] **HUD**: thanh HP, thanh XP, cấp độ, thời gian sống sót, số quái đã giết.
- [ ] **Menu**: Main Menu (Play/Quit), Pause, Game Over (điểm, Retry, về Menu).
- [ ] Chuyển scene bằng `SceneManager` (thử `LoadSceneAsync`).

**Kiến thức cần nắm**
- Canvas: Screen Space Overlay, Canvas Scaler, Anchor, Layout Group.
- UI **event-driven**: UI chỉ lắng nghe sự kiện, không bị gameplay gọi trực tiếp và không tự chứa logic game.
- Vì sao không cập nhật text UI trong `Update` mỗi frame nếu không cần.
- `Time.timeScale = 0` ảnh hưởng gì (animation, coroutine, `WaitForSecondsRealtime`).
- Coroutine: bản chất, khi nào dùng, khác `async/await` ra sao.

**Câu hỏi phỏng vấn liên quan**
- Bạn tổ chức UI và gameplay tách rời thế nào?
- Coroutine hoạt động ra sao, có hạn chế gì?
- Cách chuyển scene và giữ dữ liệu giữa các scene?

**Tiêu chí hoàn thành:** chơi một lượt trọn vẹn từ Menu → Chơi → Lên cấp chọn nâng cấp → Thua → Game Over → Retry, không lỗi.

---

### PHASE 5: Hoàn thiện và lưu trữ (Tuần 5)

**Mục tiêu:** game có cảm giác "xong", có lưu điểm và độ khó cân bằng.

**Việc cần làm**

- [ ] **Save system**: lưu điểm cao/thời gian sống lâu nhất bằng JSON (`JsonUtility` + `Application.persistentDataPath`).
- [ ] **Âm thanh**: `AudioManager`, `AudioMixer` (nhóm Music/SFX), chỉnh âm lượng.
- [ ] **VFX**: Particle System cho quái chết, trúng đạn, lên cấp.
- [ ] **Animation**: Animator cho Player/Enemy (idle/run/hit) hoặc animation bằng code (DOTween tùy chọn).
- [ ] **Độ khó tăng dần**: `WaveData` tăng số lượng/máu quái theo thời gian.
- [ ] **Game feel**: camera shake nhẹ, flash khi bị đánh, hiển thị số sát thương (tùy chọn).
- [ ] Thêm loại vũ khí/quái thứ 2–3 bằng ScriptableObject để chứng minh kiến trúc mở rộng được.
- [ ] Viết **Unit test EditMode** cho logic thuần (công thức XP, chọn nâng cấp ngẫu nhiên, save/load).

**Kiến thức cần nắm**
- Serialization: `JsonUtility` vs `PlayerPrefs` vs file; khi nào dùng cái nào; nhược điểm `PlayerPrefs`.
- AudioSource/AudioClip/AudioMixer.
- Animator Controller, Animation Event, parameter.
- Unity Test Framework (EditMode vs PlayMode).

**Câu hỏi phỏng vấn liên quan**
- Bạn lưu dữ liệu thế nào? Nếu cần bảo mật/đồng bộ thì sao?
- Bạn test game logic ra sao?
- Bạn cân bằng độ khó bằng cách nào?

**Tiêu chí hoàn thành:** đóng game mở lại vẫn còn điểm cao, âm thanh/hiệu ứng đầy đủ, có ít nhất vài test chạy xanh.

---

### PHASE 6: Tối ưu, build và portfolio (Tuần 6)

**Mục tiêu:** biến dự án thành thứ có thể đưa cho nhà tuyển dụng.

**Việc cần làm**

- [ ] **Profiling**: dùng Profiler, đo CPU/Memory/GC Alloc; tìm và sửa **ít nhất 1 vấn đề thật**, chụp ảnh **trước/sau**.
- [ ] Rà soát: bỏ `Find`/`GetComponent` trong `Update`, tránh cấp phát rác mỗi frame (LINQ, string concat, `new` trong Update).
- [ ] Sửa lỗi, xóa code chết, đặt tên lại, thêm comment ở chỗ khó.
- [ ] **Build** bản Windows (và/hoặc WebGL); thử chạy trên máy khác.
- [ ] Đăng lên **itch.io** để người khác chơi thử.
- [ ] Viết **README** (mẫu ở Phần E).
- [ ] Quay **video demo 1 phút** (gameplay + lướt qua kiến trúc).
- [ ] Dọn repo: `.gitignore` đúng, không đẩy thư mục `Library/`, tag phiên bản `v1.0`.
- [ ] Luyện trả lời các câu hỏi ở Phần D bằng chính dự án của bạn.

**Kiến thức cần nắm**
- Profiler, Frame Debugger (khái niệm), Memory Profiler.
- Batching, sprite atlas, draw call ở mức cơ bản.
- Quy trình build, Player Settings, Quality Settings, target frame rate.

**Tiêu chí hoàn thành:** có link chơi thử, repo sạch, README rõ, video demo, và bạn kể được "vấn đề hiệu năng tôi gặp và đã giải quyết thế nào".

---

## PHẦN D. CHUẨN BỊ PHỎNG VẤN

### D1. Bản đồ kỹ năng đã được chứng minh trong dự án

| Chủ đề phỏng vấn | Bạn chứng minh bằng | Phase |
|---|---|---|
| Vòng đời MonoBehaviour | PlayerController, các script khởi tạo | 1 |
| Vật lý, va chạm, layer | Combat, Collision Matrix | 2 |
| Object Pooling, GC | Projectile/Enemy pool, Profiler | 2, 6 |
| ScriptableObject | EnemyData/WeaponData/UpgradeData | 3 |
| Design pattern | Observer, State, Singleton, Object Pool | 3 |
| Tách UI/gameplay | Event-driven HUD | 4 |
| Coroutine, async | Spawner/timer/hiệu ứng | 4 |
| Lưu dữ liệu | SaveSystem JSON | 5 |
| Testing | EditMode tests | 5 |
| Tối ưu hiệu năng | Profiler trước/sau | 6 |
| Quy trình làm việc | Git, README, build, itch.io | 0, 6 |

### D2. Bộ câu hỏi ôn tập (tự trả lời bằng ví dụ từ dự án)

**C# / Unity cơ bản**
1. `Update`, `FixedUpdate`, `LateUpdate` khác nhau thế nào?
2. `Awake` khác `Start`; `OnEnable` khi nào chạy?
3. Coroutine là gì, khác `async/await` chỗ nào?
4. `struct` vs `class` trong Unity (ví dụ `Vector3`)?
5. `GetComponent` tốn kém thế nào và cách tránh?
6. Vì sao so sánh `null` với `UnityEngine.Object` khác `null` thông thường?

**Hiệu năng**
7. GC là gì, gây giật thế nào, cách giảm cấp phát?
8. Object Pooling giải quyết gì?
9. Draw call, batching là gì?
10. Bạn đã tìm và sửa vấn đề hiệu năng nào?

**Kiến trúc**
11. ScriptableObject dùng khi nào? Nhược điểm?
12. Bạn giảm coupling bằng cách nào?
13. Singleton có nên dùng không?
14. Bạn thiết kế hệ thống nâng cấp sao cho dễ mở rộng thế nào?

**Quy trình**
15. Bạn dùng Git ra sao trong Unity? (merge conflict với scene/prefab, `.meta`, Force Text)
16. Bạn debug bằng cách nào? (Debug.Log, breakpoint, Profiler, Gizmos)

### D3. Cách kể về dự án (khung 2 phút)

1. **Là gì:** "Game 2D survival, người chơi né quái và chọn nâng cấp khi lên cấp."
2. **Vì sao làm:** "Để thực hành pooling, data-driven design và tối ưu."
3. **Điểm tự hào kỹ thuật:** chọn 2 điều (ví dụ hệ thống pool + hệ thống nâng cấp dựa trên ScriptableObject).
4. **Vấn đề khó nhất và cách giải quyết:** dẫn số liệu Profiler trước/sau.
5. **Nếu có thêm thời gian:** boss, thêm vũ khí, Addressables, gamepad.

---

## PHẦN E. TÀI LIỆU MẪU

### E1. Mẫu README

```markdown
# Arena Survival

Game 2D top-down survival làm bằng Unity 6. [Chơi thử trên itch.io](link) | [Video demo](link)

## Tính năng
- Vũ khí tự động bắn, wave quái tăng độ khó
- Lên cấp và chọn nâng cấp (3 lựa chọn ngẫu nhiên)
- Lưu điểm cao

## Điểm kỹ thuật nổi bật
- **Object Pooling** cho đạn/quái: giảm GC alloc từ X KB/frame xuống Y KB/frame
- **ScriptableObject** cho dữ liệu quái/vũ khí/nâng cấp: thêm nội dung không cần sửa code
- **Event-driven**: UI và gameplay giao tiếp qua GameEvents, không tham chiếu chéo
- **State machine** cho vòng đời game, **EditMode tests** cho logic thuần

## Kiến trúc
(Sơ đồ + mô tả ngắn các module)

## Cách chạy
Unity 6000.x LTS, mở scene `MainMenu`.

## Bài học rút ra / Nếu làm lại
...

## Credits
Asset và giấy phép sử dụng.
```

### E2. Quy ước commit

```
feat: thêm hệ thống chọn nâng cấp khi lên cấp
fix: đạn không trả về pool khi ra khỏi màn hình
refactor: tách Health khỏi Enemy để dùng chung với Player
perf: pool DamageNumber để giảm GC alloc
```

Mỗi giai đoạn nên có một **tag** (`phase-1`, `phase-2`...) để dễ quay lại.

---

## PHẦN F. QUẢN LÝ RỦI RO

| Rủi ro | Dấu hiệu | Cách xử lý |
|---|---|---|
| Scope creep | Đang thêm tính năng khi "Must" chưa xong | Quay lại bảng MoSCoW, dừng thêm mới |
| Code "God class" | Một script >300 dòng làm nhiều việc | Tách theo trách nhiệm, chuyển dữ liệu sang SO |
| Sa lầy vào art | Mất cả tuần chỉnh sprite | Dùng asset miễn phí, ưu tiên gameplay |
| Bỏ dở giữa chừng | Không mở dự án 1 tuần | Đặt mục tiêu nhỏ/ngày, commit mỗi buổi |
| Copy tutorial không hiểu | Không giải thích được code | Sau mỗi phần, tự viết lại không nhìn và ghi chú "vì sao" |
| Tối ưu quá sớm | Tối ưu khi chưa đo | Chỉ tối ưu khi Profiler chỉ ra điểm nghẽn |

**Mẹo giữ nhịp:** mỗi buổi học kết thúc bằng một thứ chạy được và một commit.

---

## PHẦN G. TÀI NGUYÊN HỌC

- **Unity Learn**: pathway *Junior Programmer*, *Creative Core* (miễn phí, tài liệu chính thức).
- **Unity Documentation & Manual**: tra cứu API, đọc phần Input System, Physics 2D, Profiler.
- **Sách/ebook Unity miễn phí của Unity** về design pattern và kiến trúc (Game Programming Patterns bản của Unity, ScriptableObject architecture).
- **Game Programming Patterns** (Robert Nystrom, đọc miễn phí online): Observer, State, Object Pool, Command.
- **YouTube:** Code Monkey, Brackeys, Jason Weimann (kiến trúc, pattern).
- **Asset miễn phí:** Kenney.nl, itch.io (mục asset), OpenGameArt, Freesound.

---

## PHẦN H. CHECKLIST TỔNG KẾT

**Sản phẩm**
- [ ] Chơi trọn vẹn Menu → Game → Game Over → Retry, không crash
- [ ] Có lưu điểm cao, có âm thanh và hiệu ứng
- [ ] Có ít nhất 3 loại quái, 2 loại vũ khí, 6+ nâng cấp

**Kỹ thuật**
- [ ] Object pooling cho đối tượng sinh hàng loạt
- [ ] ScriptableObject cho dữ liệu
- [ ] Event system, không tham chiếu chéo trực tiếp
- [ ] State machine cho GameManager
- [ ] Không `Find`/`GetComponent` trong `Update`
- [ ] Có Unit test cho logic thuần
- [ ] Có số liệu Profiler trước/sau tối ưu

**Portfolio**
- [ ] Repo GitHub sạch, commit rõ ràng, có tag
- [ ] README đầy đủ (mẫu ở E1)
- [ ] Bản build chơi thử (itch.io)
- [ ] Video demo 1 phút
- [ ] Đã luyện trả lời D2 và D3 bằng lời của mình

---

*Gợi ý cách dùng file này: mỗi tuần, chép các checkbox của Phase tương ứng vào công cụ quản lý việc (Trello/Notion/GitHub Projects), tick dần, và ghi chú "điều mới học được" cuối mỗi buổi để dùng khi ôn phỏng vấn.*
