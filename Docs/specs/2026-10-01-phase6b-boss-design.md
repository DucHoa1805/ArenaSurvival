# Phase 6B — Boss, mini-boss và hành vi quái

Ngày: 2026-10-01
Trạng thái: đã chốt hướng (người dùng chọn phương án A và yêu cầu làm luôn)
Liên quan: `Docs/project-plan.md` (Phase 6), `Docs/specs/2026-10-01-phase6a-stages-design.md`

## 1. Mục tiêu

- Tách hành vi quái khỏi `Enemy` thành **dữ liệu cắm vào được** (ScriptableObject), để thêm quái/boss mới chủ yếu bằng asset.
- **Boss chương** 3 pha cho màn 1-3, **mini-boss** cho màn 1-2, **quái bắn xa "Xạ thủ"** cho các đợt thường.
- **Đạn của quái** né được; **thanh máu boss**; hạ boss là thắng.

## 2. Quyết định đã chốt

| Câu hỏi | Quyết định |
|---|---|
| Phạm vi | Boss chương (1-3) + mini-boss (1-2) + 1 quái bắn xa |
| Thắng màn có boss | Boss chết → mọi quái còn lại nổ theo (hiệu ứng, không rơi XP) → thắng sau 1 s. Màn không boss giữ luật "dọn sạch sân" của 6A |
| Kiến trúc | Phương án A: `EnemyBehaviour` (SO) cho hành vi; boss = `BossBehaviour` gồm các pha, mỗi pha gồm các `BossAttack` (SO) |
| Đạn của quái | Layer mới `EnemyProjectile` chỉ va chạm `Player`; dùng lại class `Projectile`; một pool chung trong scene |
| Hình boss | Sprite Kenney phóng to + tô màu (tạm, thay ở Phase 10/13) |

## 3. Ngoài phạm vi

- Boss cho chương 2–3, thêm loại quái (Phase 10).
- Hình/hoạt ảnh boss riêng (Phase 10/13).
- Đòn có vùng cảnh báo vẽ trên mặt đất (dash dùng cảnh báo bằng rung + dừng lại).

## 4. Hệ hành vi quái (`Scripts/Enemies/`)

### 4.1. `EnemyContext` (class, mỗi `Enemy` tạo **một lần** trong `Awake`)
Trạng thái riêng của từng con, dùng chung cho mọi hành vi (asset hành vi là dùng chung giữa các con nên không được giữ trạng thái):
- Tham chiếu: `Enemy Enemy`, `Rigidbody2D Body`, `Transform Player`, `EnemyData Data`, `EnemyModifiers Modifiers`, `EnemyProjectiles Projectiles`, `WaveSpawner`-giao-diện để triệu hồi (`IEnemySummoner Summoner`).
- Ô nhớ chung: `float Timer`, `float Timer2`, `int State`, `int Index`, `int Counter`, `Vector2 Vector`, `int Phase`.
- Tiện ích: `DirectionToPlayer`, `DistanceToPlayer`, `MoveTowards(Vector2 dir, float speed)`, `FaceTowards(Vector2 dir)`, `ResetState()`.

### 4.2. `EnemyBehaviour` (abstract ScriptableObject)
- `virtual void OnSpawn(EnemyContext ctx)` (mặc định `ctx.ResetState()`), `abstract void Tick(EnemyContext ctx, float dt)` (gọi trong `FixedUpdate`).
- `ChaseBehaviour`: hành vi hiện tại (đuổi thẳng, xoay mặt).
- `KeepDistanceShooterBehaviour`: giữ khoảng cách `preferredRange ± tolerance` (lại gần nếu xa, lùi nếu gần, đứng yên nếu vừa), bắn 1 viên về phía Player mỗi `fireInterval` khi trong `fireRange`. Quyết định di chuyển là hàm thuần `EnemyRules.RangeIntent(distance, preferred, tolerance)` → -1 lùi / 0 đứng / +1 tiến.

### 4.3. `EnemyData` thêm
`EnemyBehaviour behaviour` (bắt buộc; asset hiện có gán `Behaviour_Chase`), `bool isBoss`, `float projectileDamage` (sát thương đạn, nhân `Modifiers.damage`).

### 4.4. `Enemy` thay đổi
- `FixedUpdate` → `_data.behaviour.Tick(_ctx, Time.fixedDeltaTime)`. Phần đuổi theo hiện tại chuyển vào `ChaseBehaviour`.
- `Initialize(data, mods)` cập nhật `_ctx`, gọi `behaviour.OnSpawn(ctx)`.
- Thêm `public void Despawn(bool reward)` cho "nổ theo boss": phát `GameEvents.EnemyKilled(reward ? xp : 0, pos)` rồi trả về pool (XP 0 → không rơi orb).
- Spawner gán `Projectiles` và `Summoner` cho context mỗi lần sinh (gán tham chiếu, không cấp phát).

## 5. Đạn của quái

- Layer `EnemyProjectile` (layer 11) chỉ va chạm `Player` (Collision Matrix).
- Prefab `EnemyProjectile`: nhân bản `Projectile.prefab`, layer `EnemyProjectile`, sprite tròn đỏ (`vfx_circle` tô đỏ, ~0.3 đơn vị), tốc độ mặc định 5, thời gian sống 4 s.
- `Projectile` thêm `[SerializeField] bool _raiseHitEvent = true` (đạn quái tắt, để không phát hiệu ứng/tiếng "trúng quái"); chỉ gây sát thương cho đối tượng có `Health` (Player) — bất tử khi hồi sinh vẫn được tôn trọng qua `Health.Invulnerable`.
- `EnemyProjectiles` (MonoBehaviour trong scene Game): pool chung (`ObjectPool<Projectile>`, tạo sẵn 60), cache delegate trả về pool; API `Fire(Vector2 position, Vector2 direction, float speed, float damage)`.

## 6. Boss

### 6.1. Dữ liệu
- `BossBehaviour : EnemyBehaviour` (asset): `BossPhase[] phases`, `float chaseSpeedMultiplier` (khi giữa các đòn).
- `BossPhase` (serializable): `float healthThreshold` (vào pha khi máu ≤ ngưỡng; pha đầu = 1), `BossAttack[] attacks` (xoay vòng), `float pauseBetweenAttacks`.
- `BossAttack` (abstract SO): `virtual void Begin(EnemyContext ctx)`, `abstract bool Tick(EnemyContext ctx, float dt)` (trả `true` khi xong).
  - `RadialBurstAttack`: `bulletsPerRing`, `rings`, `ringInterval`, `rotatePerRing` (độ), `bulletSpeed`. Đứng yên khi bắn. Hướng đạn: hàm thuần `EnemyRules.RadialDirection(index, count, offsetDeg)`.
  - `DashAttack`: `windUp` (dừng + rung tại chỗ, cảnh báo), `dashSpeed`, `dashDuration`, `recover`. Gây sát thương bằng va chạm như thường.
  - `SummonAttack`: `EnemyData minion`, `count`, `maxAlive` (không triệu hồi nếu đã đủ), sinh quanh boss bán kính 2 qua `IEnemySummoner`.
- Chọn pha: hàm thuần `EnemyRules.PhaseForHealth(float[] thresholds, float hpFraction, int current)` — chỉ tiến, không lùi.

### 6.2. Hành vi
Giữa các đòn: đi chậm về phía Player (`chaseSpeedMultiplier`). Hết `pauseBetweenAttacks` → bắt đầu đòn kế (vòng tròn trong pha hiện tại). Mỗi lần cập nhật kiểm tra pha theo % máu; đổi pha → bỏ đòn đang dở, phát `Enemy.OnBossPhaseChanged(int)`.

### 6.3. Nội dung mẫu

| Asset | Sprite | Máu / tốc / chạm | Pha |
|---|---|---|---|
| `Enemy_Shooter` (thường) | Man Brown (súng) | 15 / 2.2 / 4, đạn 6 | `KeepDistanceShooter` (range 5.5±1, bắn mỗi 2 s, đạn tốc 5) |
| `Boss_MiniTank` (mini-boss, 1-2) | Robot 1 (súng máy), ×1.9, tô cam | 400 / 1.4 / 15, đạn 8 | Pha 1 (100%): Dash → Radial 10 viên ×2. Pha 2 (≤50%): Dash nhanh hơn → Radial 14 ×3 |
| `Boss_Warlord` (boss chương, 1-3) | Survivor 1 (súng máy), ×2.6, tô đỏ | 1500 / 1.2 / 20, đạn 10 | Pha 1 (100%): Radial 12×3. Pha 2 (≤66%): Radial + Dash. Pha 3 (≤33%): Radial 16×4 + Dash + Summon 4 Basic (tối đa 8) |

Hệ số độ khó theo màn vẫn nhân vào máu/sát thương boss. Đợt cuối: màn 1-2 = mini-boss + 2 Tank tinh anh hộ tống; màn 1-3 = boss + 3 Fast tinh anh. Màn 1-1 giữ nguyên tinh anh. `Wave_02`, `Wave_03` thêm `Enemy_Shooter`.

## 7. Luồng màn chơi (thay đổi `StageSession`, `StageData`)

- `StageData` thêm `EnemyData boss` (tùy chọn) và `float bossDelay` (mặc định 1.5 s sau khi rải xong tinh anh).
- Đợt cuối: rải tinh anh như 6A → nếu có boss: sinh boss, giữ tham chiếu, phát `OnBossSpawned(Enemy)`; nghe `Health.OnDied` của boss.
- Boss chết → `WaveSpawner.DespawnAll()` (gọi `Despawn(false)` cho mọi quái còn sống) → chờ `_victoryDelay` (chỉ đếm khi `Playing`) → thắng.
- Không có boss → luật 6A.
- `WaveSpawner` theo dõi danh sách quái đang sống (List tạo sẵn, thêm khi sinh, bớt khi trả về pool) để `DespawnAll` không phải tìm kiếm.

## 8. Giao diện

- **Thanh máu boss** (`BossHealthBar`, mới): đáy màn hình, rộng ~900 px; tên boss (`EnemyData.displayName`), thanh đỏ, vạch ngưỡng pha; hiện khi `OnBossSpawned`, ẩn khi boss chết.
- **HUD**: màn có boss → đợt cuối ghi `"{tên màn} · BOSS"`; banner hiện "BOSS XUẤT HIỆN!" thay cho "ĐỢT TINH ANH!".
- Banner nhỏ khi boss đổi pha ("BOSS NỔI GIẬN!"), dùng lại banner HUD.

## 9. Kiểm tra

1. Unit test `EnemyRulesTests`: `RangeIntent` (xa → +1, gần → -1, trong khoảng → 0, biên), `RadialDirection` (đều nhau, độ dài 1, offset), `PhaseForHealth` (đúng pha theo máu, chỉ tiến, máu 0, ngưỡng rỗng).
2. Toàn bộ test cũ đạt; biên dịch 0 lỗi; Missing Script = 0.
3. Chơi thử bằng unity-cli: Xạ thủ giữ khoảng cách và bắn (đọc khoảng cách, đếm đạn); màn 1-2 → mini-boss có thanh máu, đổi pha ở ≤50%; màn 1-3 → boss 3 pha (đặt máu bằng eval để kiểm tra pha 2, 3; triệu hồi có giới hạn); đạn quái trúng Player trừ máu, không trúng khi bất tử; boss chết → quái còn lại nổ, thắng, không rơi orb.
4. Benchmark 300 quái (`-stage 1`): GC trung bình vẫn 0 B/frame.
