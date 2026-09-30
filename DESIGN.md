\# Arena Survival — Design Doc



\## 1. Tổng quan

\- \*\*Thể loại:\*\* 2D top-down arena survival (kiểu Vampire Survivors thu nhỏ)

\- \*\*1 câu mô tả:\*\* Người chơi né quái trong một đấu trường, vũ khí tự động bắn kẻ địch gần nhất, sống sót càng lâu càng khó, lên cấp để chọn nâng cấp sức mạnh.

\- \*\*Đối tượng:\*\* Dự án học tập / portfolio cho phỏng vấn vị trí Unity Developer (không phát hành thương mại)

\- \*\*Nền tảng:\*\* PC (Windows) + WebGL (để đăng itch.io)



\## 2. Core loop



```

Di chuyển/né  →  Vũ khí tự bắn  →  Hạ quái  →  Nhặt XP

&#x20;     ↑                                              ↓

&#x20; Sống sót lâu hơn  ←  Chọn nâng cấp  ←  Lên cấp

```



Người chơi không tấn công thủ công — trọng tâm gameplay là \*\*né tránh và ra quyết định khi chọn nâng cấp\*\*, không phải kỹ năng bắn súng.



\## 3. Vòng đời một ván chơi



1\. Main Menu → bấm Play

2\. Spawn vào đấu trường, quái bắt đầu xuất hiện theo wave

3\. Vũ khí tự bắn quái gần nhất trong tầm

4\. Quái chết rơi XP orb → nhặt → đầy thanh XP → lên cấp

5\. Lên cấp → game pause → chọn 1 trong 3 nâng cấp ngẫu nhiên

6\. Độ khó tăng dần theo thời gian (quái đông hơn, mạnh hơn)

7\. HP về 0 → Game Over → hiển thị thời gian sống sót + điểm cao nhất

8\. Retry hoặc về Main Menu



\## 4. Phạm vi tính năng (MoSCoW)



| Mức | Nội dung |

|---|---|

| \*\*Must have\*\* | Di chuyển, bắn tự động, 2–3 loại quái, spawner theo wave, HP, XP + lên cấp + chọn nâng cấp, HUD, main menu, game over, lưu điểm cao, object pooling, ScriptableObject cho dữ liệu, event system, game state machine |

| \*\*Should have\*\* | Âm thanh, particle hiệu ứng, animation cơ bản, pause menu, 2–3 loại vũ khí, unit test cho logic thuần |

| \*\*Could have\*\* | Boss, hỗ trợ gamepad, camera shake, cài đặt âm lượng, build WebGL |

| \*\*Won't have (lần này)\*\* | Multiplayer, shop, nhiều màn chơi, save cloud, procedural generation |



> Nguyên tắc: không chuyển sang "Should" khi "Must" chưa chạy ổn định.



\## 5. Vì sao chọn thiết kế này



\- \*\*2D, 1 màn duy nhất:\*\* giảm gánh nặng level design/3D, dồn thời gian vào lập trình.

\- \*\*Bắn tự động:\*\* bỏ input phức tạp nhưng vẫn giữ được các bài toán kỹ thuật quan trọng: pooling, targeting, va chạm.

\- \*\*Dữ liệu quái/vũ khí/nâng cấp tách bằng ScriptableObject:\*\* để chứng minh kiến trúc data-driven — dễ mở rộng, dễ giải thích khi phỏng vấn.

\- \*\*Nhiều đối tượng cùng lúc (quái, đạn):\*\* tạo ra vấn đề hiệu năng thật, có cái để đo bằng Profiler và kể câu chuyện "đã tối ưu như thế nào".



\## 6. Kỹ năng kỹ thuật muốn thể hiện



\- Vòng đời MonoBehaviour, Input System

\- Vật lý 2D, Collider/Trigger, Layer Collision Matrix

\- Object Pooling, quản lý GC

\- ScriptableObject (data-driven design)

\- Design pattern: Observer (event), State Machine, Object Pool

\- Tách UI khỏi gameplay qua event

\- Coroutine

\- Save/load bằng JSON

\- Unit test (EditMode)

\- Profiling và tối ưu hiệu năng



\## 7. Định nghĩa "xong"



\- Chơi trọn vẹn Menu → Chơi → Lên cấp → Game Over → Retry, không lỗi

\- Có repo GitHub sạch, README, bản build chơi thử được (itch.io)

\- Có ít nhất 1 ví dụ tối ưu hiệu năng đo được bằng Profiler (trước/sau)

