# TÀI LIỆU ĐẶC TẢ CHI TIẾT REQUIREMENT TỪNG PHASE – ABYSSAL DUNGEON

> [!IMPORTANT]
> **Tổng quan tiến độ dự án hiện tại:**
> - **Tổng số giai đoạn (theo Master Prompt – Phần 14):** **13 Phase** (`PHASE 0` → `PHASE 12`).
> - **Đã hoàn thành:** **13 / 13 Phase** (`PHASE 0` → `PHASE 12` — **100% Hoàn tất**).
> - **Còn lại:** **0 Phase**.

---

## BẢNG TỔNG HỢP TIẾN ĐỘ 13 PHASE

| Phase | Tên Giai Đoạn | Đối Chiếu Master Prompt & GDD | Trạng Thái |
| :--- | :--- | :--- | :--- |
| **PHASE 0** | **Project Audit** | Phần 1, 12, 13, 14, 15 + Toàn bộ GDD | `[x]` Đã hoàn thành |
| **PHASE 1** | **Visual Foundation** | Phần 2.1, 2.3, 2.4.4, 14 | `[x]` Đã hoàn thành |
| **PHASE 2** | **Player Controller** | Phần 3.1, 14 + GDD Mục 6 | `[x]` Đã hoàn thành |
| **PHASE 3** | **Combat Foundation** | Phần 3.2, 14 + GDD Mục 4.5 | `[x]` Đã hoàn thành |
| **PHASE 4** | **Class System (10 Classes)** | Phần 2.2, 2.4.1, 4, 14 + GDD Mục 3.1, 3.10 | `[x]` Đã hoàn thành |
| **PHASE 5** | **Enemy AI (10 Enemies)** | Phần 2.4.2, 5, 14 + GDD Mục 3.3, 3.4, 6 | `[x]` Đã hoàn thành |
| **PHASE 6** | **Dungeon System** | Phần 2.1, 2.4.4, 6, 14 + GDD Mục 3.5, 4.4, 6 | `[x]` Đã hoàn thành |
| **PHASE 7** | **Boss System (Abyssal Knight)** | Phần 2.4.3, 8, 14 + GDD Mục 3.7, 6 | `[x]` Đã hoàn thành |
| **PHASE 8** | **Progression, Loot & Save** | Phần 2.4.5, 9, 14 + GDD Mục 3.8, 3.9, 3.10, 6 | `[x]` Đã hoàn thành |
| **PHASE 9** | **Abyss Director AI** | Phần 7, 14 + GDD Mục 3.6, 6 | `[x]` Đã hoàn thành |
| **PHASE 10** | **Multiplayer Co-op (3–4 Players)** | Phần 10, 14 + GDD Mục 3.2, 4.2, 6 | `[x]` Đã hoàn thành |
| **PHASE 11** | **UI/UX (16 Màn hình & HUD)** | Phần 11, 14 + GDD Mục 4.1–4.7, 5, 6 | `[x]` Đã hoàn thành |
| **PHASE 12** | **Polish, Optimization & Build** | Phần 12, 14, 15, 16 + GDD Mục 4.6, 4.7, 6 | `[x]` Đã hoàn thành |

---

## PHẦN A. NGUYÊN TẮC CỐT LÕI & YÊU CẦU XUYÊN SUỐT MỌI PHASE

Toàn bộ 13 Phase bắt buộc tuân thủ các quy chuẩn chung đã gửi trong **Master Prompt** và tài liệu gốc **`Kich_Ban_Game_Abyssal_Dungeon.docx` (GDD)**:

### 1. Vai trò Đội ngũ Phát triển (AAA/AA Standards)
Kết hợp tư duy và chuyên môn của 11 vị trí:
- Game Director, Senior Unity Gameplay Programmer, 3D Technical Artist, Combat System Designer, Enemy AI Engineer, Multiplayer Network Engineer, Level Designer, UI/UX Designer, VFX & Audio Designer, Game Performance Engineer, QA Engineer.

### 2. Danh mục 14 Tính năng Cốt lõi Bất khả xâm phạm (Phần 1 & GDD)
Tuyệt đối **không được tự ý loại bỏ** bất kỳ tính năng cốt lõi nào sau đây:
1. Multiplayer Co-op 3–4 người (hỗ trợ 4 kiểu đội hình: *Tổ đội cân bằng, Tổ đội công, Tổ đội thủ, Tổ đội khống chế*).
2. Hệ thống 10 Class nhân vật (mỗi người chơi điều khiển một Aether Hunter 3D).
3. Hệ thống Enemy AI (10 loại quái vật 3D với hành vi khác biệt).
4. Hệ thống Abyss Director AI (điều chỉnh độ khó động theo hiệu suất tổ đội).
5. Dungeon nhiều phòng liên hoàn.
6. Elite Monster và Mini Boss.
7. Boss cuối nhiều phase (mẫu đầu tiên: **Abyssal Knight** – 3 Phase).
8. Combat System chuyên sâu có chiều sâu hành động.
9. EXP và Level.
10. Equipment và Loot (6 bậc phẩm chất, 5 loại trang bị, 8 chỉ số).
11. Skill và Character Build (nhiều hướng phát triển cho mỗi Class).
12. Town và Main Menu.
13. Lobby và Party System.
14. End Game và thống kê thành tích trận đấu.
- *Nguyên tắc minh bạch:* Phân biệt rõ giữa **yêu cầu chính thức trong GDD** và **đề xuất mở rộng**; không tự ý đưa ra quyết định kiến trúc lớn mà không giải thích.

### 3. Quy chuẩn Sản xuất 3D Asset & Art Direction (Phần 2 & Phần 2.4)
- **Phong cách hình ảnh:** *High Quality Dark Fantasy 3D Action RPG* — tham khảo chất lượng trình bày, ánh sáng, môi trường, cảm giác chiến đấu của *Diablo IV, Path of Exile 2, Dark Souls III, Elden Ring, Lost Ark* (tuyệt đối không sao chép tài sản/IP độc quyền).
- **Quy trình 12 bước sản xuất Asset 3D:**
  1. Phân tích yêu cầu thiết kế → 2. Xây dựng concept & silhouette → 3. Tạo 3D Model (Blender/Python API/Procedural) → 4. UV Unwrapping → 5. Texture & PBR Materials → 6. Rigging (nếu là nhân vật/quái) → 7. Animation → 8. Export (FBX/GLB/OBJ) → 9. Import vào Unity → 10. Tạo Prefab → 11. Thiết lập Material, Collider & LOD → 12. Kiểm tra chất lượng trong Unity Scene.
- **Tổ chức thư mục chuẩn:**
  ```text
  Assets/
  ├── Art/
  │   ├── Characters/
  │   ├── Enemies/
  │   ├── Bosses/
  │   ├── Weapons/
  │   ├── Armor/
  │   ├── Environments/
  │   ├── Props/
  │   ├── Materials/
  │   ├── Textures/
  │   └── VFX/
  ├── Animations/
  ├── Prefabs/
  └── Scenes/
  ```
- **Yêu cầu chất lượng Asset 3D:**
  - Không phụ thuộc hoàn toàn vào Asset mặc định của Unity hoặc dùng hình khối placeholder đơn giản (`Cube`, `Capsule`) làm Asset cuối cùng.
  - Không tạo các model giống hệt nhau rồi chỉ đổi màu; không dùng texture kéo giãn hoặc vật liệu thiếu đồng nhất; không tạo nhân vật thiếu rigging khi cần animation.
  - Tối ưu polygon count theo tầm quan trọng, thiết lập LOD, Occlusion Culling, tối ưu texture và draw calls.

### 4. Kiến trúc Phần mềm & Code Quality (Phần 12)
- **Kiến trúc ưu tiên:** Clean Architecture, SOLID, Modular Design, Component-based Architecture, `ScriptableObjects`, Event-driven Architecture, Reusable Systems.
- **Phân chia 16 phân hệ rõ ràng:** `Core`, `Characters`, `Combat`, `Classes`, `Skills`, `Enemies`, `AI`, `Dungeon`, `Bosses`, `Inventory`, `Equipment`, `Progression`, `Multiplayer`, `UI`, `Audio`, `VFX`.
- **Quy định nghiêm ngặt:**
  - Không tạo God-script khổng lồ chứa mọi chức năng; không lặp lại logic.
  - Không hardcode dữ liệu gameplay khi có thể dùng `ScriptableObjects`.
  - Không tạo nhiều Singleton tùy tiện.
  - **Không tự ý xóa code đang hoạt động**; **không phá vỡ Scene, Prefab và tài nguyên hiện tại**.
  - Luôn kiểm tra null references, compile errors (`0 errors`) và dependency giữa các hệ thống.

### 5. Yêu cầu Bắt buộc về Awesome GameDev Agent Skills (Phần 13)
- Trước mỗi nhiệm vụ, bắt buộc đọc [.agents/skills/router/SKILL.md](file:///e:/N4_1/LAP_TRINH_GAME_2/GIUA_KY/Abyssal_Dungeon_3D/.agents/skills/router/SKILL.md) và các `SKILL.md` liên quan trực tiếp, áp dụng kiến thức thực tế vào code (không chỉ liệt kê tên):
  - **Nhóm Unity:** `unity-csharp-scripting`, `unity-animation`, `unity-input-system`, `unity-navmesh`, `unity-physics`, `unity-scriptableobjects`, `unity-build-pipeline`.
  - **Nhóm Game Development:** `game-ai`, `game-feel`, `game-ui-ux`, `camera-systems`, `audio-design`, `create-game-assets`, `performance-optimization`, `procedural-gen`, `level-design`, `rpg`, `save-systems`, `shader-programming`.

### 6. Quy tắc Unity Editor & Tiêu chuẩn Hoàn thành từng Phase (Phần 15 & Phần 16)
- **Trước khi sửa:** (1) Kiểm tra hệ thống hiện tại → (2) Tìm thành phần tái sử dụng → (3) Kiểm tra dependency → (4) Lập kế hoạch chỉnh sửa.
- **Khi triển khai:** Chỉ sửa phần cần thiết, đặt script đúng thư mục, giữ nguyên tài nguyên hiện có, kiểm tra compilation & Unity Console, kiểm tra reference/dependency. Không tuyên bố hoàn thành nếu chỉ tạo script rời mà chưa tích hợp & kiểm thử.
- **Tiêu chuẩn nghiệm thu mỗi hệ thống:** Code compile thành công (`0 errors`), không lỗi runtime nghiêm trọng, tích hợp đúng hệ thống hiện có, kiểm thử được trong Unity, có hướng dẫn sử dụng, không làm hỏng chức năng khác, có tiêu chí nghiệm thu rõ ràng.
- **Báo cáo bắt buộc gồm 8 mục cuối mỗi Phase:**
  1. Những chức năng đã hoàn thành.
  2. Những file được tạo mới.
  3. Những file được chỉnh sửa.
  4. Những Skills đã sử dụng.
  5. Những lỗi đã phát hiện.
  6. Những vấn đề chưa giải quyết.
  7. Cách kiểm thử.
  8. Kế hoạch Phase tiếp theo.

---

## PHẦN B. CHI TIẾT REQUIREMENT CỦA TỪNG PHASE (`PHASE 0` → `PHASE 12`)

---

### PHASE 0 – PROJECT AUDIT `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 1, Phần 12, Phần 13, Phần 14, Lệnh Bắt Đầu* & toàn bộ *GDD*.
- **Mục tiêu:** Khảo sát toàn diện dự án hiện tại, đối chiếu GDD và thiết lập kiến trúc tổng thể cùng lộ trình phát triển trước khi can thiệp code.
- **Chi tiết Requirement:**
  - Kiểm tra toàn bộ Unity Project: Unity Version (`2021.3.11f1`), Render Pipeline (`URP 12.1.7`), danh sách Packages đã cài.
  - Đọc toàn bộ GDD `Kich_Ban_Game_Abyssal_Dungeon.docx` (vòng lặp gameplay *Vào phó bản → Đánh quái → Nhận EXP → Nhặt trang bị → Xây dựng nhân vật → Đánh boss → Nhận phần thưởng → Nâng cấp → Tiếp tục phó bản khó hơn*).
  - Kiểm tra toàn bộ Scene, Prefab, Scripts, Assets và đánh giá chất lượng code/kiến trúc hiện tại (những gì đã có, những gì còn thiếu).
  - Đọc Router và các GameDev Skills đã cài đặt.
  - Đề xuất Art Direction, Technical Architecture và kế hoạch phát triển chi tiết theo từng Phase.
  - **Ràng buộc:** Không xóa hoặc viết lại toàn bộ dự án; không tự ý thay đổi hệ thống đang chạy khi chưa duyệt báo cáo Phase 0.

---

### PHASE 1 – VISUAL FOUNDATION `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 2.1, Phần 2.3, Phần 2.4.4, Phần 14*.
- **Mục tiêu:** Xây dựng nền tảng đồ họa Dark Fantasy 3D trên URP, bộ tài nguyên môi trường modular 3D và khu vực mẫu chất lượng hình ảnh cao.
- **Chi tiết Requirement:**
  - **7 Chủ đề Môi trường (Environment Biomes):**
    1. *Ancient Dungeon*
    2. *Forgotten Temple*
    3. *Underground Fortress*
    4. *Abyssal Cave*
    5. *Cursed Forest*
    6. *Crystal Chamber*
    7. *Boss Arena*
  - **Chi tiết kiến trúc & bầu không khí môi trường:**
    - Kiến trúc 3D có chiều sâu không gian (trần vòm, trụ cột, bậc thang, tầng cao thấp).
    - Vật liệu đá, kim loại, gỗ có độ chi tiết PBR cao (Albedo, Normal, Metallic/Smoothness, AO).
    - Ánh sáng tương phản mạnh (chiaroscuro), sương mù thể tích / sương mù chiều sâu.
    - Nguồn sáng điểm từ đuốc lửa, tinh thể phát quang và ma thuật.
    - Hệ thống hạt (Particle) môi trường, Decal vết nứt, rêu phong, dấu vết chiến đấu.
    - Âm thanh môi trường riêng theo từng khu vực.
  - **Lighting & Rendering (URP):**
    - Kiểm tra Render Pipeline hiện có trước, không tự ý đổi pipeline làm hỏng asset phụ thuộc.
    - Thiết lập PBR Materials, High Quality Lighting, Shadow Mapping, Ambient Occlusion (SSAO), Bloom có kiểm soát (không gây chói giảm tầm nhìn gameplay), Volumetric/Height Fog, Reflection, Post Processing và Shader Graph / Custom URP Shader.
  - **Tự tạo Bộ Modular Environment 3D (Phần 2.4.4):**
    - *Dungeon Walls, Floor Tiles, Pillars, Stairs, Doors, Gates, Ancient Ruins, Torches, Crystals, Statues, Treasure Chests, Traps, Boss Arena, Environmental Props* (thiết kế modular tái sử dụng kèm Collider & LOD).
  - Tạo một khu vực mẫu (*Visual Showcase*) đạt chất lượng hình ảnh cao để kiểm chứng chuẩn nhìn chung.

---

### PHASE 2 – PLAYER CONTROLLER `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 3.1, Phần 14* & GDD – *Mục 6 (Điều khiển nhân vật)*.
- **Mục tiêu:** Xây dựng bộ điều khiển nhân vật góc nhìn thứ ba (Third Person) mượt mà, phản hồi nhạy bén và đồng bộ hoàn toàn với hệ thống Animation.
- **Chi tiết Requirement:**
  - **Hệ thống Camera góc nhìn thứ 3 (Third Person Camera):**
    - Theo dõi nhân vật mượt mà, xoay tự do bằng chuột.
    - Có **Camera Collision** và cơ chế **tránh xuyên tường** (SphereCast/anti-clipping).
    - Cho phép người chơi điều chỉnh khoảng cách camera (zoom) và độ nhạy chuột.
    - Hỗ trợ cơ chế **Lock-on Target** vào quái vật / Boss.
  - **Điều khiển chuyển động & Hành động (Character Movement & Actions):**
    - Điều khiển di chuyển `WASD` theo hướng camera phản hồi tức thì.
    - Triển khai đầy đủ 10 trạng thái hành động:
      1. *Idle*
      2. *Walk*
      3. *Run*
      4. *Sprint*
      5. *Jump* (khi thiết kế gameplay cần)
      6. *Dodge* (lướt né đòn có i-frames)
      7. *Attack* (đánh thường cơ bản)
      8. *Skill* (thi triển kỹ năng)
      9. *Take Damage* (phản ứng trúng đòn)
      10. *Death* (ngã gục)
  - **Chất lượng chuyển động:** Animation đồng bộ chặt chẽ với tốc độ di chuyển thực tế, **tuyệt đối không để nhân vật trượt chân trên mặt đất (foot sliding)** hoặc chuyển tiếp animation bị giật/thiếu tự nhiên.

---

### PHASE 3 – COMBAT FOUNDATION `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 3.2, Phần 14* & GDD – *Mục 4.5*.
- **Mục tiêu:** Xây dựng hệ thống chiến đấu hành động có sức nặng, chiều sâu cơ chế và phản hồi nghe-nhìn mạnh mẽ (*Game Feel / Juice*).
- **Chi tiết Requirement:**
  - **Cơ chế chiến đấu cốt lõi (Combat Mechanics):**
    - *Basic Attack* & *Combo Chain* (chuỗi đòn liên hoàn nhiều nhịp).
    - *Heavy Attack* (đòn đánh mạnh phá thế/gây choáng).
    - *Skill Attack* (kỹ năng chủ động theo thời gian hồi chiêu *Cooldown* và tiêu hao *Mana/Energy*).
    - *Dodge* né đòn kết hợp i-frames & input buffering.
    - Công thức tính sát thương đầy đủ (*Damage Calculation*, *Critical Hit*, phòng thủ, xuyên giáp/hút máu).
    - Hệ thống va chạm **Hitbox và Hurtbox** không gian 3D thực sự (**tuyệt đối không làm combat chỉ trừ HP đơn thuần khi bấm phím**).
    - Hiệu ứng vật lý & khống chế: *Knockback* (đẩy lùi), *Stagger* (khựng đòn), *Status Effects* (Cháy, Độc, Làm chậm, Đóng băng, Choáng, Nguyền rủa, Khiêu khích, Khiên chắn...).
    - Cơ chế *Death và Respawn* / Hồi sinh đồng đội trong tổ đội.
  - **Hiệu ứng phản hồi chiến đấu (Combat Feedback & VFX/SFX):**
    - *Weapon Trail* (vệt chém vũ khí 3D).
    - *Hit Impact* (tia lửa/hạt va chạm tại đúng điểm chém trúng).
    - *Damage Numbers* (số sát thương nổi phân biệt đòn thường / chí mạng / hồi máu / sát thương theo thời gian).
    - *Camera Shake* (rung chấn camera theo lực đòn đánh).
    - *Hit Stop* (ngưng đọng khung hình cực ngắn khi đòn nặng/chí mạng trúng đích).
    - *Screen Feedback* (viền màn hình cảnh báo trúng đòn, máu thấp, chí mạng).
    - *Skill Particles* và *Sound Effects* riêng biệt; mọi kỹ năng đều phải có animation, thời gian thi triển (cast time), phạm vi, sát thương và hiệu ứng rõ ràng.

---

### PHASE 4 – CLASS SYSTEM (10 CLASSES) `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 2.2, Phần 2.4.1, Phần 4, Phần 14* & GDD – *Mục 3.1, Mục 3.10, Mục 4.3, Mục 6*.
- **Mục tiêu:** Triển khai kiến trúc modular và tài nguyên 3D riêng biệt cho đầy đủ **10 Class nhân vật** trong GDD.
- **Chi tiết Requirement:**
  - **Danh sách đầy đủ 10 Class bắt buộc:**
    1. **Knight** – *Tank / Fighter* (Phòng thủ, chống chịu, bảo vệ đồng đội).
    2. **Guardian** – *Defensive Support* (Tạo khiên, giảm sát thương, bảo vệ đội hình).
    3. **Berserker** – *Risk-based Melee DPS* (Càng mất máu càng mạnh, lối chơi mạo hiểm).
    4. **Assassin** – *Burst Damage / Mobility* (Áp sát, sát thương chí mạng bùng nổ, kết liễu).
    5. **Ranger** – *Ranged DPS / Traps* (Tấn công tầm xa, đặt bẫy, gây hiệu ứng bất lợi).
    6. **Mage** – *AoE / Crowd Control* (Phép thuật diện rộng mạnh mẽ và khống chế đám đông).
    7. **Warlock** – *Damage over Time / Summoning* (Sát thương theo thời gian DOT, nguyền rủa, triệu hồi).
    8. **Monk** – *Fast Melee / Support* (Đánh liên hoàn tốc độ cao, cơ động, hỗ trợ đồng đội).
    9. **Druid** – *Transformation / Healing* (Chuyển đổi hình dạng chiến đấu/hồi phục linh hoạt).
    10. **Cleric** – *Healing / Support* (Hồi máu, tạo khiên, giải hiệu ứng xấu, hồi sinh đồng đội).
  - **Chiều sâu Gameplay mỗi Class:**
    - Có đầy đủ: *Base Stats*, *Unique Skills (Q/W/E/R)*, *Passive Abilities*, *Basic Attack*, *Combat Identity*, *Skill Progression*, *Equipment Compatibility* và **Build Variations** (tối thiểu 3 hướng Build/Class; ví dụ: Mage có *Fire / Frost / Arcane*; Knight có *Tank / Paladin / Dark Knight*; Druid có *Healing Form / Combat Form / Transformation Mechanics*).
    - **Cấm:** Không nhân bản một bộ kỹ năng chung rồi chỉ đổi tên hoặc thay đổi con số sát thương.
  - **Tài nguyên 3D & Nhận diện Hình ảnh mỗi Class (Phần 2.2 & 2.4.1):**
    - Mỗi Class phải có: *Silhouette riêng biệt*, *3D Character Model*, *Armor/Trang phục riêng*, *Weapon Model đặc trưng*, *Character Texture & PBR Materials*, *Rigging/Skeleton*, *Animation Controller & Clips*, và *VFX kỹ năng riêng*.

---

### PHASE 5 – ENEMY AI (10 LOẠI QUÁI VẬT & TACTICAL AI) `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 2.4.2, Phần 5, Phần 14* & GDD – *Mục 3.3, Mục 3.4, Mục 6*.
- **Mục tiêu:** Xây dựng hệ thống điều khiển quái vật 3D, điều hướng không gian (`NavMesh`/Steering), kiến trúc State Machine / Behavior Tree phân cấp và hành vi phối hợp chiến thuật cho 10 chủng loại quái vật cùng biến thể Tinh Anh (Elite).
- **Chi tiết Requirement:**
  1. **Kiến trúc AI & Chu trình Trạng thái (State Machine / Behavior Tree):**
     - Triển khai đầy đủ chu trình ra quyết định linh hoạt:
       `Idle` → `Patrol` (Tuần tra) → `Detect` (Phát hiện người chơi) → `Select Target` (Chọn mục tiêu ưu tiên) → `Chase` (Truy đuổi có Navigation tránh vật cản) → `Attack` (Tấn công / Dùng kỹ năng) → `Evaluate` (Đánh giá tình huống chiến trường) → `Retreat / Change Target` (Rút lui chiến thuật hoặc Đổi mục tiêu).
  2. **Đầy đủ 10 Chủng loại Quái vật (Enemy Roster):**
     1. **Goblin** (Cận chiến nhanh, đi theo bầy).
     2. **Goblin Archer** (Bắn tên tầm xa, giữ cự ly).
     3. **Skeleton** (Bộ xương cận chiến cơ bản).
     4. **Skeleton Warrior** (Chiến binh xương mang khiên/kiếm bền bỉ).
     5. **Skeleton Mage** (Pháp sư xương bắn chưởng phép/debuff tầm xa).
     6. **Zombie** (Chậm nhưng lì đòn, áp sát gây độc/làm chậm).
     7. **Dark Knight** (Kỵ sĩ hắc ám giáp nặng, chém quét mạnh, đỡ đòn).
     8. **Demon** (Ác quỷ tốc độ & sát thương bùng nổ, kỹ năng lửa/hắc ám).
     9. **Crystal Golem** (Người đá tinh thể khổng lồ đóng vai trò Tank/Mini-Boss chắn đòn).
     10. **Forest Monster** (Quái thú rừng nguyền rủa có chiêu trói chân/hồi phục/độc).
  3. **Hành vi Chiến thuật Đặc biệt & Phối hợp Tổ đội Quái (Role Behaviors):**
     - **Nhóm Archer / Ranged (*Goblin Archer, Skeleton Mage*):** Chủ động giữ khoảng cách an toàn, tấn công tầm xa, tự động lùi/lách ngang (*kiting*) tránh người chơi áp sát.
     - **Nhóm Assassin / Flanker (*Goblin, Demon*):** Đánh giá máu/vị trí tổ đội người chơi để tiếp cận mục tiêu yếu hoặc bị tách lẻ khỏi đội, tìm đường vòng ra **tấn công từ phía sau** (*flanking/backstab*), và rút lui khi máu thấp hoặc gặp nguy hiểm.
     - **Nhóm Tank / Protector (*Skeleton Warrior, Dark Knight, Crystal Golem*):** Chủ động di chuyển chắn trước mặt **bảo vệ Elite Monster / Pháp sư / Xạ thủ phe quái**, dùng kỹ năng thu hút sự chú ý/khiêu khích người chơi.
     - Các AI không đứng đánh rời rạc mà phải có **sự phối hợp nhóm (group coordination)** trong cùng một encounter (không dồn cục đè lên nhau, phân bổ góc vây quanh mục tiêu).
  4. **Biến thể Quái Tinh Anh (Elite Monsters):**
     - Có kích thước lớn hơn, thiết kế ngoại hình/phụ kiện đặc biệt, hào quang (affix aura) nhận diện rõ ràng, chỉ số vượt trội và kỹ năng cường hóa riêng.
  5. **Tự tạo Tài nguyên 3D Enemy (Phần 2.4.2):**
     - Mỗi loại trong 10 Enemy phải có: *Model 3D riêng*, *Texture riêng*, *PBR Material riêng*, *Skeleton phù hợp*, và đủ bộ 5 nhóm hoạt ảnh: *Idle Animation*, *Walk/Run Animation*, *Attack Animation*, *Hit Reaction*, *Death Animation* (lưu trữ đúng chuẩn trong `Assets/Art/Enemies/`, `Assets/Prefabs/`).
  - **Skills bắt buộc đọc & áp dụng:** `router`, `game-ai`, `ai-behavior-trees-utility-ai`, `unity-navmesh`, `unity-animation`, `unity-physics`, `create-game-assets`, `unity-scriptableobjects`.
  - **Tiêu chí nghiệm thu Phase 5:** 10 loại quái + Elite xuất hiện với ngoại hình 3D phân biệt rõ, di chuyển mượt không kẹt tường, thể hiện rõ 3 hành vi đặc trưng (*Archer giữ cự ly*, *Assassin móc lốp mục tiêu yếu & rút lui*, *Tank bảo vệ Elite*), có đủ hoạt ảnh tấn công/trúng đòn/chết và compile `0 errors`.

---

### PHASE 6 – DUNGEON SYSTEM `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 2.1, Phần 2.4.4, Phần 6, Phần 14* & GDD – *Mục 3.5, Mục 4.4, Mục 6*.
- **Mục tiêu:** Xây dựng hệ thống quản lý phó bản nhiều phòng liên hoàn có thiết kế thủ công (handcrafted) chất lượng cao kết hợp khả năng mở rộng sinh ngẫu nhiên (Procedural Generation) để tăng giá trị chơi lại.
- **Chi tiết Requirement:**
  1. **Tiến trình Chuỗi Phòng chuẩn GDD (Multi-Room Flow):**
     `Entrance` (Cổng vào) → `Combat Room` (Phòng chiến đấu) → `Elite Room` (Phòng Tinh Anh) → `Event / Puzzle Room` (Phòng sự kiện / Giải đố) → `Mini Boss Room` (Phòng Tiểu Chủ) → `Combat Room` (Phòng chiến đấu tiền sảnh Boss) → `Final Boss Arena` (Đấu trường Boss cuối).
  2. **Đầy đủ 8 Thành phần Gameplay trong Dungeon:**
     - *Normal Monsters* (Đợt quái thường đa chủng loại).
     - *Elite Monsters* (Quái tinh anh có kỹ năng nguy hiểm).
     - *Treasure Chests* (Rương kho báu có hiệu ứng mở rương & rơi đồ).
     - *Traps* (Bẫy cơ học/ma thuật: bẫy gai, bẫy lửa, lưỡi cưa/trụ cổ ngữ có chu kỳ cảnh báo).
     - *Environmental Events* (Sự kiện môi trường: phục kích, bảo vệ cổ vật, tế đàn nguyền rủa/hồi phục...).
     - *Puzzle Rooms* (Phòng giải đố cơ chế tương tác thực tế như kích hoạt trụ cổ ngữ đúng thứ tự / dẫn luồng năng lượng / đĩa áp lực).
     - *Mini Bosses* (Trận đấu Mini-Boss giữa chặng canh giữ cổng sâu hơn, ví dụ *Ancient Crystal Golem* / *Cursed Warden*).
     - *Final Boss* (Cổng đại sảnh dẫn vào đấu trường Boss cuối).
  3. **7 Hệ thống Quản lý Kỹ thuật Dungeon:**
     - *Room Management*: Quản lý trạng thái từng phòng (*Unvisited, Active, Cleared*), chuyển đổi ánh sáng/sương mù/âm thanh môi trường theo từng khu vực (7 chủ đề môi trường ở Phần 2.1).
     - *Encounter Management* & *Enemy Spawning*: Quản lý từng đợt quái (waves), cổng triệu hồi/điểm spawn hợp lý, không spawn đè vào người chơi.
     - *Door Lock / Unlock*: Tự động đóng cổng sắt/rào chắn ma thuật khi bắt đầu encounter và mở khóa kèm hiệu ứng âm thanh/hình ảnh khi hoàn thành mục tiêu phòng.
     - *Room Transition*: Chuyển tiếp giữa các phòng mượt mà, đồng bộ cả tổ đội.
     - *Dungeon Progression* & *Dungeon Rewards*: Theo dõi tiến độ toàn phó bản (% hoàn thành, mục tiêu hiện tại, số quái còn lại) và trao thưởng rương/EXP khi vượt ải.
  4. **Thiết kế Thủ công ưu tiên + Tùy chọn Procedural Generation:**
     - **Bắt buộc ưu tiên:** Xây dựng hoàn chỉnh một phó bản thiết kế thủ công (handcrafted) có bố cục kiến trúc, ánh sáng, vật cản chiến thuật và thử thách khác biệt rõ rệt giữa từng phòng.
     - **Mở rộng:** Kiến trúc hỗ trợ *Procedural Dungeon Generation* (ghép phòng modular theo seed + luật pacing) cho các tầng phó bản tiếp theo.
  - **Skills bắt buộc đọc & áp dụng:** `router`, `level-design`, `procedural-gen`, `unity-physics`, `unity-scriptableobjects`, `create-game-assets`, `audio-design`.
  - **Tiêu chí nghiệm thu Phase 6:** Đi xuyên suốt từ *Entrance* đến *Final Boss* qua đủ 7 loại phòng; cửa khóa/mở chuẩn xác theo trạng thái encounter; bẫy, rương kho báu, sự kiện môi trường, phòng giải đố và Mini-Boss hoạt động trơn tru không soft-lock.

---

### PHASE 7 – BOSS SYSTEM (ABYSSAL KNIGHT – 3 PHASES) `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 2.4.3, Phần 8, Phần 14* & GDD – *Mục 3.7, Mục 6*.
- **Mục tiêu:** Thiết kế và triển khai hoàn chỉnh trận đánh Boss cuối đầu tiên **Abyssal Knight** 3 giai đoạn với mô hình 3D uy nghi, tín hiệu báo đòn rõ ràng (*Telegraph*) và cơ chế biến đổi đấu trường.
- **Chi tiết Requirement:**
  1. **Tự tạo Tài nguyên 3D Boss Abyssal Knight (Phần 2.4.3):**
     - Model 3D chất lượng cao thể hiện sự nguy hiểm và quyền lực: *Heavy Armor* (đại giáp gai hắc ám), *Greatsword* (đại kiếm vực thẳm), *Glowing Abyssal Energy* (năng lượng vực thẳm phát sáng trên giáp/vũ khí).
     - *Unique Skeleton/Rig*, bộ *Animation riêng cho từng Phase*, *Skill VFX* đặc trưng và *Death Animation* hoành tráng.
  2. **Bộ Kỹ năng & Kịch bản Chiến đấu 3 Phase chuẩn GDD:**
     - **Phase 1 (100% → 70% HP – Kiếm kỹ & Khiên chiến):**
       - *Sword Slash*: Chuỗi đại kiếm chém quét vòng cung rộng phía trước.
       - *Shield Bash*: Đập khiên/vai cận chiến gây choáng (*Stun*) và đẩy lùi (*Knockback*).
       - *Charge*: Khóa hướng lao thẳng tốc độ cao để lại vệt chấn động.
     - **Phase 2 (70% → 35% HP – Thức tỉnh Năng lượng Hắc Ám):**
       - *Dark Slash*: Kiếm khí hắc ám phóng tầm xa theo hình nón/đường thẳng.
       - *Summon Skeleton*: Triệu hồi quân đoàn Skeleton hỗ trợ gây áp lực lên đội hình.
       - *Area Attack*: Chấn động vực thẳm nổ diện rộng xung quanh Boss buộc người chơi phải giãn khoảng cách hoặc canh nhịp né.
     - **Phase 3 (35% → 0% HP – Cuồng Nộ Vực Thẳm):**
       - *Enrage*: Cường hóa tốc độ ra đòn, sát thương và hiệu ứng hào quang đỏ/tím hắc ám.
       - *Meteor Attack*: Triệu hồi mưa thiên thạch vực thẳm rơi liên tiếp xuống nhiều vị trí trên sàn đấu.
       - *Change Arena*: Biến đổi đấu trường (thu hẹp vùng an toàn bằng vòng lửa hắc ám / khe nứt năng lượng / cột trụ sụp đổ).
       - *Summon Elite Monsters*: Triệu hồi quái Tinh Anh (Elite) tham chiến ở giai đoạn sinh tử.
  3. **8 Yêu cầu Trải nghiệm Trận Boss bắt buộc:**
     - *Boss Health Bar*: Thanh máu Boss cỡ lớn trên HUD kèm tên danh hiệu, vạch chia mốc Phase và trạng thái hiệu ứng.
     - *Phase Transition*: Hoạt cảnh/hiệu ứng chuyển giao giữa Phase 1 → 2 → 3 rõ ràng (miễn nhiễm ngắn/gầm thét đẩy lùi/đổi nhạc & ánh sáng).
     - *Attack Telegraph*: **Mọi đòn đánh nguy hiểm bắt buộc có tín hiệu báo trước** (vòng/hình nón/đường thẳng cảnh báo trên mặt đất + tư thế gồng chiêu *wind-up* + âm thanh cảnh báo) để người chơi đọc chiêu và né tránh bằng kỹ năng cá nhân, không chỉ đọ lượng máu lớn.
     - *Boss Animation*, *Skill VFX*, *Damage Feedback* (nháy sáng vật liệu, rung chấn, stagger gauge), *Arena Mechanics* và *Victory Sequence* (chuỗi hiệu ứng quay chậm/ánh sáng chiến thắng/rơi chiến lợi phẩm khi hạ gục Boss).
  - **Skills bắt buộc đọc & áp dụng:** `router`, `game-ai`, `game-feel`, `camera-systems`, `unity-animation`, `shader-programming`, `audio-design`, `create-game-assets`.
  - **Tiêu chí nghiệm thu Phase 7:** Abyssal Knight vận hành mượt đủ 3 Phase, đủ 10 chiêu thức/cơ chế theo GDD, mọi chiêu diện rộng/lao tới đều có Telegraph trực quan tránh được bằng di chuyển/Dodge, chuyển đổi đấu trường ở Phase 3 hoạt động ổn định và kích hoạt chuỗi chiến thắng khi HP về 0.

---

### PHASE 8 – PROGRESSION, LOOT, EQUIPMENT & SAVE SYSTEM `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 2.4.5, Phần 9, Phần 14* & GDD – *Mục 3.8, Mục 3.9, Mục 3.10, Mục 6*.
- **Mục tiêu:** Xây dựng trọn vẹn vòng lặp phát triển nhân vật kiểu Action RPG: nhận EXP, lên cấp, phân bổ điểm & cây kỹ năng, rơi chiến lợi phẩm 6 bậc hiếm, trang bị 3D, so sánh đồ và lưu trữ dữ liệu an toàn.
- **Chi tiết Requirement:**
  1. **Hệ thống Độ hiếm 6 Bậc (Item Rarity):**
     `Common` (Trắng/Xám) → `Uncommon` (Lục) → `Rare` (Lam) → `Epic` (Tím) → `Legendary` (Cam/Vàng) → `Mythic` (Đỏ/Huyền thoại).
  2. **Các ô Trang bị & Thư viện 3D Weapon/Equipment (Phần 9 & Phần 2.4.5):**
     - **5 Ô trang bị chính trong GDD:** *Weapon, Armor, Helmet, Ring, Amulet*.
     - **Thư viện 3D Asset Trang bị Dark Fantasy (Phần 2.4.5):** Tạo mô hình 3D cho *Sword, Greatsword, Dagger, Bow, Staff, Shield, Helmet, Armor, Gloves, Boots, Rings, Amulets*.
     - **Đặc quyền cấp cao:** Trang bị bậc **Legendary** và **Mythic** bắt buộc có ngoại hình 3D đặc biệt, hiệu ứng phát sáng (*Emission/Rim Glow*) hoặc hạt ma thuật bao quanh, kèm dòng nội tại đặc biệt.
  3. **8 Chỉ số Trang bị & Nhân vật chuẩn GDD (Stats):**
     1. *Attack* (Sức tấn công)
     2. *Defense* (Phòng thủ)
     3. *HP* (Máu tối đa)
     4. *Critical Chance* (Tỷ lệ chí mạng)
     5. *Attack Speed* (Tốc độ đánh)
     6. *Mana* (Năng lượng tối đa)
     7. *Lifesteal* (Hút máu)
     8. *Cooldown Reduction* (Giảm thời gian hồi chiêu)
  4. **Hệ thống EXP, Level Up & Skill Build:**
     - **5 Nguồn nhận EXP:** *Normal Enemy*, *Elite Enemy*, *Mini Boss*, *Final Boss*, *Dungeon Objectives* (hoàn thành sự kiện/giải đố/vượt ải).
     - **Khi Level Up:** Tăng chỉ số gốc (*Increase Stats*), nhận điểm kỹ năng (*Skill Points*), mở khóa/nâng cấp kỹ năng (*Unlock Skills*), và định hình hướng *Skill Build* chuyên sâu cho từng Class.
  5. **Trải nghiệm Nhặt đồ (Loot Feel) & So sánh Trang bị:**
     - Vật phẩm rơi ra thế giới 3D có cột sáng màu theo bậc hiếm (*Loot Beam*), âm thanh rơi đồ đặc trưng và hiệu ứng nhặt đồ.
     - Giao diện Túi đồ & Trang bị trực quan, tích hợp **Hệ thống So sánh Trang bị (Equipment Comparison Tooltip)** hiển thị rõ mức chênh lệch `+/-` từng chỉ số so với món đang mặc.
  6. **Save System (Lưu & Tải Dữ liệu Bền vững):**
     - Lưu trữ toàn bộ tiến trình nhân vật (Class, Level, EXP, Gold, Inventory, Equipment đang mặc, Skill Points, Skill Build, Tầng phó bản đã mở) với cơ chế ghi file an toàn (*atomic write*), có đánh phiên bản schema (*schema versioning*) chống hỏng file save.
  - **Skills bắt buộc đọc & áp dụng:** `router`, `rpg`, `save-systems`, `procedural-gen` (weighted loot tables), `unity-scriptableobjects`, `create-game-assets`, `game-ui-ux`.
  - **Tiêu chí nghiệm thu Phase 8:** Đánh quái/Boss nhận EXP lên cấp đúng công thức; rơi vật phẩm đủ 6 phẩm chất với cột sáng 3D; mặc/tháo 5 ô trang bị cập nhật ngay chỉ số thực chiến và ngoại hình vũ khí/giáp; tooltip so sánh chỉ số chính xác; thoát game vào lại giữ nguyên 100% dữ liệu.

---

### PHASE 9 – ABYSS DIRECTOR AI `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 7, Phần 14* & GDD – *Mục 3.6, Mục 6*.
- **Mục tiêu:** Xây dựng trí tuệ nhân tạo điều phối phó bản (*Dynamic Difficulty & Pacing Director*) theo dõi hiệu suất thực tế của tổ đội để điều chỉnh các phòng tiếp theo một cách cân bằng, công bằng.
- **Chi tiết Requirement:**
  1. **Thu thập & Phân tích 7 Chỉ số Hiệu suất Tổ đội (Performance Telemetry):**
     1. *Death Count* (Số lần thành viên trong đội gục ngã/chết).
     2. *Damage Dealt* (Tổng sát thương gây ra & DPS trung bình).
     3. *Damage Taken* (Tổng sát thương gánh chịu).
     4. *Healing* (Tổng lượng hồi máu & tạo khiên thực tế).
     5. *Completion Time* (Thời gian dọn từng phòng và tiến độ thời gian tổng).
     6. *Party Composition* (Thành phần vai trò tổ đội: thiếu Tank, thiếu Healer, hay thiên công/khống chế).
     7. *Enemy Kill Speed* (Tốc độ tiêu diệt quái trung bình).
  2. **Quy tắc Điều chỉnh Thích ứng cho Encounter Tiếp theo (Adaptive Encounter Rules):**
     - **Khi tổ đội quá mạnh (áp đảo nhanh, ít mất máu, 0 lần gục):**
       - Tăng độ khó encounter kế tiếp một cách có kiểm soát.
       - Bổ sung thêm **Elite Monster** hoặc biến thể quái khắc chế đội hình.
       - Tăng áp lực chiến đấu (nhịp spawn đợt tiếp theo nhanh hơn, phối hợp quái chặt hơn, kèm phần thưởng tương xứng).
     - **Khi tổ đội gặp khó khăn (mất nhiều máu, có người gục, tốc độ hạ quái chậm, thiếu Healer):**
       - Giảm mật độ quái trong đợt tiếp theo.
       - Tạo thêm **cơ hội hồi phục** (triệu hồi Tế đàn Hồi máu/Mana, tăng tỷ lệ rơi bình hồi phục).
       - Điều chỉnh giảm nhẹ áp lực sát thương của encounter kế tiếp.
  3. **Giới hạn Toán học & Sự Công bằng (Fairness & Clamping Formulas):**
     - **Bắt buộc sử dụng các ngưỡng giới hạn trên/dưới (min/max clamps), hàm làm mượt (smoothing/hysteresis) và ngân sách điểm nguy hiểm (Threat Budget)** để tránh tăng/giảm độ khó đột ngột cực đoan.
     - Đảm bảo người chơi luôn cảm nhận thử thách **công bằng, kịch tính**, tuyệt đối không cảm thấy game "gian lận" hoặc trừng phạt vô lý.
  - **Skills bắt buộc đọc & áp dụng:** `router`, `ai-behavior-trees-utility-ai` (response/scoring curves), `game-ai`, `level-design` (tension/rest pacing curve), `unity-scriptableobjects`.
  - **Tiêu chí nghiệm thu Phase 9:** Hệ thống ghi nhận chính xác cả 7 thông số thời gian thực, tính toán điểm hiệu suất có giới hạn biên rõ ràng, điều chỉnh thực tế encounter phòng kế tiếp (thêm Elite khi đội mạnh / giảm mật độ & sinh đài hồi phục khi đội yếu) và có bảng log/chỉ báo minh bạch để kiểm thử khi chấm Lab.

---

### PHASE 10 – MULTIPLAYER CO-OP (3–4 PLAYERS) `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 10, Phần 14* & GDD – *Mục 3.2, Mục 4.2, Mục 6*.
- **Mục tiêu:** Triển khai hệ thống Co-op cốt lõi hỗ trợ tổ đội **3–4 người chơi**, đồng bộ đầy đủ sảnh chờ, chiến đấu, quái vật, tiến trình hầm ngục, phân phối chiến lợi phẩm và trận đánh Boss.
- **Chi tiết Requirement:**
  1. **Phân tích Kiến trúc & Minh bạch Chế độ Chơi (Yêu cầu bắt buộc Phần 10):**
     - Phân tích phiên bản Unity (`2021.3.11f1`) và các package hiện có trong dự án để lựa chọn/triển khai giải pháp networking phù hợp.
     - **Cấm tuyệt đối:** Không giả lập multiplayer bằng cách chỉ tạo nhiều nhân vật AI rồi tuyên bố đã hoàn thành multiplayer.
     - Phải **phân biệt rõ ràng, minh bạch** trong kiến trúc và giao diện giữa:
       1. *Single-player mode (cùng AI đồng đội hỗ trợ tổ đội 3–4 người)* — đảm bảo chạy cực kỳ ổn định trước tiên.
       2. *Local / LAN Multiplayer* (Host/Client qua mạng nội bộ / loopback nhiều cửa sổ máy trạm).
       3. *Online Multiplayer* (kiến trúc transport tách lớp sẵn sàng mở rộng Relay/Online).
  2. **12 Chức năng Multiplayer & Party bắt buộc:**
     1. *Create Room* (Tạo phòng chơi mới với cấu hình cổng/mã phòng).
     2. *Join Room* (Tham gia phòng bằng địa chỉ IP / mã phòng / danh sách LAN).
     3. *Invite Player* (Mời người chơi / thêm thành viên vào sảnh).
     4. *Party Members* (Quản lý danh sách 3–4 thành viên, hỗ trợ gợi ý 4 đội hình GDD: *Cân bằng, Công, Thủ, Khống chế*).
     5. *Class Selection* (Mỗi người chơi chọn 1 trong 10 Class và hướng Build, đồng bộ hiển thị cho cả phòng).
     6. *Ready / Unready* (Trạng thái sẵn sàng của từng thành viên trước khi Host bắt đầu phó bản).
     7. *Player Synchronization* (Đồng bộ vị trí, hướng xoay, vận tốc, animation, máu/mana của các người chơi).
     8. *Enemy Synchronization* (Server/Host-authoritative đồng bộ spawn, mục tiêu, di chuyển và máu của quái vật).
     9. *Combat Synchronization* (Đồng bộ kỹ năng thi triển, sát thương, hiệu ứng trạng thái, hồi máu và cứu đồng đội gục ngã).
     10. *Dungeon Synchronization* (Đồng bộ trạng thái khóa/mở cửa phòng, bẫy, giải đố, sự kiện và Abyss Director).
     11. *Loot Distribution* (Phân phối chiến lợi phẩm & EXP công bằng cho các thành viên trong tổ đội).
     12. *Boss Synchronization* (Đồng bộ máu Boss, chuyển Phase, vị trí vòng cảnh báo Telegraph và biến đổi đấu trường).
  - **Skills bắt buộc đọc & áp dụng:** `router`, `unity-csharp-scripting`, `unity-build-pipeline` (test 2 instance Host + Client), `game-ui-ux`.
  - **Tiêu chí nghiệm thu Phase 10:** Chế độ Single-player + Party AI chạy mượt 100%; đồng thời mở được thực tế **2 cửa sổ game (Host + Client)** kết nối vào cùng phòng Lobby, thấy nhau đổi Class & bấm Ready, cùng vào Dungeon di chuyển, tung chiêu, đánh quái/Boss và nhận Loot đồng bộ thời gian thực.

---

### PHASE 11 – UI/UX (DARK FANTASY RPG – 16 MÀN HÌNH & HUD) `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 11, Phần 14* & GDD – *Mục 4.1–4.7, Mục 5, Mục 6*.
- **Mục tiêu:** Thiết kế và đồng bộ toàn bộ giao diện người dùng theo phong cách **Dark Fantasy RPG** sang trọng, sắc nét (loại bỏ hoàn toàn diện mạo UI xám mặc định của Unity).
- **Chi tiết Requirement:**
  1. **Đầy đủ 16 Màn hình / Giao diện Chức năng (Phần 11):**
     1. **Main Menu** (*Play → sang Multiplayer Lobby, Instructions, Settings, Town, Quit*).
     2. **Instructions** (Hướng dẫn điều khiển, 10 Class, Combat, Party và mục tiêu Dungeon).
     3. **Settings** (Âm lượng Master/BGM/SFX, độ nhạy chuột, khoảng cách camera, chất lượng đồ họa, độ phân giải).
     4. **Multiplayer Lobby** (Tạo/vào phòng, mời người chơi, đội hình 3–4 người, Ready/Unready).
     5. **Class Selection** (Xem mô hình 3D xoay trực tiếp, chọn 1 trong 10 Class, xem chỉ số, kỹ năng & hướng Build).
     6. **Character Status** (Bảng chi tiết 8 chỉ số chiến đấu, cấp độ, EXP, hướng Build hiện tại).
     7. **Inventory** (Túi đồ phân loại, lọc theo phẩm chất, thao tác mặc/bán/tách đồ).
     8. **Equipment** (Giao diện 5 ô trang bị nhân vật kèm so sánh chỉ số trực tiếp).
     9. **Skill Tree** (Cây kỹ năng / nâng cấp bậc chiêu thức và chuyển đổi 3 nhánh Build cho từng Class).
     10. **Party Management** (Quản lý đội hình, vai trò Tank/DPS/Support/CC và trạng thái đồng đội).
     11. **Dungeon HUD** (Giao diện chiến đấu chính trong hầm ngục).
     12. **Boss Health Bar** (Thanh máu Boss đa giai đoạn kèm tên & cảnh báo chuyển Phase).
     13. **Map** (Bản đồ tổng quan các phòng trong Dungeon & Mini-map radar thời gian thực).
     14. **Loot Reward** (Thông báo/bảng chiến lợi phẩm nhặt được và mở rương).
     15. **Dungeon Completion** (Xác nhận hoàn thành phó bản khi hạ gục Boss cuối).
     16. **Town** (Khu căn cứ/thị trấn 3D với giao diện Thợ rèn, Giả kim, Huấn luyện nâng cấp và Cổng vào lại Dungeon — kèm màn hình **End Game** thống kê đầy đủ: *Trạng thái hoàn thành, Level & EXP nhận được, Gold & vật phẩm, Sát thương gây ra, Sát thương nhận vào, Lượng hồi máu, Số quái tiêu diệt, Thời gian hoàn thành* và các nút *Về Town, Chơi lại, Main Menu, Thoát*).
  2. **Đầy đủ 9 Thành phần HUD Chiến đấu (Phần 11 & GDD Mục 5):**
     - *HP Bar*, *Mana/Energy Bar*, *Level*, *EXP Bar*, *Skill Bar (`Q / W / E / R` hiển thị hồi chiêu & tiêu hao)*, *Party HUD (tên, icon class, HP & trạng thái các thành viên)*, *Dungeon Objective (mục tiêu hiện tại, tiến trình phòng & số lượng quái còn lại)*, *Boss HP*, và *Mini-map*.
  3. **Tiêu chuẩn Thẩm mỹ & UX:**
     - Typography sắc nét, dễ đọc (hiển thị chuẩn đầy đủ dấu Tiếng Việt).
     - Bộ Icon kỹ năng, Class, trang bị và trạng thái đồng nhất phong cách Dark Fantasy.
     - Có hiệu ứng chuyển cảnh mượt mà (*Screen Transitions*), hoạt ảnh phản hồi nút bấm/mở bảng (*UI Animations*).
     - *Responsive Layout* (neo tỉ lệ chuẩn `CanvasScaler`), hiển thị tốt trên nhiều độ phân giải và tỉ lệ màn hình (`16:9`, `16:10`, `21:9`) mà không chồng lấn chữ.
  - **Skills bắt buộc đọc & áp dụng:** `router`, `game-ui-ux`, `create-game-assets`, `unity-csharp-scripting`, `audio-design` (UI SFX).
  - **Tiêu chí nghiệm thu Phase 11:** Luồng chuyển Scene khép kín `Main Menu → Lobby → Class Selection → Dungeon → End Game ↔ Town` mượt mà; đủ 16 màn hình/bảng giao diện và 9 thành phần HUD hoạt động theo sự kiện (event-driven), không dùng giao diện trắng/xám mặc định của Unity, không lỗi cắt chữ ở các độ phân giải phổ biến.

---

### PHASE 12 – POLISH, OPTIMIZATION & BUILD TESTING `[ĐÃ HOÀN THÀNH]`
- **Nguồn đối chiếu:** Master Prompt – *Phần 12, Phần 14, Phần 15, Phần 16* & GDD – *Mục 6 (Yêu cầu thực hiện Lab)*.
- **Mục tiêu:** Hoàn thiện chất lượng nghe-nhìn đồng bộ cấp sản phẩm, tối ưu hóa hiệu năng CPU/GPU/Memory, rà soát sửa sạch lỗi trên toàn bộ 6 Scene và đóng gói kiểm thử bản Build thực tế.
- **Chi tiết Requirement:**
  1. **Visual & Animation Polish:**
     - Tinh chỉnh đồng bộ ánh sáng, đổ bóng, Post-Processing, vật liệu PBR và chuyển tiếp hoạt ảnh mượt mà cho cả 10 Class, 10 Enemy và Boss Abyssal Knight.
  2. **VFX & Audio Mixing Polish:**
     - Hoàn thiện hiệu ứng hạt kỹ năng, tia chém, cảnh báo Boss, hiệu ứng lên cấp/nhặt đồ Legendary–Mythic.
     - Cân bằng âm lượng theo kiến trúc Audio Bus (*Master / BGM / Combat SFX / UI / Ambience*), thêm biến thiên cao độ (*pitch/volume variation*) chống lặp âm nhàm chán, và nhạc nền chuyển pha theo cường độ chiến đấu/Boss Phase.
  3. **Performance Optimization (Tối ưu Hiệu năng):**
     - Áp dụng **Object Pooling** cho toàn bộ đối tượng sinh/hủy liên tục (số sát thương nổi, đạn/chưởng phép, hạt va chạm, quái triệu hồi, vật phẩm rơi) nhằm triệt tiêu giật khung hình do rác bộ nhớ (*GC Alloc spikes* trong `Update`).
     - Kiểm tra và tối ưu **LOD Group**, **Static/SRP Batcher**, **Occlusion/Frustum Culling**, polygon count, kích thước nén Texture và số lượng Draw Calls / Realtime Shadow Lights.
  4. **Bug Fixing & Kiểm tra Toàn diện Hệ thống:**
     - Rà soát toàn bộ tham chiếu `null`, va chạm vật lý kẹt góc tường, ngoại lệ chuyển Scene nhanh, lưu/tải file save biên và đồng bộ mạng.
  5. **Build Testing & Nghiệm thu Cuối cùng (Checklist Lab GDD Mục 6):**
     - Cấu hình `Build Settings` chuẩn thứ tự các Scene (`MainMenu`, `Lobby`, `ClassSelection`, `Dungeon`, `EndGame`, `Town` + `VisualShowcase`).
     - Kiểm tra biên dịch Standalone Player (`Windows x64`) `0 errors`, đối chiếu tích xanh 100% các mục trong **Mục 6 – Yêu cầu thực hiện Lab** của `Kich_Ban_Game_Abyssal_Dungeon.docx` và bàn giao tài liệu hướng dẫn chạy/kiểm thử hoàn chỉnh.
  - **Skills bắt buộc đọc & áp dụng:** `router`, `performance-optimization`, `game-feel`, `audio-design`, `unity-build-pipeline`, `physics-tuning`.
  - **Tiêu chí nghiệm thu Phase 12:** Toàn bộ dự án compile `0 errors` cho cả Editor & Player Build; FPS ổn định trong cả trận Boss Phase 3 đông hiệu ứng; không phát sinh lỗi đỏ Console trong suốt vòng lặp chơi hoàn chỉnh từ `Main Menu` đến `End Game` & `Town`.

---

## PHỤ LỤC: ĐỐI CHIẾU YÊU CẦU THỰC HIỆN LAB TRONG GDD (`MỤC 6`)

| STT | Hạng mục Yêu cầu Lab (GDD Mục 6) | Chi tiết bắt buộc | Phase Đáp ứng |
| :---: | :--- | :--- | :--- |
| 1 | **Tạo Main Menu Scene** | Thiết kế menu gồm *Play, Instructions, Settings, Quit*; thực hiện chuyển Scene. | Phase 11 |
| 2 | **Xây dựng Multiplayer Lobby** | Tạo & tham gia phòng, hiển thị người chơi, chọn Class, *Ready/Unready*. | Phase 10, 11 |
| 3 | **Điều khiển nhân vật** | Di chuyển góc nhìn thứ ba, điều khiển camera, đánh thường, kích hoạt kỹ năng, *Dodge*. | Phase 2, 3 (`Đã xong`) |
| 4 | **Hệ thống Class** | Chọn Class (10 Class), chỉ số riêng, kỹ năng cơ bản & nâng cao, vai trò chiến đấu & Build. | Phase 4 (`Đã xong`) |
| 5 | **Tạo quái vật** | Spawn quái, di chuyển, chiến đấu, đủ 10 loại Enemy + Elite. | Phase 5 |
| 6 | **AI quái vật** | Phát hiện, chọn mục tiêu, Navigation, tấn công, hành vi khác nhau (*Archer, Assassin, Tank*). | Phase 5 |
| 7 | **Xây dựng Dungeon** | Nhiều phòng, chuyển phòng, Encounter thường, Encounter Elite, Event/Puzzle, Mini-Boss, Boss cuối. | Phase 6 |
| 8 | **AI Dungeon Director** | Theo dõi 7 chỉ số hiệu suất, phân tích tiến trình, điều chỉnh encounter & độ khó theo tổ đội. | Phase 9 |
| 9 | **Loot và Leveling** | EXP, Level, 5 ô Trang bị, 6 bậc Độ hiếm, nâng 8 chỉ số, so sánh đồ & lưu trữ. | Phase 8 |
| 10 | **Boss** | Boss *Abyssal Knight* 3 Phase, kỹ năng từng Phase, thanh HP Boss, Telegraph & chuyển Phase. | Phase 7 |
| 11 | **Chuyển Scene** | Luồng khép kín: `Main Menu → Lobby → Class Selection → Dungeon → End Game (↔ Town)`. | Phase 11, 12 |
| 12 | **Hoàn thành game** | Phát hiện Boss bị hạ, tính phần thưởng, hiển thị bảng thống kê thành tích, đưa về Town/Main Menu. | Phase 7, 11, 12 |

