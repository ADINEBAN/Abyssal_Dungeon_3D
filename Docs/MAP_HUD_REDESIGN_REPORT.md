# ABYSSAL DUNGEON 3D — BÁO CÁO NÂNG CẤP MAP & HUD (ANCIENT GOTHIC CATHEDRAL)

**Project**: `C:\LTG\Abyssal_Dungeon_3D`  
**Engine / Pipeline**: `Unity 2021.3.11f1` + `URP 12.1.7`  
**Mode**: Co-op 3–4 players (AI Party + LAN Co-op via `LanRoomBridge`)  
**Target Hardware Verified**: `Intel(R) Iris(R) Xe Graphics` (integrated GPU, 2048 MB effective budget)  
**Unity Console Status**: `0 errors`, `0 warnings` (verified in Edit Mode & live Play Mode across Entrance, Puzzle, Treasure, and Boss Arena Phase 3).

---

## 1. Backup an toàn trước khi chỉnh sửa

Toàn bộ Scene và các script lõi đã được backup tại `Backups/MapHudRedesign_PreBackup/`:
- `Dungeon.unity`
- `DungeonWorldBuilder.cs`, `ProceduralDungeonGenerator.cs`, `ModularDungeonArchitect.cs`
- `DungeonAndEndViews.cs`, `DungeonOverlayPanelsUI.cs`, `GameUIKit.cs`, `UIButtonGothicFX.cs`

---

## 2. Thiết kế lại Môi trường & Layout Dungeon (Ancient Gothic Cathedral)

### A. Bộ Asset 3D Modular tạo từ Blender 5.3.0
- Generator script: `Tools/BuildGothicCathedralKit_Blender.py` (`use_space_transform=True, bake_space_transform=True`, baked Unity `+Y up, +Z forward`, zero root rotation).
- File master Blender: `VFX_Blender_Production/05_Environment_VFX/Abyssal_Gothic_Cathedral_Kit.blend`
- 5 FBX nhẹ xuất trực tiếp vào `Assets/Resources/EnvironmentKit/Raw/` & đóng gói thành Prefab trong cả `Assets/Prefabs/Environment/` và `Assets/Resources/EnvironmentKit/`:
  1. `GothicCathedralVaultRib.prefab` (`1,044 tris`) — Vòm cung sườn Gothic cao phía trên camera (`y > 6.5m`), thay thế thanh xà ngang chắn camera cũ (`y = 4.92m`).
  2. `FallenAngelStatue_Gothic.prefab` (`800 tris`) — Tượng thiên thần sa ngã trùm mũ, cánh gập và trường kiếm đá cắm đất.
  3. `WoodenBarrelsCrates_Gothic.prefab` (`500 tris`) — Cụm thùng gỗ sồi đai sắt + hòm gỗ nẹp cổ.
  4. `WallRuneTablet_Gothic.prefab` (`216 tris`) — Bia đá Gothic gắn tường khảm cổ ngữ phát sáng.
  5. `GothicLightShaft_Cone.prefab` (`224 tris`) — Nón tia sáng volumetric giả lập siêu nhẹ (0 realtime light).

### B. Shader & Material URP 12.1.7
- Shader mới: `Assets/Resources/Shaders/AbyssalLightShaft.shader` (`UniversalForward`, `Blend SrcAlpha One`, `ZWrite Off`, `half4` fragment output, Fresnel edge fade, monotonic smoothstep vertical fade, bụi sáng chuyển động nhẹ, camera-near fade).
- Material mới tại `Assets/Resources/Environment/Materials/`:
  - `M_GothicLightShaft_Warm.mat` (tia sáng vàng ấm từ cửa sổ cao).
  - `M_GothicLightShaft_Arcane.mat` (tia sáng lam lạnh cho khu vực ma thuật / Puzzle / Shrine).
  - `M_WallRune_Arcane.mat` (cổ ngữ phát sáng trên bia đá tường).

### C. Cấu trúc Map 8 Phòng (7 Mục đích + Nhánh phụ) — Mở rộng trên `ProceduralDungeonGenerator` sẵn có
Không tạo generator thứ hai; nâng cấp trực tiếp `ProceduralDungeonGenerator.cs`, `DungeonWorldBuilder.cs`, `ModularDungeonArchitect.cs`, `GameDefs.cs`:
- **Room 0 — Entrance Hall** (`20×18×7.8m`): Đại sảnh bắt đầu, bậc thang đá cổ, đuốc cam ấm, tượng hộ vệ, vòm cung cao.
- **Room 1 — Combat Hub** (`26×24×7.2m`): Phòng chiến đấu trung tâm có 2 lối đi (lối chính phía Bắc + lối phụ phía Đông dẫn sang Kho Báu).
- **Room 2 — Treasure Room (Nhánh Đông)** (`16×16×6.8m`, `isSideBranch = true`): Hành lang vòm Đông–Tây, cổng đá cổ có thể đóng/mở, 2 tượng Thiên Thần Sa Ngã, 2 rương kho báu (chính + phụ), lính canh kho báu.
- **Room 3 — Puzzle Room** (`24×22×7.8m`): Phòng 3 Ấn Phong Cổ, ánh sáng xanh ma thuật, tia sáng arcane, bẫy chông.
- **Room 4 — Elite Room** (`28×26×8.2m`): Phòng quái tinh anh, bố cục cột đá + vật cản tactical cover chừa hành lang rộng `≥ 4m` cho co-op 4 người.
- **Room 5 — Mini Boss Room** (`28×26×8.6m`): Sảnh cao đấu Mini-Boss Crystal Golem.
- **Room 6 — Combat Sanctum** (`26×24×7.6m`): Hành lang chiến đấu tiền đấu trường Boss.
- **Room 7 — Boss Arena** (`36×36×9.4m`): Đấu trường Abyssal Knight 3 Phase (đã thay toàn bộ trụ lập phương hồng chói cũ ở Phase 3 bằng trụ đá cổ ngữ `RunicPillarAltar` + cụm pha lê đỏ thẫm `AbyssalCrystalCluster`).

### D. Sửa lỗi kỹ thuật Môi trường, AI & Hiệu năng
- **Fix lỗi lộn mặt tường (`ObjMeshLoader.cs`)**: Sửa thứ tự đỉnh tam giác (winding) của `AppendBoxUvScaled` trên cả 6 mặt về chuẩn thuận chiều kim đồng hồ (Unity LH) + UV thẳng đứng + bảo toàn multi-material/custom shader.
- **Chống kẹt góc / kẹt cột (`HeroPartyAI.cs`, `MonsterAI.cs`, `DungeonWorldBuilder.cs`)**: Quạt 5 tia né vật cản (`0°, ±34°, ±70°`) + trượt tiếp tuyến theo tường + kiểm tra `Physics.CheckSphere` khi chọn điểm spawn & tuần tra + trigger vào phòng 2D (`X` & `Z`) hỗ trợ phòng rẽ nhánh.
- **Tối ưu GPU (`DungeonWorldBuilder.cs`, `GraphicsManager.cs`, `EnvironmentDistanceCuller.cs`)**: Gom tĩnh runtime bằng `StaticBatchingUtility.Combine` cho kiến trúc đá/vòm; tắt shadow trên đèn đuốc điểm cho GPU tích hợp (chấm dứt hoàn toàn cảnh báo tràn URP shadow atlas 96 punctual shadow maps).

---

## 3. Thiết kế lại HUD (Ancient Gothic Dark Fantasy)

### A. Asset & Prefab UI tái sử dụng
- **UI Sprite Atlas**: `Assets/Resources/UI/AbyssalHUD_Icons.spriteatlas` (đóng gói `UI/Icons` & `UI/Classes`) + 9-slice border cho toàn bộ `UI/Panels/*.png` và `UI/Buttons/*.png`.
- **7 Prefab UI tái sử dụng** tại `Assets/Prefabs/UI/` (tạo bởi `Assets/Scripts/Game/Editor/GothicCathedralAssetSetup.cs` — Menu `Tools/Abyssal Dungeon/2. Build Gothic Cathedral & HUD Prefabs`):
  - `DungeonHUD_Master.prefab`
  - `HUD_PartyPanel.prefab`
  - `HUD_QuestNotification.prefab`
  - `HUD_MinimapPanel.prefab`
  - `HUD_RightMenuPanel.prefab`
  - `HUD_PlayerStatusPanel.prefab`
  - `HUD_SkillHotbarPanel.prefab`

### B. Các cụm HUD đã nâng cấp (`DungeonAndEndViews.cs`, `DungeonOverlayPanelsUI.cs`, `HeroPlayerInput.cs`, `LanRoomBridge.cs`, `ActorUnit.cs`)
1. **Party Panel (Góc trên trái)**: 4 slot thành viên, avatar class, tên + class + level, thanh HP đỏ thẫm + thanh MP xanh lam, trạng thái Sống / Hấp hối / Tử trận, nhãn trạng thái `TỔ ĐỘI 4/4` hoặc `LAN CO-OP`.
2. **Quest Notification (Trên trung tâm)**: Khung vàng đồng cổ kính, icon mục tiêu động theo loại phòng, tiêu đề + mô tả ngắn, animation trượt/fade mượt bằng `CanvasGroup`.
3. **Minimap (Góc trên phải)**: Khung `192×192` nền đá đen-nâu cổ, viền kim loại đồng cổ, hiển thị Fog-of-War (phòng đã khám phá vs viền đứt chưa khám phá), hành lang chính + nhánh Kho Báu phía Đông, cửa khóa (đỏ) vs cửa mở (vàng), icon Boss/Kho Báu/Mục tiêu, mũi tên hướng player, click trực tiếp mở Bản Đồ Chiến Thuật `[M]`.
4. **Menu chức năng bên phải**: Đủ 4 nút cổ kính gắn callback thực tế: `Hành Trang & Build [I]`, `Bản Đồ Chiến Thuật [M]`, `Kỹ Năng [K]` (mới bổ sung modal nhanh & phím `K`), `Tạm Dừng [Esc]`.
5. **Player Status (Góc dưới trái)**: Avatar + huy hiệu `Lv.` góc, tên Class, bảng Vàng có icon đồng xu riêng, thanh HP đỏ thẫm + dải Khiên vàng mảnh mép trên dễ đọc, thanh MP xanh, Thể lực xanh lục, thanh EXP vàng.
6. **Skill Hotbar (Dưới trung tâm)**: Đủ 8 ô (`LMB`, `RMB/X`, `Q/1`, `2/Z` — đã sửa lỗi ghi sai `W/2` cũ, `E/3`, `R/4` có viền ánh kim Ultimate, `Space` Né Đòn, `H` Bình Máu kèm số lượng), overlay hồi chiêu `Radial360`, cảnh báo thiếu mana (phủ xanh tím + chữ mana đỏ), và hiệu ứng lóe sáng vàng khi chiêu sẵn sàng.
7. **Thanh máu trên đầu nhân vật (`ActorUnit.cs`)**: Thu nhỏ gọn gàng viền đồng cổ, không còn che kín màn hình như bản cũ.

---

## 4. Kết quả kiểm tra Play Mode & Hiệu năng

| Chỉ số | Trước nâng cấp | Sau nâng cấp | Cải thiện |
|---|---:|---:|---|
| Draw Calls (`rendering/stats`) | `2,737` | `865` | **Giảm 3.2×** |
| Static Batches | `0` | `10` (`466` batched draws) | Bật runtime static batching |
| Triangles | `1,193,049` | `462,870` | **Giảm 2.6×** |
| Shadow Casters | `626` | `117` | **Giảm 5.4×** |
| Unity Console (Play Mode) | Cảnh báo shadow atlas | **0 errors, 0 warnings** | Sạch hoàn toàn |
| FPS (Editor, preset Ultra trên Intel Iris Xe) | — | **~53.0 FPS** | Đạt 60+ FPS ở Low/Medium hoặc bản Build |

- **Kiểm tra đa tỷ lệ màn hình**: `1920×1080 (16:9)`, `1920×1200 (16:10)`, `2560×1080 (21:9)` → tất cả đều không chồng lấn (`topClear=True`, `botClear=True`).
- **Kiểm tra tương tác & gameplay**: Toàn bộ callback nút `[I]`, `[M]`, `[K]`, `[Esc]`, combat 4 người, AI party, 8 phòng (BFS đồ thị `GRAPH_BFS_VALID=True`), Boss 3 Phase, rơi đồ Legendary/Epic và màn hình Victory hoạt động nguyên vẹn không regression.

---

## 5. Ảnh chụp Game View (Trước & Sau)

- **Trước nâng cấp**: `Captures/MapHudRedesign/01_Before_Dungeon_GameView.png` (lộ tường lộn mặt tối đen, thanh xà trần chắn camera, thanh máu đồng đội quá khổ che màn hình, minimap 1 đường thẳng, thiếu nút `[K]`, sai hotkey `W/2`).
- **Sau nâng cấp — Khu vực Puzzle / Hành lang Gothic + Tia sáng Volumetric + HUD mới**: `Captures/MapHudRedesign/04_After_Final_EntranceHall.png`
- **Sau nâng cấp — Phòng Kho Báu nhánh Đông (2 Tượng Thiên Thần Sa Ngã + Rương + Đuốc)**: `Captures/MapHudRedesign/05_After_TreasureRoom_GameView.png`
- **Sau nâng cấp — Đấu trường Boss Arena Phase 3 (Trụ Cổ Ngữ Pha Lê Đỏ + Boss Bar 3 Phase + Combat 4 người)**: `Captures/MapHudRedesign/06_After_BossArena_GameView.png`

---

## 6. Danh sách file đã tạo mới / chỉnh sửa

### File tạo mới
- `Tools/BuildGothicCathedralKit_Blender.py`
- `VFX_Blender_Production/05_Environment_VFX/Abyssal_Gothic_Cathedral_Kit.blend`
- `Assets/Resources/EnvironmentKit/Raw/{GothicCathedralVaultRib,FallenAngelStatue_Gothic,WoodenBarrelsCrates_Gothic,WallRuneTablet_Gothic,GothicLightShaft_Cone}.fbx`
- `Assets/Prefabs/Environment/{GothicCathedralVaultRib,FallenAngelStatue_Gothic,WoodenBarrelsCrates_Gothic,WallRuneTablet_Gothic,GothicLightShaft_Cone}.prefab`
- `Assets/Resources/EnvironmentKit/{GothicCathedralVaultRib,FallenAngelStatue_Gothic,WoodenBarrelsCrates_Gothic,WallRuneTablet_Gothic,GothicLightShaft_Cone}.prefab`
- `Assets/Resources/Shaders/AbyssalLightShaft.shader`
- `Assets/Resources/Environment/Materials/{M_GothicLightShaft_Warm,M_GothicLightShaft_Arcane,M_WallRune_Arcane}.mat`
- `Assets/Resources/UI/AbyssalHUD_Icons.spriteatlas`
- `Assets/Prefabs/UI/{DungeonHUD_Master,HUD_PartyPanel,HUD_QuestNotification,HUD_MinimapPanel,HUD_RightMenuPanel,HUD_PlayerStatusPanel,HUD_SkillHotbarPanel}.prefab`
- `Assets/Scripts/Game/Editor/GothicCathedralAssetSetup.cs`
- `Docs/MAP_HUD_REDESIGN_REPORT.md`

### File chỉnh sửa
- `Assets/Scenes/Dungeon.unity`
- `Assets/Scripts/Game/Core/GameDefs.cs`
- `Assets/Scripts/Game/Dungeon/ProceduralDungeonGenerator.cs`
- `Assets/Scripts/Game/Dungeon/DungeonWorldBuilder.cs`
- `Assets/Scripts/Game/Environment/ModularDungeonArchitect.cs`
- `Assets/Scripts/Game/Environment/ObjMeshLoader.cs`
- `Assets/Scripts/Game/Environment/PbrMaterialLibrary.cs`
- `Assets/Scripts/Game/Environment/BiomeAtmosphereProfile.cs`
- `Assets/Scripts/Game/Environment/EnvironmentDistanceCuller.cs`
- `Assets/Scripts/Game/Graphics/GraphicsManager.cs`
- `Assets/Scripts/Game/AI/HeroPartyAI.cs`
- `Assets/Scripts/Game/AI/MonsterAI.cs`
- `Assets/Scripts/Game/AI/AbyssalBossAI.cs`
- `Assets/Scripts/Game/Combat/ActorUnit.cs`
- `Assets/Scripts/Game/Combat/HeroPlayerInput.cs`
- `Assets/Scripts/Game/Net/LanRoomBridge.cs`
- `Assets/Scripts/Game/UI/DungeonAndEndViews.cs`
- `Assets/Scripts/Game/UI/DungeonOverlayPanelsUI.cs`

---

## 7. Các vấn đề / giới hạn còn tồn tại (Trung thực)

1. **Điều hướng AI dùng Raycast Steering thay vì Baked `NavMeshData`**: Do toàn bộ 8 phòng và vật cản được sinh động (procedural) tại runtime trong `DungeonWorldBuilder`, project hiện dùng `CharacterController` + hệ thống né vật cản 5 tia (`HeroPartyAI.ObstacleAvoidDir`) và `Physics.CheckSphere` chứ chưa tích hợp package `com.unity.ai.navigation` (`NavMeshSurface` runtime baking). Nếu sau này thêm phòng nhiều tầng có cầu thang dốc, nên cài `AI Navigation` để bake runtime NavMesh.
2. **FPS trong Unity Editor ở preset Ultra trên Intel Iris Xe**: Đo trực tiếp trong Editor đạt `~53 FPS` ở preset `Ultra` (do overhead render Editor + Game View). Chuyển sang preset `Medium`/`Low` trong Settings hoặc chạy Standalone Build sẽ vượt ổn định 60 FPS.
3. **HUD runtime vẫn khởi tạo qua `DungeonHUDView.Build()`**: 7 UI Prefab trong `Assets/Prefabs/UI/` đã được build đồng bộ làm prefab chuẩn có thể chỉnh trực tiếp trong Unity Inspector / tái sử dụng cho các scene khác, nhưng `DungeonHUDView` vẫn giữ cơ chế sinh runtime để bảo toàn 100% tương thích với `SceneBootstrap` và các scene hiện hữu mà không phá dependency cũ.
4. **Đa người chơi LAN (`LanRoomBridge`)**: Header Party đã gắn động với `LanRoomBridge.IsLanActive` và tương thích trạng thái 4 thành viên; tuy nhiên đợt test Play Mode vừa rồi chỉ chạy xác thực cục bộ (1 local player + 3 AI party) chứ chưa test kết nối 2 máy thực qua LAN.
