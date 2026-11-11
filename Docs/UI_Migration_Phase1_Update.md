# Cập nhật: đã chuyển UI đợt 1 (Main Menu, GamePlay, Setting)

**Ngày:** 10/11/2026. **Kế hoạch gốc:** `Docs/UI_Migration_Phase1.md`.

**Kết quả:**
- Game chạy từ **một scene `Main.unity`**.
- Mở và đóng UI qua `UIManager`. Chuyển màn qua `GameManager` (máy trạng thái MainMenu ⇄ Playing).
- Đã chạy thử đủ 8 bước trong bảng kiểm tra, Console không có lỗi hay cảnh báo.

---

## 1. File MỚI

| File | Nội dung |
|---|---|
| `Scripts/Core/Singleton.cs` | `Singleton<T>` (namespace `FishJam`). Tự tạo object nếu thiếu và **ghi cảnh báo**. Thêm `Singleton<T>.Exists` để kiểm tra mà không tự tạo, dùng cho scene thử UI. |
| `Scripts/UI/UICanvas.cs` | Lớp cha mọi màn UI. Có `SetUp()`, `Open()`, `Close(time)` (thời gian thật), `CloseAnimated()`, `CloseDirectly()`. Ô `dim` và `panel` (public) cho hiệu ứng mở/đóng dùng chung: nền tối dần, khung nảy 0.6 → 1.08 → 1, chạy cả khi `timeScale = 0`. `Awake()` để `protected virtual` và tự gán `Camera.main` cho Canvas ScreenSpaceCamera. |
| `Scripts/UI/UIManager.cs` | Chép từ Bottle_Up: `Resources.LoadAll<UICanvas>("UI/")`, `Open<T>()`, `GetUI<T>()`, `Close<T>(t)`, `CloseDirectly<T>()`, `IsLoaded<T>()`, `IsOpened<T>()`, `CloseAll()`. Ô `parent` trỏ tới `UIRoot`. |
| `Scripts/GameState/IGameState.cs` | `OnEnter`, `OnExecute`, `OnExit`. |
| `Scripts/GameState/MainMenuState.cs` | Enter: `CloseAll()` rồi `Open<MainMenu>()`. Exit: đóng MainMenu. |
| `Scripts/GameState/PlayingState.cs` | Enter: `CloseAll()` rồi `Open<GamePlay>()`. Exit: đóng GamePlay. Có chú thích `(gameplay)` đánh dấu chỗ sinh màn và kiểm tra thắng/thua. |
| `Scripts/Manager/GameManager.cs` | `ChangeState()`, `ReplayLevel()`, `GetCurrentState()`, `CurrentLevel` (PlayerPrefs `current_level`, bắt đầu từ 1). `Start()` vào thẳng `MainMenuState` và đặt `targetFrameRate = 60`. |
| `Editor/MigrateUIPhase1.cs` | Menu **FishJam > Migrate UI (Phase 1)**, chạy một lần (đã chạy). Có thể chạy lại an toàn: bỏ qua những gì đã chuyển. |
| `Scenes/Main.unity` | Main Camera, EventSystem (Input System), GameManager, UIManager kèm con UIRoot. |

## 2. Script ĐỔI TÊN và CHUYỂN CHỖ (chi tiết)

**Cách chuyển:** mỗi file `.cs` được chuyển **kèm file `.meta`**, nên GUID không đổi. Unity nhận ra script trên prefab theo GUID, còn dữ liệu đã gán trong Inspector được giữ theo **tên trường**. Vì vậy:
- tên lớp đổi theo tên file mới;
- prefab không bị "Missing script";
- mọi trường giữ nguyên tên thì giữ nguyên dữ liệu đã gán.

Tất cả vẫn ở namespace `FishJam.UI`.

Ký hiệu dùng trong các bảng: **➕ thêm**, **➖ bỏ**, **✏️ sửa**, **＝ giữ nguyên**.

---

### 2.1 `MainMenuController.cs` → `UI/Canvas/MainMenu.cs`

| | Trước (`MainMenuController : MonoBehaviour`) | Sau (`MainMenu : UICanvas`) |
|---|---|---|
| ✏️ Lớp cha | `MonoBehaviour` | `UICanvas` (để `UIManager` nạp, mở và đóng) |
| ＝ Trường | `playButton`, `levelLabel`, `settingsButton` | Giữ nguyên tên nên giữ nguyên tham chiếu trong prefab |
| ➖ Trường | `settingsPopup` (GameObject, rồi SettingsPopup), `gameplayScene = "Game"` | Bỏ. Popup mở bằng `UIManager`, không còn chuyển scene |
| ➖ Hằng | `LevelKey = "current_level"` | Chuyển sang `GameManager.LevelKey` (cùng giá trị nên level cũ vẫn đọc được) |
| ✏️ Khởi tạo | `Start()`: đọc PlayerPrefs, ghi "Level n", nối nút | `Awake()` (override): chỉ nối nút. `SetUp()`: ghi "Level n" **mỗi lần mở** menu, đọc từ `GameManager.Instance.CurrentLevel` |
| ✏️ Nút Level | `SceneManager.LoadScene("Game")` | `PlayButton()` → `GameManager.Instance.ChangeState(PlayingState.Instance)` |
| ✏️ Nút ⚙ | `settingsPopup.Open()` (bản nhúng trong Canvas) | `SettingButton()` → `UIManager.Instance.Open<Setting>()` |
| ➖ using | `UnityEngine.SceneManagement` | Bỏ |

```csharp
// Trước
void Start() {
    int level = PlayerPrefs.GetInt(LevelKey, 1);
    if (levelLabel) levelLabel.text = "Level " + level;
    if (playButton) playButton.onClick.AddListener(() => SceneManager.LoadScene(gameplayScene));
    if (settingsButton) settingsButton.onClick.AddListener(() => { if (settingsPopup) settingsPopup.Open(); });
}
// Sau
protected override void Awake() { base.Awake(); playButton.onClick.AddListener(PlayButton); settingsButton.onClick.AddListener(SettingButton); }
public override void SetUp() { base.SetUp(); levelLabel.text = "Level " + GameManager.Instance.CurrentLevel; }
public void PlayButton()    { GameManager.Instance.ChangeState(PlayingState.Instance); }
public void SettingButton() { UIManager.Instance.Open<Setting>(); }
```

**Ảnh hưởng:** về lại menu (Home, hoặc khi đủ cá) thì "Level n" tự cập nhật. Trước đây chỉ cập nhật một lần lúc tải scene.

---

### 2.2 `GameplayHUD.cs` → `UI/Canvas/GamePlay.cs`

| | Trước (`GameplayHUD : MonoBehaviour`) | Sau (`GamePlay : UICanvas`) |
|---|---|---|
| ✏️ Lớp cha | `MonoBehaviour` | `UICanvas` |
| ＝ Trường | `levelLabel`, `countLabel`, `countBox`, `settingsButton`, `comboWindow` (5s), `praises[8]`, `spark`, `warningFrame`, `warningText`, `gameplayLayer` | Giữ nguyên, nên giữ 8 chữ khen và `PraiseAnchor` đã chỉnh tay |
| ➖ Trường | `settingsPopup` (popup nhúng) | Bỏ |
| ✏️ `Awake()` | `void Awake()`: gán `Camera.main` cho Canvas, ẩn chữ khen, loé sáng, cảnh báo, nối ⚙ → `settingsPopup.Open` | `protected override void Awake()`: `base.Awake()` (việc gán camera chuyển lên `UICanvas`), `ResetVisuals()`, nối ⚙ → `SettingButton` |
| ➕ `SetUp()` | — | Gọi mỗi lần HUD mở (vào màn hoặc Retry): `SetLevel(GameManager.CurrentLevel)` (chỉ khi có GameManager), `ResetCombo()`, `ResetVisuals()` |
| ➕ `SettingButton()` | — | `UIManager.Instance.Open<Setting>()` |
| ➕ `ResetVisuals()` (private) | — | Dừng mọi coroutine (chữ khen, nảy bộ đếm, cảnh báo), `warning = false`, ẩn chữ khen và loé sáng, `warningFrame.alpha = 0`, thu `warningText` về 0, `countBox` về scale 1 |
| ＝ API gameplay | `SetLevel`, `SetFishCount`, `OnTankCompleted`, `ResetCombo`, `ShowPraise`, `SetWarning`, `Combo` | **Không đổi tên, tham số hay hành vi** |
| ＝ Hiệu ứng | `Bump`, `Praise`, `Warn` (chạy theo `unscaledDeltaTime`), nhấp nháy cảnh báo trong `Update` | Không đổi |
| ✏️ Chú thích lớp | — | Thêm hướng dẫn lấy HUD: `UIManager.Instance.GetUI<GamePlay>()` |

**Ảnh hưởng:**
- **Retry:** trước đây không có gì đặt lại HUD. Giờ `PlayingState` gọi `Open<GamePlay>()`, kéo theo `SetUp()`, nên combo, chữ khen và cảnh báo đều sạch.
- **Lấy HUD:** gameplay không giữ tham chiếu kéo thả trong Inspector nữa, mà lấy qua `UIManager.Instance.GetUI<GamePlay>()`.

---

### 2.3 `SettingsPopup.cs` → `UI/Canvas/Setting.cs`

| | Trước (`SettingsPopup : MonoBehaviour`, nhúng trong 2 Canvas) | Sau (`Setting : UICanvas`, Canvas riêng `Canvas_Setting`) |
|---|---|---|
| ✏️ Lớp cha | `MonoBehaviour` | `UICanvas` |
| ✏️ Chọn chế độ | Đặt sẵn trong Inspector của từng bản nhúng (Menu hoặc InGame) | `SetUp()` **tự chọn**: `GameManager` đang `PlayingState` thì `InGame`, còn lại `Menu`. Trường `mode` vẫn còn để xem trong Inspector |
| ✏️ Tạm dừng | Phát `onPause(true/false)` để gameplay tự xử lý | Tự đặt `Time.timeScale = 0` khi mở ở chế độ InGame |
| ✏️ Đóng | `Close()` có hiệu ứng, rồi `SetActive(false)`; `CloseNow()` đóng ngay | `CloseButton()` → `CloseAnimated()` của lớp cha. `CloseDirectly()` (override) **luôn** đặt `Time.timeScale = 1`, `IsShowing = false`, rồi gọi `base.CloseDirectly()` |
| ✏️ Nút Home | `CloseNow()` rồi `onHome.Invoke()` (gameplay tự nối) | `HomeButton()` → `CloseDirectly()` rồi `GameManager.ChangeState(MainMenuState)` |
| ✏️ Nút Retry | `CloseNow()` rồi `onRetry.Invoke()` | `RetryButton()` → `CloseDirectly()` rồi `GameManager.ReplayLevel()` |
| ➕ Chống bấm hai lần | — | Cờ `isClicked` (đặt lại trong `SetUp()`), dùng cho ✕, Home, Retry |
| ➕ `IsShowing` (static) | — | `true` khi đang mở. Ô TEST đọc để tự ẩn mà không cần gọi `UIManager` |
| ✏️ Mở | `Open()` riêng: `SetActive`, `SetAsLastSibling`, `Wire()`, `ApplyMode()`, `Refresh()`, phát `onPause`, chạy hiệu ứng | `Open()` override: `IsShowing = true`, `SetAsLastSibling()`, `base.Open()` (hiệu ứng của lớp cha). Chọn chế độ và làm mới nút nằm trong `SetUp()` |
| ✏️ Nối nút | `Wire()` có cờ `wired`, gọi trong `Awake` và `Open` | Nối thẳng trong `Awake()` (override), bỏ `Wire()` và cờ `wired` |
| ✏️ Hiệu ứng mở/đóng | `Anim()`, `Back()`, `Play()` và trường `anim` nằm trong lớp | **Chuyển lên `UICanvas`**, cùng thông số: tối dần 0.28s, khung 0.6 → 1.08 → 1, đóng 0.16s về 0.7 |
| ✏️ Trường `dim`, `panel` | Khai báo trong `SettingsPopup` | Khai báo ở `UICanvas` (public, cùng tên), nên dữ liệu đã gán **vẫn giữ** |
| ➖ Sự kiện | `onHome`, `onRetry`, `onPause(bool)` | Bỏ: UI gọi thẳng `GameManager` |
| ＝ Sự kiện | `onLanguage`, `onSupport` | Giữ, chưa có chức năng |
| ＝ Bật/tắt | Lớp con `Toggle3` (nền xanh ↔ đỏ, gạch chéo), `Refresh()` qua `GameSettings.Changed`, nút nảy `Tap/Punch` | Không đổi |
| ＝ Khác | `ApplyMode()`, `OnValidate()`, `versionLabel` = "Version " + `Application.version` | Không đổi |
| ✏️ `OnDisable()` | Huỷ đăng ký `GameSettings.Changed` | Thêm `IsShowing = false` (phòng trường hợp bị tắt từ bên ngoài, ví dụ `CloseAll()`) |

```csharp
// Sau – phần cốt lõi
public override void SetUp() {
    base.SetUp(); isClicked = false;
    mode = GameManager.Instance.GetCurrentState() is PlayingState ? Mode.InGame : Mode.Menu;
    ApplyMode(); Refresh();
    if (mode == Mode.InGame) Time.timeScale = 0f;
}
public override void CloseDirectly() { Time.timeScale = 1f; IsShowing = false; base.CloseDirectly(); }
```

**Ảnh hưởng:**
- Gameplay **không cần nối sự kiện nào** cho Setting.
- Khi tạm dừng (`timeScale = 0`):
  - **đứng yên:** hiệu ứng nền chạy theo `Time.time` (rong, san hô, ánh sáng, bong bóng), và cả đồng hồ combo;
  - **vẫn chạy:** popup, chữ khen, nút nảy, vì chạy theo thời gian thật.
- `UIManager.CloseAll()` gọi `CloseDirectly()`, nên đổi màn khi popup đang mở cũng không làm game đứng hình.

---

### 2.4 `GameplayHUDDemo.cs` → `UI/Debug/GameplayHUDDemo.cs`

| | Trước | Sau |
|---|---|---|
| ✏️ Yêu cầu | `[RequireComponent(typeof(GameplayHUD))]` | `[RequireComponent(typeof(GamePlay))]` |
| ＝ Trường | `showPanel`, `autoPraise` (mặc định tắt), `praiseEvery` (2.6s) | Giữ |
| ➕ Trường | — | `total = 30` (tổng cá giả lập) |
| ✏️ Khởi tạo | `Start()`: lấy HUD, tắt nếu là bản build thật | `Awake()`: như cũ. ➕ `OnEnable()`: **mỗi lần HUD mở** (vào màn hoặc Retry) đặt `fish = 0`, `ending = false`, tắt cảnh báo, ghi `0/30` |
| ✏️ Đếm cá | `AddFish()`: tăng 3, quay vòng 45 ↔ 120 | `AddFish(n)`: tăng n, tối đa `total`. **Đủ thì `ending = true` và chạy `EndLevel()`** |
| ➕ `TankFull()` | — | `OnTankCompleted()` (combo, chữ khen) + `AddFish(3)` |
| ➕ `EndLevel()` | — | Chờ **1 giây thật** (`WaitForSecondsRealtime`), rồi: có GameManager thì `ChangeState(MainMenuState)` (sau này: `WinState`); scene thử UI không có GameManager thì đặt lại về 0 |
| ✏️ Tự động | `Update`: `OnTankCompleted()` + `AddFish()` | `Update`: `TankFull()`, dừng khi `ending` |
| ✏️ Ẩn ô TEST | Khi `hud.settingsPopup` đang hiện | Khi `Setting.IsShowing` |
| ✏️ Nút | Tự động xong bể · **Xong 1 bể (combo n)** · Chữ khen kế tiếp · Sắp thua (đỏ) | Tự động xong bể · **+3 cá (combo n)** · **Đủ cá** · Chữ khen kế tiếp · Sắp thua (đỏ) |
| ➕ Giao diện ô TEST | Tiêu đề "TEST" | Tiêu đề "TEST  n/30". **Khoá mọi nút** khi đang kết thúc màn (`GUI.enabled = !ending`). Ô cao hơn (6.2 dòng), đặt ở 50% chiều cao màn |
| ＝ | Chỉ hiện trong Editor hoặc Development Build | Không đổi |

---

### 2.5 Tóm tắt "ai gọi ai" sau khi đổi

```
MainMenu.PlayButton ──► GameManager.ChangeState(PlayingState) ──► UIManager.CloseAll + Open<GamePlay> ──► GamePlay.SetUp
MainMenu/GamePlay.SettingButton ──► UIManager.Open<Setting> ──► Setting.SetUp (chọn mode, timeScale=0 nếu InGame) ──► Open (hiệu ứng)
Setting.HomeButton  ──► CloseDirectly (timeScale=1) ──► GameManager.ChangeState(MainMenuState) ──► Open<MainMenu> ──► MainMenu.SetUp
Setting.RetryButton ──► CloseDirectly ──► GameManager.ReplayLevel ──► PlayingState Exit+Enter ──► GamePlay.SetUp (reset)
GameplayHUDDemo đủ cá ──► 1s ──► GameManager.ChangeState(MainMenuState)
```

**Không đổi:** `GameSettings.cs`, `SafeAreaFitter.cs`, `Effects/*`.

## 3. Builder (Editor) đã sửa

| File | Thay đổi |
|---|---|
| `Editor/MainMenuBuilder.cs` | Gắn `MainMenu` (thay `MainMenuController`). **Lưu prefab** `Resources/UI/Canvas_MainMenu.prefab` (`sortingOrder` 10), không lưu scene, không thêm vào Build Settings. Hỏi xác nhận trước khi ghi đè. |
| `Editor/GameplayUIBuilder.cs` | Gắn `GamePlay` (thay `GameplayHUD`). Lưu vào `Resources/UI/Canvas_GamePlay.prefab`, vẫn lưu scene thử `GameplayUI.unity`. Hỏi xác nhận trước khi ghi đè. |
| `Editor/SettingsPopupBuilder.cs` | Tạo `Resources/UI/Canvas_Setting.prefab`: gốc có Canvas (Overlay, `sortingOrder` 30), CanvasScaler 1080×1920 (match 0.5), GraphicRaycaster và `Setting`. **Bỏ** hàm `Attach` (không nhúng vào hai Canvas nữa). Hỏi xác nhận trước khi ghi đè. |

> ⚠ Chạy lại builder sẽ **ghi đè** prefab và mất các chỉnh tay (chữ khen 8 mức, `PraiseAnchor`…). Chỉ dùng khi muốn dựng lại từ đầu.

## 4. Prefab và Scene

| Trước | Sau |
|---|---|
| `Prefabs/UI/Canvas_MainMenu.prefab` | `Resources/UI/Canvas_MainMenu.prefab`: `sortingOrder` 10, gỡ bản nhúng `SettingsPopup`. |
| `Prefabs/UI/Canvas_Gameplay.prefab` | `Resources/UI/Canvas_GamePlay.prefab` (đổi tên, chữ **P** hoa): `sortingOrder` 0, gỡ bản nhúng `SettingsPopup`. |
| `Prefabs/UI/SettingsPopup.prefab` | `Resources/UI/Canvas_Setting.prefab`: thêm Canvas, CanvasScaler và GraphicRaycaster lên gốc, Overlay, `sortingOrder` 30. |
| `Scenes/MainMenu.unity`, `Scenes/Game.unity` | Chuyển sang `Scenes/_Old/` (giữ để tham khảo, không xoá). |
| — | **`Scenes/Main.unity`** (mới). |
| Build Settings | Chỉ còn `Scenes/Main.unity` (vị trí 0). |

- Thư mục `Prefabs/UI/` hiện trống.
- `Scenes/GameplayUI.unity` vẫn giữ làm scene thử HUD. Bản Canvas trong đó tự trỏ sang prefab mới, vì GUID không đổi.

## 5. Kết quả chạy thử (scene `Main`)

| # | Thao tác | Kết quả |
|---|---|---|
| 1 | Play | `MainMenuState`, Main Menu hiện đủ (logo, cá, cây, rương, Level 1). ✔ |
| 2 | Bấm Level 1 (bấm hai lần) | `PlayingState`, HUD hiện `0/30`, menu đã đóng; lần bấm thứ hai bị bỏ qua. ✔ |
| 3 | ⚙ trên HUD | Setting chế độ **InGame** (Home/Retry), `timeScale = 0`. ✔ |
| 4 | ✕ | Đóng, `timeScale = 1`. ✔ |
| 5 | "+3 cá" ×3 | Combo 3, bộ đếm `9/30`, hiện chữ "Cool!". ✔ |
| 6 | ⚙ → Retry (bấm hai lần) | Vẫn `PlayingState`, bộ đếm `0/30`, combo 0, `timeScale = 1`. ✔ |
| 7 | ⚙ → Home (bấm hai lần) | `MainMenuState`, menu mở, HUD đóng, `timeScale = 1`. ✔ |
| 8 | ⚙ trên menu | Setting chế độ **Menu** (Language/Support/"Version 0.1.0"). Tắt Music, mở lại vẫn tắt. ✔ |
| 9 | "Đủ cá" | Bộ đếm `30/30`, khoá nút, về Main Menu. ✔ |

*Ghi chú:* khi Unity Editor không được focus, khung hình không chạy, nên lúc test bằng công cụ, các hiệu ứng (khung nảy, chờ 1 giây) được cho chạy xong bằng tay. Khi bạn tự bấm Play trong Editor thì mọi thứ chạy bình thường.

## 6. Lưu ý

1. **Render mode:** `Canvas_GamePlay` và `Canvas_MainMenu` đang đặt ScreenSpaceCamera nhưng prefab không gán camera, nên Unity coi như **Overlay** (`UICanvas.Awake` chỉ gán camera khi thật sự là ScreenSpaceCamera). Hiện tại hiển thị đúng. Khi bạn cùng nhóm làm cá và bể thì chốt lại (xem `UI_Architecture_Migration.md`, mục 2 ý 7).
2. **Nối nút:** tất cả nút được nối trong `Awake()` bằng `AddListener`. **Đừng** nối thêm OnClick trong Inspector, nếu không hàm sẽ chạy hai lần.
3. **Gọi HUD từ gameplay:**
   ```csharp
   var hud = UIManager.Instance.GetUI<GamePlay>();
   hud.SetFishCount(collected, total);
   hud.OnTankCompleted();
   hud.SetWarning(true);
   ```
4. **Thư mục `Resources`:** `Assets/_Game/Resources/FishJamFever/` (asset game gốc) cũng nằm trong một thư mục `Resources`, nên **toàn bộ sẽ bị đóng vào bản build**. Điều này có từ trước, không do đợt này gây ra. `UIManager` chỉ nạp `Resources/UI`. Trước khi build thật nên chuyển những gì đang dùng ra khỏi `Resources/FishJamFever`.

## 7. Cập nhật sang máy hoặc nhánh khác

1. Commit **cả file `.cs` lẫn `.meta`**, kèm các thư mục mới: `Scripts/Core`, `Manager`, `GameState`, `UI/Canvas`, `UI/Debug`, `Resources/UI`, `Scenes/_Old`, `Scenes/Main.unity`, `ProjectSettings/EditorBuildSettings.asset`.
2. Các file cũ đã chuyển (`MainMenuController.cs`, `GameplayHUD.cs`, `SettingsPopup.cs`, `GameplayHUDDemo.cs` ở chỗ cũ, `Prefabs/UI/*.prefab`) phải **biến mất** ở chỗ cũ. Nếu Git báo là xoá rồi thêm mới, vẫn đúng vì GUID trong `.meta` không đổi.
3. Mở Unity, mở `Scenes/Main.unity`, bấm Play.
4. Code nào ngoài các file trên còn gọi `MainMenuController`, `GameplayHUD` hoặc `SettingsPopup` thì đổi sang `MainMenu`, `GamePlay` hoặc `Setting`. Hiện trong project không còn chỗ nào.

## 8. Việc đợt sau

- **Loading:** `LoadingState` + `Canvas_Loading` (`sortingOrder` 100). `GameManager.Start()` → `ChangeState(LoadingState.Instance)`.
- **Win/Lose:** `WinState`, `LoseState` + `Canvas_Win`, `Canvas_Lose` (`sortingOrder` 20). Trong `GameplayHUDDemo.EndLevel()`, đổi `MainMenuState` thành `WinState`.
- **Âm thanh:** `AudioManager` (đọc `GameSettings`) và `ButtonClickSound`.
- **Gameplay:** sinh màn trong `PlayingState`, chặn chạm khi popup mở, chọn render mode cho Canvas màn chơi.
