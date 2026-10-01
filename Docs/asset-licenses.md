# Giấy phép asset

Game dự định bán thương mại, nên **mọi asset phải cho phép dùng thương mại** (plan, Phần E). File này liệt kê từng bộ asset, giấy phép và bằng chứng. Chỉ ghi một giấy phép là "đã xác minh" khi có file giấy phép trong repo hoặc link nguồn chính thức.

Cập nhật: 2026-10-01

| Asset | Vị trí | Giấy phép | Bằng chứng | Trạng thái |
|---|---|---|---|---|
| Kenney – Topdown Shooter Pack (cả tile sân sa mạc/thành phố `Art/Tiles/Tile_*.asset` dựng từ sprite của gói) | `Assets/_Project/Art/Kenney-TopDownShooter/` | CC0 1.0 | `License.txt` trong thư mục | ✅ Đã xác minh |
| Âm thanh Kenney (giao diện) | `Assets/_Project/Audio/UI/` | Cần kiểm tra (các gói Kenney thường là CC0) | Chưa có | ⚠ Tải `License.txt` từ trang gói trên kenney.nl, đặt vào thư mục |
| Âm thanh Kenney (va chạm) | `Assets/_Project/Audio/Impact/` | Cần kiểm tra (các gói Kenney thường là CC0) | Chưa có | ⚠ Như trên |
| Âm thanh Kenney (digital) | `Assets/_Project/Audio/Digital/` | Cần kiểm tra (các gói Kenney thường là CC0) | Chưa có | ⚠ Như trên |
| Nhạc nền "Level 3" – Juhani Junkala | `Assets/_Project/Audio/Music/JuhaniJunkala_Level3.ogg` | Cần kiểm tra | Chưa có | ❗ Tìm trang tải gốc, xác minh giấy phép cho phép thương mại, ghi link vào đây |
| Font Bungee | `Assets/_Project/Art/Fonts/` | SIL Open Font License 1.1 | `Bungee-OFL.txt` | ✅ Đã xác minh |
| Font Chakra Petch | `Assets/_Project/Art/Fonts/` | SIL Open Font License 1.1 | `ChakraPetch-OFL.txt` | ✅ Đã xác minh |
| LiberationSans (font mặc định của TextMesh Pro) | `Assets/TextMesh Pro/` | Cần kiểm tra | Đi kèm package TextMesh Pro | ⚠ Ghi chú lại; game hiện dùng Bungee/Chakra Petch, LiberationSans chỉ là font dự phòng |
| Sprite UI và icon nâng cấp | `Assets/_Project/Art/UI/` | Tự làm trong dự án | — | ⚠ Người dùng xác nhận |
| Icon ngôi sao, đồng vàng, khiên (`icon_star.png`, `icon_coin.png`, `icon_shield.png`) | `Assets/_Project/Art/UI/` | Tự làm (vẽ bằng script trong Unity, Phase 6A và 8A) | Tạo trong phiên làm việc 2026-10-01 | ✅ Tự làm |
| Icon trang bị, nguyên liệu, vòng sáng pickup (`icon_armor_leather`, `icon_armor_iron`, `icon_ring_power`, `icon_charm_wind`, `icon_ore`, `icon_leather`, `icon_boss_core`, `pickup_glow`) | `Assets/_Project/Art/UI/` | Tự làm (vẽ bằng script trong Unity, Phase 8B) | Tạo trong phiên làm việc 2026-10-01 | ✅ Tự làm |
| Icon 5 vũ khí chính, 5 icon mảnh, sprite đạn lựu và tia laser (`icon_weapon_*`, `icon_shard_*`, `Projectiles/grenade.png`, `Projectiles/laser_bolt.png`) | `Assets/_Project/Art/UI/`, `Assets/_Project/Art/Projectiles/` | Tự làm (vẽ bằng script trong Unity, Phase 8C) | Tạo trong phiên làm việc 2026-10-01 | ✅ Tự làm |
| Icon Hub (`icon_gem`, `icon_gear`, `icon_lock`, `icon_shop`, `icon_gift`, `icon_backpack`) | `Assets/_Project/Art/UI/` | Tự làm (vẽ bằng script trong Unity, Phase 9) | Tạo trong phiên làm việc 2026-10-01 | ✅ Tự làm |
| XP orb, đạn, lưỡi cưa, VFX | `Assets/_Project/Art/Pickups/`, `Projectiles/`, `Weapons/`, `Vfx/` | Tự làm trong dự án | — | ⚠ Người dùng xác nhận |

## Quy tắc khi thêm asset mới

1. Chỉ dùng asset có giấy phép cho phép **thương mại** (CC0, CC-BY có ghi công, SIL OFL, hoặc bản quyền đã mua).
2. Lưu file giấy phép vào cùng thư mục với asset.
3. Thêm một dòng vào bảng trên trước khi commit asset.
4. Asset CC-BY bắt buộc ghi công: thêm vào màn Credits của game.
