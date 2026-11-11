# Chuyển UI Fish Jam Fever sang cấu trúc kiểu Bottle_Up

**Ngày:** 10/11/2026. **Tham khảo:** `Code_C_Sharp/Bottle_Up` (`Scripts/UI`, `Scripts/GameState`, `Scripts/Manager`, `Scripts/Core`).

**Người làm:**
- **B:** UI, chữ khen, âm thanh.
- **A:** gameplay.

Tài liệu này dùng chung cho cả hai người.

---

## 1. Cấu trúc UI của Bottle_Up

### Sơ đồ

```
1 scene duy nhất (SampleScene)
├─ GameManager   (Singleton) – máy trạng thái: Loading → MainMenu → Playing → Win / Lose
├─ UIManager     (Singleton) – nạp mọi prefab UI trong Resources/UI, mở/đóng theo KIỂU
│    └─ UIRoot   – cha chứa các Canvas được tạo ra
├─ AudioManager  (Singleton)
└─ LevelManager  (Singleton) – sinh màn chơi

Resources/UI/
  Canvas_Loading  (LoadingUI : UICanvas)  sortingOrder 100
  Canvas_MainMenu (MainMenu  : UICanvas)  10
  Canvas_GamePlay (GamePlay  : UICanvas)  0
  Canvas_Setting  (Setting   : UICanvas)  20
  Canvas_Win      (Win       : UICanvas)  20
  Canvas_Lose     (Lose      : UICanvas)  20
```

### Luồng hoạt động

- **`UICanvas`** là lớp cha của mọi màn UI. Mỗi màn có:
  - `SetUp()`: điền dữ liệu trước khi mở;
  - `Open()`;
  - `Close(time)`;
  - `CloseDirectly()`, với tuỳ chọn ẩn đi hoặc huỷ hẳn.
- **`UIManager`** khi khởi động dùng `Resources.LoadAll<UICanvas>("UI/")` để tạo bảng *kiểu → prefab*. Code mở màn bằng `UIManager.Instance.Open<Win>()`: chưa có thì tự tạo, có rồi thì bật lên. Ngoài ra còn `CloseAll()`, `IsOpened<T>()`, `GetUI<T>()`.
- **Mỗi trạng thái game tự mở và đóng UI của nó.** Ví dụ, `PlayingState.OnEnter` gọi `CloseAll()` rồi `Open<GamePlay>()`, còn `WinState.OnEnter` gọi `Open<Win>()`.
- **Nút UI gọi thẳng vào GameManager**, nối bằng OnClick trong Inspector. Ví dụ, `Win.NextLevelButton()` gọi `GameManager.Instance.GoToNextLevel()`. Mỗi nút có cờ `isClicked` để chống bấm hai lần.
- **Popup Setting** khi mở đặt `Time.timeScale = 0` và `GameInputGate.BottleInputEnabled = false`. Mọi đường đóng đều đi qua `CloseDirectly()`, nơi trả lại `timeScale = 1`.
- **`LoadingState`** tạo sẵn các màn UI mỗi khung một màn rồi ẩn đi, để lần mở đầu không bị khựng.
- **`ButtonClickSound`** gắn lên nút để có tiếng bấm, phát qua `AudioManager`.

## 2. Đánh giá: cấu trúc này có ổn cho Fish Jam Fever không?

**Kết luận: ổn, nên chuyển.** Game này nhỏ, khoảng 5–6 màn UI, chỉ có hai người làm, và cấu trúc này giải quyết đúng những chỗ project hiện tại đang vướng.

| Vấn đề hiện tại của Fish Jam | Bottle_Up giải quyết thế nào |
|---|---|
| Hai scene (`MainMenu`, `Game`) cộng thêm scene xem trước `GameplayUI`. `MainMenu.unity` hiện không có Canvas. Chuyển màn bằng `SceneManager.LoadScene`. | Một scene duy nhất. Chuyển màn bằng đổi trạng thái, không tải lại scene, không nháy màn hình. |
| Popup Settings nhúng vào **cả hai** Canvas. Thêm Win/Lose thì lại phải nhúng tiếp. | Mỗi popup là **một Canvas riêng**, mở ở đâu cũng được bằng `Open<Setting>()`. Thứ tự hiển thị lo bằng `sortingOrder`. |
| Gameplay phải nối UnityEvent (`onHome`, `onRetry`, `onPause`) trong Inspector, rất dễ quên. | UI gọi thẳng GameManager. Gameplay chỉ cần viết các State. |
| Chưa có chỗ chung để tạm dừng game hay chặn bấm xuyên popup. | Có `Time.timeScale` và `GameInputGate`. |
| Chưa có nơi điều phối Win/Lose. | `WinState` và `LoseState` mở popup tương ứng. |

### Điểm yếu của Bottle_Up, cần sửa khi chép sang

1. **`Close(float time)` dùng `Invoke`.** `Invoke` chạy theo thời gian có tính `timeScale`, nên khi `timeScale = 0` (Setting đang mở) thì popup sẽ không bao giờ đóng. → Đổi sang coroutine chạy theo `Time.unscaledDeltaTime`.
2. **`UICanvas.Awake()` là `private`.** Lớp con không viết được `Awake` của riêng mình (`LoadingUI` phải lách bằng `Start`). → Đổi thành `protected virtual void Awake()`.
3. **Chừa tai thỏ bằng cách đoán theo tỉ lệ màn** (`ratio > 2.1` thì trừ 100px) là không chính xác. → Dùng `SafeAreaFitter` sẵn có của Fish Jam, gắn cho nhóm nút HUD.
4. **Không có hiệu ứng mở/đóng.** → Đưa hiệu ứng *tối dần + khung nảy* của `SettingsPopup` hiện tại vào `UICanvas`, làm hàm `virtual` để mọi popup dùng chung.
5. **`Singleton.Instance` tự tạo object mới nếu không tìm thấy.** Quên đặt `UIManager` vào scene thì game vẫn chạy nhưng không có UI, rất khó tìm lỗi. → Khi phải tự tạo thì ghi `Debug.LogWarning`.
6. **`Resources.LoadAll` nạp mọi thứ trong `Resources/UI`.** Với 6 Canvas thì ổn. Nếu sau này có shop hay event nhiều ảnh thì cân nhắc chuyển sang Addressables (chưa cần).
7. **Render mode:**
   - Bottle_Up dùng toàn `ScreenSpaceOverlay`. Fish Jam có nền là UI (Stage 1080×1920), còn cá và bể có thể là sprite trong thế giới game, do A quyết định.
   - **Đề xuất:** `Canvas_Gameplay` dùng `ScreenSpaceCamera`. Nếu gameplay là sprite trong thế giới game thì tách nền ra một Canvas riêng nằm sau (`planeDistance` lớn) và đặt HUD ở một Canvas nằm trước.
   - Các popup (Setting, Win, Lose) dùng `Overlay` với `sortingOrder` cao.
   - Việc này **phải chốt với A** trước bước 4.

## 3. Cấu trúc đích cho Fish Jam Fever

```
Assets/_Game/
├─ Scenes/Main.unity                      ← scene duy nhất
├─ Resources/UI/                          ← mọi Canvas UI (prefab)
│   ├─ Canvas_MainMenu.prefab   (MainMenu : UICanvas)      sortingOrder 10
│   ├─ Canvas_GamePlay.prefab   (GamePlay : UICanvas)      0
│   ├─ Canvas_Setting.prefab    (Setting  : UICanvas)      30
│   ├─ Canvas_Win.prefab        (Win      : UICanvas)      20   (làm sau)
│   └─ Canvas_Lose.prefab       (Lose     : UICanvas)      20   (làm sau)
└─ Scripts/
    ├─ Core/      Singleton.cs, GameInputGate.cs
    ├─ Manager/   GameManager.cs                (A + B thống nhất)
    ├─ GameState/ IGameState.cs, MainMenuState.cs, PlayingState.cs, WinState.cs, LoseState.cs
    ├─ Audio/     AudioManager.cs, ButtonClickSound.cs         (B)
    └─ UI/
        ├─ UICanvas.cs, UIManager.cs                           (B)
        ├─ GameSettings.cs, SafeAreaFitter.cs                  (giữ nguyên)
        ├─ Canvas/ MainMenu.cs, GamePlay.cs, Setting.cs, (Win.cs, Lose.cs)
        ├─ Debug/  GameplayHUDDemo.cs                          (chỉ Editor / Development Build)
        └─ Effects/ (giữ nguyên toàn bộ)
```

**Namespace:**
- Giữ `FishJam.UI` cho UI.
- Dùng `FishJam` cho Core, Manager và GameState.

### Bảng đổi tên: script cũ → mới

| Hiện tại | Sau khi chuyển | Ghi chú |
|---|---|---|
| `MainMenuController` | `UI/Canvas/MainMenu : UICanvas` | Bỏ `SceneManager.LoadScene`. Nút Play gọi `GameManager.Instance.ChangeState(PlayingState.Instance)`. Nút bánh răng gọi `UIManager.Instance.Open<Setting>()`. |
| `GameplayHUD` | `UI/Canvas/GamePlay : UICanvas` | Giữ nguyên API: `SetLevel`, `SetFishCount`, `OnTankCompleted`, `ResetCombo`, `ShowPraise`, `SetWarning`. `SetUp()` gọi `ResetCombo()`, đặt level, tắt cảnh báo. Bỏ ô `settingsPopup`. |
| `SettingsPopup` | `UI/Canvas/Setting : UICanvas` | Thành **Canvas riêng**, không nhúng nữa. Bỏ UnityEvent `onHome/onRetry/onPause`. `SetUp()` tự chọn chế độ (xem dưới). Hiệu ứng mở/đóng chuyển lên `UICanvas`. |
| `GameplayHUDDemo` | giữ, chuyển vào `UI/Debug/` | Vẫn gắn trên `Canvas_GamePlay`. Thêm nút **Win / Lose** để thử popup khi đã có. |
| `GameSettings` | giữ nguyên | `AudioManager` đọc và nghe `GameSettings.Changed`. |
| `SafeAreaFitter` | giữ nguyên | Thay cho đoạn chừa tai thỏ trong `UICanvas.Awake` của Bottle_Up. |
| Builder `MainMenuBuilder`, `GameplayUIBuilder`, `SettingsPopupBuilder` | sửa đường lưu | Lưu prefab vào `Resources/UI/` và gắn lớp con `UICanvas` lên gốc. Không tạo scene nữa. `SettingsPopupBuilder` thôi nhúng popup vào hai Canvas. |
| Scene `MainMenu.unity`, `Game.unity`, `GameplayUI.unity` | thay bằng `Main.unity` | Giữ `GameplayUI.unity` làm scene thử UI riêng nếu muốn. |

### Popup Setting tự chọn chế độ

```csharp
public override void SetUp()
{
    base.SetUp();
    isClicked = false;
    // Đang chơi -> InGame (Home + Retry); còn lại -> Menu (Language + Support + Version)
    mode = GameManager.Instance.GetCurrentState() is PlayingState ? Mode.InGame : Mode.Menu;
    ApplyMode();
    if (mode == Mode.InGame) { Time.timeScale = 0f; GameInputGate.FishInputEnabled = false; }
}
public void HomeButton()  { if (Click()) { CloseDirectly(); GameManager.Instance.ChangeState(MainMenuState.Instance); } }
public void RetryButton() { if (Click()) { CloseDirectly(); GameManager.Instance.ReplayLevel(); } }
public override void CloseDirectly()
{
    Time.timeScale = 1f;                       // MỌI đường đóng đều trả lại thời gian
    GameInputGate.FishInputEnabled = (GameManager.Instance.GetCurrentState() is PlayingState);
    base.CloseDirectly();
}
```

**Lưu ý khi tạm dừng bằng `timeScale = 0`:**
- **Đứng yên theo game:** các hiệu ứng nền dùng `Time.time` (`UISway`, `UIScrollUV`, `UIFadePulse`, `UINod`, `UIBubbleSpawner`). Như vậy là đúng ý.
- **Vẫn chạy khi tạm dừng:** chữ khen, nút nảy và hiệu ứng popup đã dùng `unscaledDeltaTime`.
- **Đồng hồ combo** (`comboWindow` 5 giây) đang tính bằng `Time.time` nên cũng đứng lại khi tạm dừng. Đúng ý: tạm dừng không làm mất combo.

## 4. Các bước chuyển (làm theo thứ tự, mỗi bước chạy thử được)

| # | Việc | Ai | Kiểm tra |
|---|---|---|---|
| 1 | Tạo `Core/Singleton.cs` (có log khi tự tạo) và `Core/GameInputGate.cs` (`FishInputEnabled`, mặc định `false`). | B | Biên dịch không lỗi. |
| 2 | Tạo `UI/UICanvas.cs` (đã sửa 4 điểm yếu ở mục 2: `Awake` protected virtual, hiệu ứng mở/đóng virtual chạy unscaled, `Close(time)` bằng coroutine, không đoán tai thỏ) và `UI/UIManager.cs` (chép từ Bottle_Up). | B | — |
| 3 | Tạo `GameState/IGameState.cs` và `Manager/GameManager.cs` khung (`ChangeState`, `ReplayLevel`, `CurrentLevel` lưu PlayerPrefs `current_level`). Tạo `MainMenuState`, `PlayingState` rỗng, mỗi State chỉ mở/đóng UI. | B tạo khung, A viết phần gameplay trong `PlayingState` | Mở game: vào MainMenu. Bấm Play: vào GamePlay. |
| 4 | **Chốt với A:** cá/bể là UI hay sprite trong thế giới game, từ đó chọn render mode cho `Canvas_GamePlay` (mục 2, ý 7). | A + B | — |
| 5 | Chuyển `MainMenuController` → `MainMenu : UICanvas`. Sửa `MainMenuBuilder` để lưu vào `Resources/UI/Canvas_MainMenu.prefab`. Xoá `Canvas_MainMenu.prefab` cũ trong `Prefabs/UI` (hoặc chuyển vào đó, giữ GUID). | B | Menu hiện đúng, logo Spine chạy. |
| 6 | Chuyển `GameplayHUD` → `GamePlay : UICanvas`, sửa `GameplayUIBuilder`. **Lưu ý:** prefab `Canvas_Gameplay` đã chỉnh tay (chữ khen 8 mức, `PraiseAnchor`), nên **chuyển file bằng Unity** (kéo thả hoặc `AssetDatabase.MoveAsset`) để giữ GUID, rồi đổi script trên gốc. Đừng build lại từ đầu. | B | HUD, ô TEST, chữ khen, cảnh báo chạy như cũ. |
| 7 | Chuyển `SettingsPopup` → `Setting : UICanvas` thành `Canvas_Setting` riêng (`sortingOrder` 30). Gỡ bản nhúng khỏi hai Canvas cũ. Nút bánh răng gọi `UIManager.Instance.Open<Setting>()`. | B | Mở từ menu ra chế độ Menu, trong màn chơi ra InGame và game dừng. Home về menu, Retry chơi lại. |
| 8 | Tạo `Main.unity` gồm Camera, EventSystem, GameManager, UIManager (+ UIRoot), AudioManager, LevelManager (A). Đặt làm scene đầu trong Build Settings. Bỏ `MainMenu.unity` và `Game.unity` khỏi build. | B | Chạy từ `Main.unity` đủ vòng: Menu → Chơi → Setting → Home. |
| 9 | `AudioManager` + `ButtonClickSound` (chép từ Bottle_Up, rút gọn). Đọc `GameSettings.Music` và `GameSettings.Sound`. | B | Tắt Music trong Setting thì nhạc tắt ngay, mở lại game vẫn tắt. |
| 10 | `WinState` / `LoseState`, `Canvas_Win` / `Canvas_Lose` (làm sau, theo mockup Win/Lose). | B (UI), A (điều kiện thắng/thua) | — |

## 5. Hợp đồng giữa gameplay (A) và UI (B)

**Gameplay gọi vào UI.** Chỉ được gọi qua `UIManager`, không tìm theo tên object:

```csharp
var hud = UIManager.Instance.GetUI<GamePlay>();
hud.SetLevel(level);
hud.SetFishCount(collected, total);
hud.OnTankCompleted();      // mỗi lần 1 bể đầy -> combo + chữ khen
hud.SetWarning(trayCount == trayMax - 1);
```

**Đổi màn.** Chỉ GameManager được đổi trạng thái:

```csharp
GameManager.Instance.ChangeState(WinState.Instance);   // A gọi khi hết cá
GameManager.Instance.ChangeState(LoseState.Instance);  // A gọi khi khay đầy, hết nước đi
GameManager.Instance.ReplayLevel();                    // UI gọi (Retry)
```

**Chặn chạm.** Code chọn cá của A phải kiểm tra `GameInputGate.FishInputEnabled` trước khi xử lý. Các State và popup bật/tắt công tắc này.

**Quy ước chung:**
- Nút UI nối OnClick trong Inspector tới hàm `public void XxxButton()` của lớp Canvas.
- Mỗi popup có cờ `isClicked` chống bấm hai lần.
- Mọi hiệu ứng UI cần chạy khi đang tạm dừng phải dùng `unscaledDeltaTime`.
