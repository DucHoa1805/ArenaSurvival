# Phase 6B — Boss — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hệ hành vi quái bằng ScriptableObject, quái bắn xa, đạn của quái, mini-boss và boss chương nhiều pha, thanh máu boss, hạ boss là thắng.

**Architecture:** `Enemy` giữ lõi; `EnemyBehaviour` (SO) + `EnemyContext` (trạng thái mỗi con, tạo 1 lần) quyết định di chuyển/tấn công; `BossBehaviour` = các pha gồm `BossAttack` (SO); hàm thuần ở `EnemyRules`; `StageSession` sinh boss ở đợt cuối.

**Tech Stack:** Unity 6000.3.13f1, C#, NUnit, unity-cli `eval_file`.

**Spec:** `Docs/specs/2026-10-01-phase6b-boss-design.md`

## Global Constraints

- Không tự commit; sửa scene/prefab qua Editor, kiểm tra `isDirty`, sau khi sửa UI gọi `Canvas.ForceUpdateCanvases()` rồi lưu lại.
- Không cấp phát mỗi frame / mỗi lần sinh quái (cache delegate, mảng tạo sẵn, không `+=` trong `OnEnable` của object trong pool).
- Comment tiếng Việt, giải thích vì sao. Đổi tên/di chuyển asset qua `AssetDatabase`.

---

### Task 1: Logic thuần (TDD)
- Create: `Scripts/Enemies/EnemyRules.cs` — `int RangeIntent(float distance, float preferred, float tolerance)`, `Vector2 RadialDirection(int index, int count, float offsetDeg)`, `int PhaseForHealth(float[] thresholds, float hpFraction, int current)`.
- Test: `Tests/EditMode/EnemyRulesTests.cs` (theo spec mục 9.1).
- [ ] Viết test → chạy thấy lỗi biên dịch → viết `EnemyRules` → toàn bộ test đạt.

### Task 2: Hệ hành vi + đạn của quái (code)
- Create: `Enemies/EnemyContext.cs`, `EnemyBehaviour.cs`, `ChaseBehaviour.cs`, `KeepDistanceShooterBehaviour.cs`, `IEnemySummoner.cs`, `EnemyProjectiles.cs`; `Enemies/Boss/BossBehaviour.cs` (+`BossPhase`), `BossAttack.cs`, `RadialBurstAttack.cs`, `DashAttack.cs`, `SummonAttack.cs`.
- Modify: `Enemy.cs` (context, behaviour, `Despawn`, `OnBossPhaseChanged`), `EnemyData.cs` (`behaviour`, `isBoss`, `projectileDamage`), `Combat/Projectile.cs` (`_raiseHitEvent`), `Stages/WaveSpawner.cs` (danh sách quái sống, `DespawnAll`, gán `Projectiles`/`Summoner`, implement `IEnemySummoner`), `Stages/StageData.cs` (`boss`, `bossDelay`), `Stages/StageSession.cs` (sinh boss, thắng khi boss chết, `OnBossSpawned`).
- [ ] Code → biên dịch 0 lỗi → test đạt.

### Task 3: Layer, prefab, dữ liệu, scene
- [ ] Layer `EnemyProjectile` (11) + Collision Matrix chỉ với `Player`.
- [ ] Prefab `Prefabs/EnemyProjectile.prefab`.
- [ ] Asset: `Data/Enemies/Behaviours/Behaviour_Chase`, `Behaviour_Shooter`; `Data/Enemies/Enemy_Shooter`; `Data/Bosses/` (attack assets, `BossBehaviour_MiniTank`, `BossBehaviour_Warlord`, `Boss_MiniTank`, `Boss_Warlord`); gán `Behaviour_Chase` cho 3 quái cũ; thêm Shooter vào `Wave_02/03`; `Stage_C1_02.boss`, `Stage_C1_03.boss` + hộ tống theo spec.
- [ ] Scene Game: object `EnemyProjectiles` (gán prefab), nối vào `WaveSpawner`.
- [ ] Quét Missing Script = 0.

### Task 4: Giao diện
- Create: `UI/BossHealthBar.cs`. Modify: `UI/HudController.cs` (chữ "BOSS", banner theo boss/đổi pha).
- [ ] Code → biên dịch → dựng UI trong scene (thanh đáy màn hình) → lưu (ForceUpdateCanvases).

### Task 5: Kiểm tra và bàn giao
- [ ] Chơi thử theo spec mục 9.3, chụp ảnh, console 0 lỗi.
- [ ] Benchmark 300 quái, GC 0 B/frame.
- [ ] Cập nhật `CLAUDE.md`, tick ô Phase 6 còn lại trong plan, `asset-licenses.md` nếu có asset mới; gợi ý commit.
