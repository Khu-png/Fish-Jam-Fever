# Đợt 1: chuyển Main Menu, Gameplay, Setting sang cấu trúc kiểu Bottle_Up

**Phạm vi:**
- **Làm:** Main Menu, Gameplay (HUD), Setting.
- **Chưa làm:** Loading, Win, Lose, chặn chạm gameplay. Cấu trúc chừa sẵn chỗ, sau này chỉ cần **thêm file**.

## 1. Kết quả

```
Scenes/Main.unity          ← scene duy nhất, đặt đầu Build Settings
├─ Main Camera, EventSystem
├─ GameManager  (Singleton – máy trạng thái MainMenu ⇄ Playing)
└─ UIManager    (Singleton) ── UIRoot

Resources/UI/
├─ Canvas_MainMenu  (MainMenu : UICanvas)  sortingOrder 10
├─ Canvas_GamePlay  (GamePlay : UICanvas)  sortingOrder 0
└─ Canvas_Setting   (Setting  : UICanvas)  sortingOrder 30
```

**Luồng:**

```
Mở game → MainMenuState
  Menu: Play → PlayingState            Menu: ⚙ → Setting (chế độ Menu)
  HUD:  ⚙ → Setting (InGame, game dừng) → ✕ chơi tiếp | Retry chơi lại | Home về menu
  HUD (TEST): đủ cá → hết màn → về Main Menu   (sau này thay bằng Win/Lose)
```

## 2. File mới

| File | Nội dung |
|---|---|
| `Core/Singleton.cs` | Chép từ Bottle_Up. Thêm `Debug.LogWarning` khi phải tự tạo object vì thiếu trong scene. |
| `UI/UICanvas.cs` | Lớp cha mọi màn. Có `SetUp()`, `Open()`, `Close(time)`, `CloseAnimated()`, `CloseDirectly()`. **Sửa so với Bottle_Up:** `Awake` để `protected virtual`; `Close(time)` dùng `WaitForSecondsRealtime`; có hiệu ứng mở/đóng dùng chung (gán `dim` + `panel`, lấy từ SettingsPopup hiện tại); tai thỏ dùng `SafeAreaFitter` thay vì đoán theo tỉ lệ màn. |
| `UI/UIManager.cs` | Chép nguyên từ Bottle_Up: `Resources.LoadAll<UICanvas>("UI/")`, `Open<T>()`, `GetUI<T>()`, `CloseDirectly<T>()`, `IsOpened<T>()`, `CloseAll()`. |
| `GameState/IGameState.cs` | `OnEnter`, `OnExecute`, `OnExit`. |
| `GameState/MainMenuState.cs` | Enter: `CloseAll()` rồi `Open<MainMenu>()`. Exit: đóng MainMenu. |
| `GameState/PlayingState.cs` | Enter: `CloseAll()` rồi `Open<GamePlay>()`. Exit: đóng GamePlay. Có chừa chỗ cho gameplay sinh màn và kiểm tra thắng/thua sau này. |
| `Manager/GameManager.cs` | `ChangeState()`, `ReplayLevel()` (đang chơi thì tự gọi Exit rồi Enter), `CurrentLevel` (PlayerPrefs `current_level`). `Start()` vào thẳng `MainMenuState` vì chưa có Loading. |

## 3. File sửa

| Cũ | Mới | Thay đổi chính |
|---|---|---|
| `MainMenuController` | `UI/Canvas/MainMenu : UICanvas` | `SetUp()` ghi "Level n". `PlayButton()` → `ChangeState(PlayingState)`. `SettingButton()` → `Open<Setting>()`. Bỏ `SceneManager.LoadScene` và ô `settingsPopup`. |
| `GameplayHUD` | `UI/Canvas/GamePlay : UICanvas` | `SetUp()` đặt level, `ResetCombo()`, tắt cảnh báo. `SettingButton()` → `Open<Setting>()`. **Giữ nguyên** `SetFishCount`, `OnTankCompleted`, `ShowPraise`, `SetWarning`. Bỏ ô `settingsPopup`. |
| `SettingsPopup` | `UI/Canvas/Setting : UICanvas` (Canvas riêng) | `SetUp()` tự chọn chế độ: đang chơi thì **InGame** (`Time.timeScale = 0`), còn lại thì **Menu**. Home → `ChangeState(MainMenuState)`. Retry → `ReplayLevel()`. ✕ → `CloseAnimated()`. `CloseDirectly()` luôn trả `timeScale = 1`. Thêm cờ `isClicked` chống bấm hai lần. Bỏ UnityEvent `onHome/onRetry/onPause`. Giữ `Toggle3` và `GameSettings`. |
| `GameplayHUDDemo` | `UI/Debug/GameplayHUDDemo` | Ẩn khi Setting đang mở (`IsOpened<Setting>()`). **Thêm test "đủ cá thì hết màn"** (mục 4). |
| `GameSettings`, `SafeAreaFitter`, `Effects/*` | giữ nguyên | — |

Gameplay gọi HUD như sau, **tên hàm không đổi**:

```csharp
UIManager.Instance.GetUI<GamePlay>().SetFishCount(collected, total);
```

## 4. Test mới: đủ cá thì hết màn và về Main Menu

Thêm vào ô TEST trên HUD:
- Nút **"+3 cá"**: tăng bộ đếm 3 (giả lập một bể đầy), đồng thời gọi `OnTankCompleted()` để chạy combo và chữ khen.
- Nút **"Đủ cá"**: đặt bộ đếm bằng tổng ngay lập tức.
- Ô **"Tự động xong bể"**: giữ như cũ, cũng tăng 3 cá mỗi lần.
- Khi `collected >= total`:
  - khoá các nút TEST;
  - chờ **1 giây** để thấy bộ đếm đầy và chữ khen cuối;
  - gọi `GameManager.Instance.ChangeState(MainMenuState.Instance)`.
- Mỗi lần vào màn chơi, bộ đếm reset về `0/total` (test dùng `total = 30` cho nhanh).

```csharp
void AddFish(int n)
{
    fish = Mathf.Min(total, fish + n); hud.SetFishCount(fish, total);
    if (fish >= total && !ending) { ending = true; StartCoroutine(EndLevel()); }
}
IEnumerator EndLevel()
{
    yield return new WaitForSecondsRealtime(1f);
    GameManager.Instance.ChangeState(MainMenuState.Instance);   // sau này: WinState
}
```

Sau này, khi làm Win thì chỉ cần đổi `MainMenuState` thành `WinState` ở dòng cuối.

## 5. Prefab và Builder

Prefab đã chỉnh tay (8 chữ khen, `PraiseAnchor`, menu Overlay), nên **di chuyển bằng Unity để giữ nguyên tham chiếu, không build lại**:

1. Chuyển `Prefabs/UI/Canvas_MainMenu` → `Resources/UI/Canvas_MainMenu`.
2. Chuyển `Prefabs/UI/Canvas_Gameplay` → `Resources/UI/Canvas_GamePlay`.
3. Thay script trên gốc và gán lại các tham chiếu bằng menu **FishJam > Migrate UI (Phase 1)** (mình viết).
4. Gỡ `SettingsPopup` đang nhúng trong hai Canvas.
5. Tạo `Resources/UI/Canvas_Setting`: Canvas Overlay, `sortingOrder` 30, gốc gắn `Setting`.
6. Nối OnClick cho các nút.

Builder đổi đường lưu sang `Resources/UI/`, thôi tạo scene. `SettingsPopupBuilder` bỏ phần nhúng vào hai Canvas.

## 6. Scene

- Tạo `Main.unity` (Camera, EventSystem, GameManager, UIManager + UIRoot) và đặt ở vị trí 0 trong Build Settings.
- `MainMenu.unity` và `Game.unity` chuyển vào `Scenes/_Old/`, không xoá.
- Giữ `GameplayUI.unity` làm scene thử UI.

## 7. Kiểm tra

| # | Thao tác | Mong đợi |
|---|---|---|
| 1 | Play scene `Main` | Main Menu hiện (logo, cây lắc, Level 1). |
| 2 | Bấm Level 1 | Sang HUD, bộ đếm `0/30`, có ô TEST. |
| 3 | ⚙ → ✕ | Setting InGame, game dừng; ✕ thì đóng có hiệu ứng, chạy tiếp. |
| 4 | ⚙ → Retry | HUD reset (đếm 0, combo 0), không đứng hình. |
| 5 | ⚙ → Home | Về menu, `timeScale` = 1. |
| 6 | Menu → ⚙ | Setting chế độ Menu. Tắt Music, mở lại vẫn tắt. |
| 7 | HUD: bấm "+3 cá" liên tục hoặc "Đủ cá" | Chữ khen theo combo. Đủ 30 thì sau 1 giây về Main Menu. |
| 8 | Bấm nhanh hai lần Home/Retry | Chỉ chạy một lần. |

## 8. Đợt sau

- **Loading:** `LoadingState` + `Canvas_Loading`.
- **Win/Lose:** `WinState`, `LoseState` + popup. Đổi dòng cuối của test (mục 4).
- **Âm thanh:** `AudioManager` (đọc `GameSettings`) và `ButtonClickSound`.
- **Gameplay:** chặn chạm khi popup mở, sinh màn trong `PlayingState`.
