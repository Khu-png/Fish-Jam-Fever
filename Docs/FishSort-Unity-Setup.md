# Fish Sort – Hướng dẫn dựng scene trong Unity

Code nằm ở `Assets/_Game/Scripts/FishSort/`. Tool editor nằm ở `Assets/_Game/Editor/FishSort/`, sprite tạm nằm ở `Assets/_Game/Art/Placeholder/`.
Theo rule của team, **mọi field trên Inspector đều kéo thả bằng tay**. Code không tự gán gì.

Đơn vị: 1 unit = 100px của bản preview HTML. Màn hình thiết kế rộng 5.4 unit.

---

## 0. Chuẩn bị (làm 1 lần)

1. **TextMesh Pro:** Window → TextMeshPro → Import TMP Essential Resources.
2. **Sprite tạm:** khi Unity import thư mục `Art/Placeholder`, `PlaceholderArtImporter` tự đặt kiểu Sprite và Pixels Per Unit cho từng file. Nếu import trước khi có script này thì chuột phải vào thư mục → Reimport.
3. **Level mẫu:** menu **Tools → FishSort → Generate Sample Levels** sẽ tạo `Level_01` đến `Level_04` trong `Assets/_Game/ScriptableObject/Levels`. Level 1 là màn tutorial. Muốn kiểm tra một level thì chuột phải vào asset → Validate.
4. Tạo các asset sau (chuột phải → Create → FishSort):
   - **Config** → `FishSortConfig`, đặt trong `ScriptableObject/`.
   - **Fish Catalog** → `FishCatalog`. Kéo `fish_00` … `fish_11` vào **đúng thứ tự** (index chính là id loại cá).
5. **Physics Material 2D** `BubblePhysics`: Friction 0.1, Bounciness 0.1.

---

## 1. Prefab

### Fish (`Prefabs/Fish.prefab`)
| Object | Component | Thiết lập |
|---|---|---|
| **Fish** (root) | `Fish`, `CircleCollider2D` | Pool Type = **Fish**; collider **Is Trigger**, Radius 0.45 |
| └ Body | SpriteRenderer | `fish_00`, Order 10, Mask Interaction **Visible Inside Mask** |
| └ Glow | SpriteRenderer | `glow`, Order 9, tắt Enabled, Mask **Visible Inside Mask** |
| └ Trail | ParticleSystem | Simulation Space **World**, Start Speed 0, Lifetime 0.4, Size 0.06–0.12, Emission: Rate over Time 0 / **Rate over Distance 25**, Color over Lifetime (alpha 1 → 0), Renderer Order 59 |

Kéo vào script `Fish`: Body Renderer, Glow Renderer, Hit Collider, Flight Trail, Config.
> Body để scale 1, vì sprite cá rộng đúng 1 unit. Kích thước cá do code tính theo bong bóng.

### Bubble (`Prefabs/Bubble.prefab`)
| Object | Component | Thiết lập |
|---|---|---|
| **Bubble** (root) | `Bubble`, `Rigidbody2D`, `CircleCollider2D` | Pool Type = **Bubble**; Rigidbody: Dynamic, Gravity Scale 1, Linear Damping 0.5, **Freeze Rotation Z**, Interpolate; Collider Radius 0.47, Material `BubblePhysics` |
| └ Shell | SpriteRenderer | `bubble`, Order 5, Mask **Visible Inside Mask** |
| └ Ice | `IceCover` + SpriteRenderer | `ice`, Order 30, Mask **Visible Inside Mask**. **Giữ object Active trong prefab** |
| &nbsp;&nbsp;└ Number | SpriteRenderer | `ice_num_3`, Order 31, Mask **Visible Inside Mask** |

Kéo vào `Bubble`: Body, Shell Renderer, Ice, Config.
Kéo vào `IceCover`: Ice Renderer, Number Renderer, Number Transform, Number Sprites = [`ice_num_1`, `ice_num_2`, `ice_num_3`], Config.

### Particle (3 prefab nhỏ)
Mỗi prefab là ParticleSystem bắn 1 lần (Burst 8–12, không Loop, Stop Action = Disable), Renderer Order 65:
`FX_BubblePop`, `FX_TankBurst`, `FX_FishLand`.

---

## 2. Scene `Game.unity`

> Trên **Main Camera**, xóa component `FishTankPrototype` (code cũ).

```
Main Camera            Orthographic, Position (0,0,-10), Clear Flags Solid #03163A
                       + CameraFit (Target Camera = Main Camera)
Background             SpriteRenderer `background`, Order -100, Position (0,0)
Manager
  GameManager          GameManager
  LevelManager         LevelManager → Levels = Level_01..Level_04 (đúng thứ tự)
  SoundManager         SoundManager → Fx Aus: theo thứ tự enum FxID (thiếu clip cũng không sao)
  UIManager            UIManager → Ui Resources = 5 prefab UI; Canvas Parent TF = Canvas
  PoolManager          PoolControler
                         Pool With Root: [Fish prefab, 130, root = FishPool], [Bubble prefab, 40, root = BubblePool]
                         Particle: [BubblePop, FX_BubblePop, 10], [TankBurst, FX_TankBurst, 4], [FishLand, FX_FishLand, 6]
  FishPool / BubblePool  (Empty)
FlightRoot             (Empty, Position 0,0,0)
TopHUD                 + ScreenEdgeAnchor (Camera, Edge Top, Offset 0)
  Shelf                SpriteRenderer `shelf`, Order 1, Local (0, -1.85)
  TankSlot0            TankSlotView, Local (-1.89, -1.28)
    TankRoot           (Empty)
      Glass            SpriteRenderer `tank_open`, Order 2
      Flash            SpriteRenderer `square`, Order 4, Scale (1.08, 0.96), Color trắng alpha 0
      IconPoint0/1/2   (Empty) Local (-0.30,-0.14) / (0,-0.14) / (0.30,-0.14)
    Badge              SpriteRenderer `badge`, Order 5, Local (0, -0.96)
      BadgeFish        SpriteRenderer, Order 6, Local (-0.2, 0), Scale 0.38
      BadgeCount       TextMeshPro, Local (0.2, 0), Font Size 3, màu #10224F, Order 7
  TankSlot1            giống TankSlot0, Local (-0.63, -1.28)
  LockedTank2          SpriteRenderer `tank_locked`, Order 2, Local (0.63, -1.28)
  LockedTank3          SpriteRenderer `tank_locked`, Order 2, Local (1.89, -1.28)
  Counter              SpriteRenderer `counter_bg`, Order 1, Local (-2.14, -2.92)
    FishesLabel        TextMeshPro "FISHES", Font Size 1.6, Local (0, 0.14), Order 2
    ProgressText       TextMeshPro "0/0", Font Size 2.6, Local (0, -0.1), Order 2
  Tray                 WaitingTray
    Slot0..Slot4       SpriteRenderer `tray_slot`, Order 1, Local x = -1.26 / -0.42 / 0.42 / 1.26 / 2.10, y = -2.92
  Combo                ComboCounter
    ComboText          TextMeshPro, Font Size 5, Local (1.26, -2.24), Order 70, căn giữa
  FieldTop             (Empty) Local (0, -3.40)
  FieldMask            SpriteMask `square`, Local (0, -9.40), Scale (5.6, 12)
BottomAnchor           + ScreenEdgeAnchor (Camera, Edge Bottom, Offset 0)
  Floor                BoxCollider2D Size (6, 1), Local (0, -0.5)
  FloorPoint           (Empty) Local (0, 0.04)
LeftWall               BoxCollider2D Size (1, 40), Position (-3.2, 0)
RightWall              BoxCollider2D Size (1, 40), Position (3.2, 0)
Field                  BubbleField (Config, Catalog, Bubble Root = Field, Floor Point, Field Top Point = FieldTop)
Gameplay               GameplayController + FishPicker
Canvas                 Screen Space - Camera (Main Camera), Plane Distance 5, Sorting Order 100
                       Canvas Scaler: Scale With Screen Size 1080x1920, Match = 0 (Width)
EventSystem            với **Input System UI Input Module**
```

Kéo vào `TankSlotView`: Tank Root, Flash Renderer, Icon Points (3), Badge Root = Badge, Badge Transform = Badge, Badge Fish Renderer, Badge Count Text, Config.
Kéo vào `TankShelf`: gắn lên **TopHUD**, Slots = [TankSlot0, TankSlot1], Config, Catalog.
Kéo vào `WaitingTray`: Slot Points = Slot0..Slot4, Config.
Kéo vào `ComboCounter`: Combo Text, Combo Transform = ComboText, Config.
Kéo vào `GameplayController`: Config, Game Camera, Field, Shelf (TopHUD), Tray, Combo, Flight Root, Progress Text.
Kéo vào `FishPicker`: Game Camera, Field, Shelf, Config.

---

## 3. Prefab UI (`Prefabs/UI/`)
Mỗi prefab là RectTransform full-stretch, nằm dưới Canvas, có Animator tùy ý. Ô **Popups** để mảng rỗng.
**Bỏ tick Raycast Target** ở mọi Image/Text không bấm được.

| Prefab | Script | Nội dung | Kéo vào script |
|---|---|---|---|
| `MainMenu` | UIMainMenu | Nền Image #0B4A8F full, tiêu đề TMP, nút Play (Image `ui_round` Sliced, màu #8B4DFF) có TMP con, nút Settings | Play Button, Play Label, Settings Button |
| `Gameplay` | UIGameplay | Level badge (Image + TMP) góc trên trái, nút Settings góc trên phải. **Warning** (CanvasGroup, tắt Blocks Raycasts): Image `ui_vignette` Sliced full-stretch + banner đỏ "Only one slot left!". **TutorialArea** full-stretch: Hand (Image `ui_hand`). **Tip** (Image `ui_round` + TMP) | Level Label, Settings Button, Warning Root/Group, Tutorial Area, Tutorial Hand, Tip Root, Tip Text |
| `Win` | UIWin | Nền tối full (Raycast bật để chặn chạm), TMP "VICTORY!", Image `fish_00`, nút Next/Home, ParticleSystem Confetti (Renderer Order 110) | Next, Home, Confetti |
| `Lose` | UILose | Nền tối, TMP "YOU LOSE", Image cá, TMP progress, Image thanh tiến độ (**Filled, Horizontal**), nút Retry/Home | Progress Label, Progress Fill, Retry, Home |
| `Settings` | UISettings | Panel, 3 Toggle (Âm thanh / Dịch điểm dò / Viền gợi ý), nhóm InGameButtons (Home, Restart), nút Close | 3 Toggle, In Game Buttons Root, Home, Restart, Close |

---

## 4. Kiểm tra nhanh
- Bấm Play → Main menu hiện "Level 1" → bấm vào thì hiện tutorial 3 bong bóng.
- Đặt Game view lần lượt 1080x1920 và 720x1600: kệ bể bám mép trên, bong bóng nằm sát đáy, màn cao hơn thì lộ thêm bong bóng.
- Settings → bật "Viền gợi ý" → ấn giữ cá: cá khớp bể sáng xanh, cá sẽ vào khay sáng cam.
- Kiểm tra hiệu năng: Window → Analysis → Frame Debugger; Project Settings → Quality → Medium.
