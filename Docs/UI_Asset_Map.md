# Fish Jam Fever – Bản đồ asset UI (Prototype)

> Mục đích: chỉ ra **ảnh nào trong bản giải nén của game gốc** dùng cho từng phần UI của prototype, để người làm UI / hiệu ứng / âm thanh lấy đúng file.
> Phạm vi: Main menu, HUD gameplay, chữ khen + hiệu ứng, Win, Lose, Cài đặt, Tutorial. Không gồm shop, booster, event.
> Lưu ý: dự án tự học, không phát hành – asset là của nhà phát hành game gốc.

Ảnh xem nhanh (contact sheet có tên + kích thước) nằm ở `Docs/asset_sheets/`.

## 0. Đường dẫn viết tắt

| Viết tắt | Đường dẫn đầy đủ |
|---|---|
| `SG/` | `Assets/_Game/Resources/FishJamFever/images/sprites/_by_source/SceneGame/` |
| `RF/` | `Assets/_Game/Resources/FishJamFever/images/sprites/_by_source/resfiles/` |
| `FISH/` | `Assets/_Game/Resources/FishJamFever/images/sprites/FishSort/Image/Fish/` (386 hình cá) |
| `SPINE-SG/` | Ảnh: `images/textures/_by_source/SceneGame/` · Dữ liệu: `data/text/_by_source/SceneGame/` (`.json` + `.atlas`) |
| `SPINE-RF/` | Ảnh: `images/textures/_by_source/resfiles/` · Dữ liệu: `data/text/_by_source/resfiles/` |
| `FONT/` | `Assets/_Game/Resources/FishJamFever/data/fonts/_by_source/SceneGame/` |

**Animation Spine** (logo, linh vật, bàn tay tutorial, chuyển màn…) là bộ 3 file `.json` + `.atlas` + `.png`, phiên bản **Spine 4.0.64**. Cần cài gói **spine-unity runtime bản 4.0** mới dùng được. Nếu chưa muốn cài, tạm thay bằng ảnh tĩnh.

---

## 1. Dùng chung

| Thành phần | File | Kích thước | Ghi chú |
|---|---|---|---|
| Font tiêu đề / số | `FONT/royal_kingdom_font.otf` | – | Font tròn đậm của game. Tạo TMP Font Asset |
| Font thường | `FONT/NotoSans-Bold.ttf` | – | Có tiếng Việt |
| Nút tròn xanh (nền icon) | `SG/Btnblue.png` | 144×144 | Nền cho nút Cài đặt, Home… |
| Nút xanh lá | `SG/btn_green.png`, `SG/btn_green_big.png`, `SG/button_green.png` | 178×178, 152×176, 180×180 | Nút chính (Next, Retry). 9-slice |
| Nút đỏ / vàng / xám / xanh nhỏ | `SG/btn_red.png`, `SG/btn_yl_win.png`, `SG/btn_grey.png`, `SG/btn_small_blue.png` | ~148–178 | 9-slice |
| Nút đóng (X đỏ) | `SG/close.png` | 155×156 | Đóng popup |
| Icon bánh răng | `SG/btn_setting.png` | 106×115 | Đặt trên `Btnblue.png` |
| Icon Home | `SG/icon_home.png` | 224×236 | Ngôi nhà nấm |
| Icon chơi lại | `SG/replay.png` | 78×78 | Nút Retry |
| Icon quay lại | `SG/icon_back.png`, `SG/btn_out.png` | 232×232, 104×104 | |
| Khung popup tròn xanh | `SG/BGpopup2.png` | 400×400 | 9-slice |
| Khung popup lớn | `RF/POPUP.png` | 432×440 | 9-slice |
| Dải tiêu đề popup | `SG/head_blue_lose.png`, `SG/head_pp.png`, `SG/head_red.png` | 731×226 / 348 / 354 | Đầu popup xanh / tím / đỏ |
| Nền tối phủ màn | `SG/Fade.png`, `RF/bg_tut.png` | 512×512, 1000×1000 | Sau popup, tutorial |
| Chuyển màn | `SPINE-SG/chuyenman` | 512×512 | Spine (bể cá + cá) |
| Loading | `SPINE-RF/Cua_loading` (con cua), `SG/Loading.png` (vòng quay) | | |

---

## 2. Main menu

| Thành phần | File | Kích thước | Ghi chú |
|---|---|---|---|
| Nền đáy biển | `SG/bg.png` | 1535×2048 | Đúng nền màn Home trong video |
| Cảnh trang trí Home | `SPINE-SG/Home` | 996×996 | Spine: tượng nàng tiên cá, rong, san hô |
| Logo | `SPINE-SG/LOGO` | 1024×1024 | Spine, animation `appear`, `idle` |
| Nút Play "Level N" (tím) | `SG/icon3_banner.png` hoặc `SG/icon2_banner.png` | 224×80 | Dải tím viền vàng, kéo giãn 9-slice + chữ "Level N" bằng font tiêu đề |
| Nút Cài đặt | `SG/Btnblue.png` + `SG/btn_setting.png` | | Góc trên phải |
| Linh vật | `SPINE-RF/Fish` | 488×488 | Cá đỏ, animation `animation` |

Không làm (game gốc có): xu (`SG/coins*.png`), Login, Starter Pack (`SG/pack*.png`), Remove Ads (`SG/No_ADS.png`), thanh tab (`SG/Tab_Bar*.png`).

---

## 3. HUD gameplay

| Thành phần | File | Kích thước | Ghi chú |
|---|---|---|---|
| Nền gameplay | `SG/BG1.png` | 2076×2732 | Đáy biển, lâu đài |
| Huy hiệu Level (tròn tím) | `SG/icon3_tim.png` | 216×216 | + chữ "Level N" |
| Kệ gỗ | `SG/Slot_Fish_Ke.png` | 755×52 | Dưới 4 bể |
| Bể cá tròn | `SPINE-SG/mask_tank`, `SPINE-SG/Tank_Stack_Fish` | | Bể tròn bản mới nằm trong Spine; cần tách hoặc dùng Spine |
| Bể vuông + dấu + (bản cũ) | `SG/Tank.png`, `SG/Front.png`, `SG/MaskTank.png` | 219×214, 268×255 | Dùng tạm nếu chưa có bể tròn |
| Bể khóa | `SG/ChauCa_Lock.png`, dấu + `SG/+.png` | 267×275, 71×73 | |
| Huy hiệu yêu cầu (bảng treo) | `SG/Board_Fish.png` | 232×232 | Đặt icon cá + chữ "x/3" |
| Icon cá nhỏ cho bộ đếm | `SG/fish_sd.png` | 56×36 | Cạnh "x/120" |
| Khung số | `SG/bg_number.png`, `SG/bg_value.png` | 71×73, 68×88 | Bộ đếm / số nhỏ |
| Ô khay chờ | `RF/Slot.png` | 388×376 | Ô vuông xanh – thu nhỏ, cần kiểm tra lại cho khớp video |
| Hình cá | `FISH/fish_*.png` | | Chọn 12 loại cho prototype |
| Bong bóng | `SG/bubble_2.png`, `SG/bubble_round3.png`, `SG/bubble_2x3.png`, `SG/bubble_2x4.png`, `SG/bubble_3x4.png`, `SG/bubble_3x5.png` | 484–646 | Một hoặc nhiều bong bóng dính nhau |
| Bong bóng băng | `SG/frozen_bubble_1.png`, `_2`, `_3` | 512×512 | 3 mức nứt (còn 3 / 2 / 1 lần) |
| Cá bị đóng băng | `SG/frozen_fish.png` | 312×284 | |
| Rong trang trí bong bóng | `SPINE-SG/fixed_bubble_front`, `fixed_bubble_back` | | |
| Viền đỏ cảnh báo | `SG/warning.png` | 264×268 | 9-slice, kéo phủ toàn màn |
| Băng-rôn "Only one slot left!" | `SG/BG_Text.png` | 952×116 | Dải tối mờ + chữ |

Booster (để sau): `SG/Booster_Clear.png`, `Booster_Collect.png`, `Booster_Shuffle.png`, `SG/Btn_Booster_BG.png`, `SG/Btn_Booster_Lock.png`, `SG/Icon_*.png`.

---

## 4. Chữ khen (combo)

| Cấp | File | Kích thước |
|---|---|---|
| Nice! | `SG/nice.png` | 358×167 |
| Good! | `SG/good.png` | 406×163 |
| Great! | `SG/great.png` | 418×158 |
| Excellent! | `SG/exellent.png` (tên file viết sai, giữ nguyên) | 575×165 |
| Amazing! | `SG/amazing.png` | 624×202 |
| Perfect! | `SG/perfect.png` | 561×169 |
| (dư) Fantastic!, Cool! | `SG/fantastic.png`, `SG/cool.png` | |

**Hạt / hiệu ứng đi kèm:**

| Dùng cho | File |
|---|---|
| Lấp lánh | `SG/sparkle01.png`, `SG/sparkle3.png`, `SG/t_sparkle_2.png`, `SG/t_flare.png` |
| Ngôi sao | `SG/star.png` |
| Quầng sáng | `SG/t_glow_2.png`, `SG/t_glow_3.png`, `SG/Circle03.png` |
| Tia sáng | `SG/t_ray3.png`, `SG/t_ray4.png`, `SG/light.png`, `SG/UI_Fx_Radial_007.png` |
| Vòng lan ra | `SG/t_ring.png`, `SG/Circle02_2.png`, `SG/Circle02_3.png`, `SG/C_ring_03 1.png` |
| Nổ sáng | `SG/fx.png`, `SG/fx2.png` |
| Bọt vỡ (bong bóng vỡ) | `SG/soap.png`, `SG/soap_trans.png`, `SG/twirl05.png` |

Gợi ý: chữ hiện ra kèm scale punch (0 → 1.2 → 1), phía sau 1 quầng sáng + 4–6 hạt lấp lánh; cấp càng cao càng nhiều hạt và thêm tia sáng.

---

## 5. Màn Win

| Thành phần | File | Ghi chú |
|---|---|---|
| Chữ chúc mừng | `SPINE-RF/congratulation anim` (`action`, `idle`), `SPINE-SG/Well_Done` | Không tìm thấy ảnh chữ "VICTORY!" tĩnh – tạm dùng Spine này hoặc chữ bằng font tiêu đề |
| Linh vật vui | `SPINE-RF/Fish` | |
| Tia sáng sau linh vật | `SG/light.png`, `SG/t_ray4.png` | Xoay chậm |
| Nút Next | `SG/btn_green_win.png` hoặc `SG/btn_green_big.png` | + chữ "Next" |
| Nút Home | `SG/Btnblue.png` + `SG/icon_home.png` | |
| Pháo giấy | Không có ảnh riêng – dùng Particle System với sprite `SG/UISprite.png` (ô vuông trắng) đổi màu ngẫu nhiên | |

Không làm: rương, xu, Claim (`SPINE-SG/Win`, `Chest_win`, `SG/Coin_Win.png`).

---

## 6. Màn Lose

| Thành phần | File | Ghi chú |
|---|---|---|
| Khung popup thua | `SG/lose_popup_pp.png` / `SG/lose_popup_red.png` | 400×400 |
| Đầu popup | `SG/head_blue_lose.png` | 731×226 |
| Trái tim vỡ | `RF/broken-heart.png` | 246×197 |
| Chữ "YOU LOSE" | Không tìm thấy ảnh – dùng font tiêu đề, màu tím, viền trắng | |
| Linh vật khóc | Không tìm thấy ảnh tĩnh – có thể là animation khác của `SPINE-RF/Fish` hoặc cần vẽ | |
| Nút Retry | `SG/btn_green.png` + `SG/replay.png` | |
| Nút Home | `SG/Btnblue.png` + `SG/icon_home.png` | Góc trên trái |
| Thanh "Progress: x%" | `SG/progress_bg.png` + `SG/progress_953.png` | 650×69 + 469×41 |

Không làm: popup "Almost there!" bán hồi sinh (`SG/Almost there.png`, `SPINE-SG/Revive`).

---

## 7. Popup Cài đặt

| Thành phần | File |
|---|---|
| Khung | `SG/BGpopup2.png` hoặc `RF/POPUP.png` + `SG/head_blue_lose.png` |
| Âm thanh / Nhạc / Rung | `SG/sound.png`, `SG/music.png`, `SG/vibrate.png` |
| Gạch chéo khi tắt | `SG/off.png` |
| Nút đóng | `SG/close.png` |
| Công tắc (nếu dùng) | `SG/Knob.png`, `SG/Checkmark.png` |

---

## 8. Tutorial

| Thành phần | File |
|---|---|
| Bàn tay chỉ | `SPINE-SG/hand_zoom` (Spine) |
| Hộp thoại | `RF/boxchat.png` (668×192), `SG/BGPopTut.png` |
| Nền tối có lỗ sáng | `RF/bg_tut.png` |
| Mũi tên | `RF/arrow 1.png` |

---

## 9. Còn thiếu / cần tự làm

| Thứ | Cách xử lý đề xuất |
|---|---|
| Chữ "VICTORY!" và "YOU LOSE" | Font `royal_kingdom_font` + viền + đổ bóng trong TMP |
| Linh vật khóc | Kiểm tra thêm animation trong `SPINE-RF/Fish`; nếu không có, dùng linh vật thường + giọt nước mắt |
| Bể tròn dạng ảnh tĩnh | Tách từ atlas `SPINE-SG/mask_tank` (dùng file `.atlas` để biết tọa độ) hoặc dùng Spine |
| Pháo giấy | Particle System |
| Âm thanh | Có sẵn 86 file trong `sounds/` – xem mục 11 |

## 10. Quy ước khi đưa vào project

- Copy các file đã chọn sang `Assets/_Game/Art/UI/<Màn>/` (vd `Art/UI/HUD/Board_Fish.png`), **không** dùng thẳng trong `Resources`.
- Import: Texture Type = Sprite (2D and UI), Mesh Type = Full Rect cho ảnh 9-slice, đặt Border trong Sprite Editor.
- Gom sprite UI vào 1–2 Sprite Atlas (`UI_Common`, `UI_Gameplay`) để giảm draw call.
- Canvas Scaler: Scale With Screen Size, 1080×1920, Match 0.5.
- Sau khi copy xong, chuyển cả thư mục `Resources/FishJamFever` ra ngoài `Assets` (vd `Reference/`) để không bị đóng gói vào bản build.

---

## 11. Âm thanh

Thư mục: `Assets/_Game/Resources/FishJamFever/sounds/` (86 file `.ogg` / `.wav`, chi tiết ở `README_sounds.md`). Gán theo tên file – **nghe lại từng file để xác nhận**.

| Sự kiện | File đề xuất |
|---|---|
| Nhạc nền menu / gameplay | `BG_Music.ogg`, `BgMusic_01.ogg` |
| Bấm nút UI / bật tắt | `SFX_UI_Button_Click.wav`, `SFX_UI_Button_Click_Pop.wav`, `SFX_UI_Button_Click_Switch.wav` |
| Mở / đóng popup | `SFX_Sliding_Door_In.ogg`, `SFX_Sliding_Door_Out.ogg` |
| Chạm cá | `SFX_ClickFish.ogg` |
| Chạm cá không chọn được (trong băng) | `SFX_Click_False_Object.ogg` |
| Cá vào bể | `SFX_ToTarget.ogg` |
| Cá vào khay chờ | `SFX_ToTemp.ogg` |
| Bể đủ 3/3 | `SFX_TargetFull.ogg` |
| Bể mới trượt vào | `SFX_Movement_Swoosh_Slow.ogg` hoặc `SFX_Whoosh_Fast.ogg` |
| Bong bóng vỡ | `SFX_Match_Pop.wav`, `SFX_soapclean.ogg` |
| Bong bóng xuất hiện | `SFX_BubbleDown.ogg`, `SFX_soapfly.ogg` |
| Băng xuất hiện / vỡ | `SFX_Frozen.ogg` / `SFX_ice_breaking.ogg` |
| Số trên băng giảm | `SFX_ding.ogg` |
| Cảnh báo khay 4/5 | `SFX_TempTip.ogg` |
| Chữ khen 6 cấp | `SFX_nice.ogg`, `SFX_good.ogg`, `SFX_great.ogg`, `SFX_excellent.ogg`, `SFX_amazing.ogg`, `SFX_perfect.ogg` |
| (dư, dùng nếu thêm cấp) | `SFX_cool.ogg`, `SFX_awesome.ogg`, `SFX_fantastic.ogg`, `SFX_wonderful.ogg`, `SFX_incredible.ogg`, `SFX_unblievable.ogg`, `SFX_legendary.ogg` |
| Thắng | `SFX_Win.ogg` (+ `SFX_Win_Level_*.ogg` cho các đoạn hiệu ứng) |
| Thua | `SFX_Lose.ogg` hoặc `SFX_Fail.ogg` |
| Chuyển màn | `SFX_Fade_In.ogg` |
