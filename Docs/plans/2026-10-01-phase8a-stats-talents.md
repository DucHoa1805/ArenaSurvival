# Phase 8A — Chỉ số, Talent — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 5 Talent tiêu vàng, StatCalculator + lực chiến, đưa chỉ số vào trận, giáp, hồi máu, bảng Talent.
**Architecture:** dữ liệu SO + hàm thuần + `TalentService` (Meta) + `PlayerLoadout` là cửa duy nhất vào trận.
**Spec:** `Docs/specs/2026-10-01-phase8a-stats-talents-design.md`

## Global Constraints
- Không tự commit; sửa scene/prefab qua Editor (`isDirty`, ForceUpdateCanvases + lưu lại sau khi sửa UI).
- Trừ tiền chỉ qua `EconomyService.TrySpend`. Save chỉ lưu id. Comment tiếng Việt giải thích vì sao.

### Task 1 (TDD): StatBlock, StatRules, StatCalculator, CombatRules, TalentRules
- [ ] Test → lỗi → code (`Scripts/Stats/*`, `Combat/CombatRules.cs`, `Meta/TalentRules.cs`) → đạt.
### Task 2 (TDD): Profile v3 + TalentService
- [ ] Test migration v2→v3 + `TalentServiceTests` → lỗi → `PlayerProfile.talents`, `SaveMigrations` v2→v3, `TalentData`, `TalentCatalog`, `TalentService`, `GameServices.Talents` → đạt.
### Task 3: Đưa vào trận
- [ ] `Health.Armor` + ApplyArmor, `PlayerStats.SetBaseMultipliers`, `HealthRegen`, `PlayerLoadout`, `HeroBaseStats`; gắn vào prefab Player; asset dữ liệu.
### Task 4: UI
- [ ] `TalentPanel`, `TalentRow`, prefab, icon khiên, nút "Nâng cấp" ở menu chính.
### Task 5: Kiểm tra, bàn giao
- [ ] Chơi thử (spec 9.3), benchmark, CLAUDE.md, tick plan, gợi ý commit.
