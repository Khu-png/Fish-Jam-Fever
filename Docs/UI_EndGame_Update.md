# Cập nhật: UI Hồi sinh / Thắng / Thua (11/11/2026)

## 1. Mockup HTML
**File mới:**
- `Docs/mockups/endgame_ui.html`: 3 màn, chuyển bằng 3 nút ở góc phải. Đã đăng thành artifact "Fish Jam Fever – Hồi sinh / Thắng / Thua".
- `Docs/mockups/make_endgame_html.py`: script tạo file trên.

**Đã chỉnh sau khi xem:**
- **Rương màn Thắng:** đổi thành 960×840, tức 1,2 lần bản đầu.
- **Hai nửa tim vỡ giống nhau:** HTML lấy nhầm `heart.png` (tim nguyên 256×256). Đã đổi sang `heart_379.png`, là ảnh chứa các mảnh của hoạt cảnh (1364×364).

## 2. Script MỚI (Unity)
| File | Nội dung |
|---|---|
| `Scripts/UI/Canvas/Revive.cs` | `Revive : UICanvas`. Có `SetProgress(0..1)`, ô `cost` = 600. Nút xu: `Wallet.Spend` (không đủ xu thì nút rung). Nút quảng cáo: hồi sinh luôn (chưa có SDK). Hồi sinh xong thì đóng popup và phát `Revive.Revived`. Nút ✕ thì `Open<Lose>()`. Mở thì `timeScale = 0`, đóng thì trả 1. Có cờ `isClicked`. |
| `Scripts/UI/Canvas/Win.cs` | `Win : UICanvas`. Có `reward` = 30, quầng sáng xoay. **Claim**: 12 xu bay vào ô xu rồi `Wallet.Add(30)`, sau đó hiện "Tap to continue" nhấp nháy. **Continue**: `CurrentLevel++` rồi `ReplayLevel()`. Dừng game khi mở. |
| `Scripts/UI/Canvas/Lose.cs` | `Lose : UICanvas`. **Retry**: `ReplayLevel()`. **Home**: `ChangeState(MainMenuState)`. Dừng game khi mở. |
| `Scripts/Core/Wallet.cs` | Lưu số xu bằng PlayerPrefs (khoá `coins`, mặc định 120). Có `Coins`, `Add`, `Spend`, sự kiện `Changed`. |
| `Scripts/UI/CoinBar.cs` | Ô hiện số xu, nghe `Wallet.Changed`. Có `Set`, `Show`, `Bump` (nảy). `icon` là điểm xu bay tới. |
| `Scripts/UI/Effects/SpineSequence.cs` | Mỗi lần bật thì chạy chuỗi hoạt cảnh: các hoạt cảnh đầu chạy 1 lần, hoạt cảnh cuối lặp. Chạy cả khi `timeScale = 0`. |
| `Scripts/UI/Effects/UIPopIn.cs` | Hiệu ứng mở: nền tối dần, khung nảy 0.6 → 1.08 → 1. Thay cho `dim`/`panel` vì `UICanvas` **không còn** giữ hiệu ứng này (bạn đã chuyển vào `Setting`). |
| `Editor/EndGameUIBuilder.cs` | Menu **FishJam > Build EndGame UI (Revive, Win, Lose)**. Nhập ảnh, cài 9-slice, dựng 3 prefab. Ghi đè khi chạy lại. |

## 3. Script cũ ĐÃ SỬA
**`Scripts/UI/Debug/GameplayHUDDemo.cs`:**
- Đủ cá: mở `Win` (trước đây về Main Menu).
- Thêm nút **"Thua -> Hoi sinh"**: mở `Revive` với phần trăm = số cá hiện có / tổng.
- Thêm nút **"Thua (bo qua hoi sinh)"**: mở `Lose`.
- Ô TEST ẩn khi `Win`, `Lose` hoặc `Revive` đang mở.

**Không sửa:** `UICanvas`, `UIManager`, `GameManager`, các State, `GamePlay`, `MainMenu`, `Setting`.

## 4. Prefab và asset
**Prefab mới:**
| Prefab | Thứ tự hiển thị | Canvas |
|---|---|---|
| `Resources/UI/Canvas_Revive.prefab` | 25 | Overlay 1080×1920 |
| `Resources/UI/Canvas_Win.prefab` | 20 | Overlay 1080×1920 |
| `Resources/UI/Canvas_Lose.prefab` | 20 | Overlay 1080×1920 |

**Ảnh** – `Art/UI/EndGame/`:
- `Almost_there`, `head_blue_lose`, `BG_blue_popup` (9-slice 90), `pop_lose_blue` (80), `close`, `Tank`, `btn_green_win` (60), `btn_yl_win` (60), `ads`, `Coin_0`, `coin_1`, `bg_number_coinblue` (40), `UI_Fx_Radial_007`, `Purple` (70), `Btnblue`, `icon_home`, `icon_add`.

**Spine** – trong `Art/Spine/`:
| Thư mục | Hoạt cảnh dùng | Ghi chú |
|---|---|---|
| `heart/` | Break → Break_idle | `heart.png` chính là `heart_379.png` đổi tên |
| `Well_Done/` | action → idle | |
| `fish_error/` | idle | |
| `Chest_win/` | Appear → Opening → Open_idle | Có sẵn từ trước |

## 5. Luồng
```
Đang chơi ── (gameplay) sắp thua ──► Open<Revive>().SetProgress(p)
   Revive: 600 xu / Revive(AD) ──► đóng, Revive.Revived (gameplay mở thêm bể)
   Revive: ✕ ──► Open<Lose>  ──► Retry: ReplayLevel | Home: MainMenuState
Đang chơi ── (gameplay) đủ cá ──► Open<Win>  ──► Claim (+30 xu bay) ──► Tap to continue: Level+1, chơi tiếp
```

## 6. Đã kiểm tra (scene Main)
| Thao tác | Kết quả |
|---|---|
| Mở Hồi sinh | Hiển thị đúng, tim vỡ đối xứng, `timeScale = 0`. |
| Bấm 600 xu khi chỉ có 120 | Không trừ xu, popup vẫn mở. |
| ✕ | Sang màn Thua (cá Spine, YOU LOSE, Retry). |
| Retry | Về màn chơi, `timeScale = 1`. |
| Mở Thắng | WELL DONE và rương mở hiện đúng. |
| Claim | Xu thành 150, hiện "Tap to continue". |
| Tap to continue | Lên Level 6, vào màn chơi, `timeScale = 1`. |
| Console | Không có lỗi. |

## 7. Còn lại
- **Gameplay:** gọi `Open<Revive>()` khi khay đầy và không còn nước đi; gọi `Open<Win>()` khi đủ cá; nghe `Revive.Revived` để mở thêm bể.
- **Quảng cáo:** chưa có thật, nút AD hồi sinh luôn.
- **Bỏ khỏi bản gốc:** Starter Pack (shop), Gallery unlocked, vòng mở khoá cá.
- **Chưa có ảnh gốc** cho chữ "VICTORY!", "YOU LOSE" và cá khóc: đang dùng WELL DONE, chữ viết bằng font game, và `fish_error`.
- **Quầng sáng xoay** sau rương chưa thấy rõ, cần xem lại.

## 8. Bổ sung 11/11 chiều: xu bay dùng prefab, phần thưởng nhảy ra từ rương
- **Prefab mới** `Prefabs/UI/FlyCoin.prefab`: Image `Coin_0`, 70×70, không chặn chạm. `Win` dùng `Instantiate(flyCoinPrefab)` thay cho tạo object bằng code. Builder có hàm `EndGameUIBuilder.FlyCoinPrefab()` để tạo prefab này.
- **`Win.cs`:**
  - **Bỏ trường:** `coinSprite`.
  - **Đổi kiểu:** `rewardGroup` từ GameObject thành RectTransform.
  - **Thêm trường:**

    | Trường | Ý nghĩa |
    |---|---|
    | `chest` | SkeletonGraphic của rương |
    | `chestMouth` | Điểm miệng rương |
    | `openEvent` | Mặc định "Open" |
    | `fallbackDelay` | Mặc định 3.5s |
    | `jumpTime` | Mặc định 0.6s |
    | `jumpHeight` | Mặc định 350 |
    | `flyCoinPrefab` | Prefab xu bay |
    | `flyCount` | Mặc định 12 |
  - **Luồng mới:**
    - Khi mở màn, phần thưởng và nút Claim **ẩn**.
    - Lúc rương mở, Spine phát event **"Open"** (ở giây 0.25 của hoạt cảnh `Opening`). Khi đó phần thưởng **nhảy ra từ miệng rương**: to dần 0.2 → 1.15 → 1, bay vòng lên rồi rơi xuống chỗ của nó, nảy nhẹ.
    - Sau đó nút Claim bật ra.
    - Nếu không nhận được event sau `fallbackDelay` giây, phần thưởng vẫn tự nhảy ra.
    - Đường bay tính bằng vị trí thật trên màn hình, nên không phụ thuộc anchor hay pivot.
- **Prefab `Canvas_Win`:** sửa trực tiếp, không build lại. Thêm object `ChestMouth` (giữa, y = 820) và gán các tham chiếu mới.
