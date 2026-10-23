## 修改了什麼

### 實測狀態

| 項目 | 狀態 |
|---|---|
| 1. 按 A 鍵平面與邊界框的顏色 | **已在 Quest 3 實測**,顏色正確顯示 |
| 2. 診斷腳本 `ScanStatusLogger` | 已加入,輸出內容待實測確認 |
| 3. 殭屍依地板高度貼地 | 已加入,但依賴下方第 4 點,尚未單獨驗證 |
| 4. 停用 XR Device Simulator | 已修改場景,**尚未在 Quest 3 驗證** |

### 1. 按 A 鍵時平面與邊界框沒有顯示顏色

**問題**:按右手 A 鍵時,DebugPanel 有顯示 `Visualization Mode` 在 0 / 1 / 2 之間切換,但畫面上看不到有顏色的平面或邊界框。

**判斷**:原本的做法是改平面預製物上材質的透明度。預製物用的是 URP 透明材質,外框線用的是 Unity 內建的線材質,這兩者在頭盔打包後不一定會保留。

**修改**:`Assets/Samples/XR Interaction Toolkit/3.0.11/AR Starter Assets/Scripts/SceneController.cs`

- `SetTrackableAlpha`:切換顯示時,為每個平面和邊界框指定一個一定會包進版本的透明材質(`Sprites/Default`),再設顏色與透明度。
- 新增 `UseSpriteMaterial`(指定材質)與 `GetTrackableColor`(依類型決定顏色)。
- 顏色:地板綠、牆壁白、天花板紅、桌面黃、沙發與座位藍、牆上裝飾橘、門洞洋紅、窗洞青、其他灰,邊界框青色。
- 模式 0(關閉)時填色與外框都隱藏。切換順序不變:關閉 → 平面 → 邊界框 → 關閉。

**結果**:在 Quest 3 上實測,按 A 後顏色正確顯示。

### 2. 新增診斷腳本,確認飄浮的原因

**問題**:殭屍和平面飄在空中,和真實空間對不上。

**修改**:新增 `Assets/Scripts/ScanStatusLogger.cs`

- 啟動時自己建立,不需要在場景裡加物件。
- 每 3 秒在 DebugPanel 印出 `SCAN:` 一行,例如:
  `SCAN: planeMgr=on running=True planes=17 (floor 1, wall 6, table 4, other 6) boxes=10 origin=Floor (asked Floor) originY=0.00 headY=1.26`
- 啟動 4 秒後印一次 `TRACKING:`,顯示頭盔支援的追蹤模式、目前使用的模式、`Camera Offset` 的實際高度與 `Camera Y Offset` 設定。
- 判讀:
  - `headY` 隨你蹲下、站起、走動而變化:頭部追蹤正常。
  - `headY` 一直不變(例如固定接近 0):相機沒有被頭盔追蹤驅動。
  - `origin=Floor (asked Floor)`:追蹤原點模式正確。
  - `origin=Device (asked Floor)`:要求 Floor 失敗,退回 Device 模式並加上 `Camera Y Offset`(1.1176 公尺),整個世界會被抬高。常見原因是 Quest 的邊界設為「站立式」或沒有設定地板高度。

### 3. 殭屍依地板高度貼地

**問題**:場景中殭屍(`Zombie1`)的位置是寫死的 `(0, -0.829, 1.5)`,這個 Y 值假設執行時 Y = 0 就是真實地板,假設不成立時殭屍就會飄。掃描不會移動這類手動擺放的物件。

**修改**:新增 `Assets/Scripts/GroundToFloor.cs`

- 啟動時自動加到名為 `Zombie1` 的物件上,不需要修改場景。
- 等待掃描到的地板平面(最多 6 秒),把殭屍的**最低點**放到地板高度;沒有地板平面就用 XR Origin 的高度。
- 完成後在 DebugPanel 印出 `GroundToFloor: Zombie1 placed on floor Y = ...`。

### 4. 停用 XR Device Simulator(最可能的飄浮原因)

**觀察到的現象**

- 殭屍會**跟著使用者移動**,不像固定在房間裡,不是 MR 的效果。
- 從截圖看,地板與另一個平面呈**水平的一條線**,位置與眼睛同高,表示平面與相機的相對高度不隨頭部移動改變。
- 不論怎麼調整地板高度,偏移都沒有改善。
- 掃描資料本身是正常的(偵測到地板、牆壁、天花板、桌面等 11 個平面與 2 個邊界框)。

**判斷**:場景裡有一個**啟用中**的 `XR Device Simulator`。它是在電腦上用鍵盤滑鼠模擬頭盔與手把的測試工具,放進頭盔版本後會接管頭部與手把輸入,導致 Unity 的相機沒有被真實的頭部追蹤驅動,虛擬世界就固定在視角上。上面的現象與這個推論吻合,但**還沒有在頭盔上驗證**。

**修改**:`Assets/Scenes/SampleScene.unity`

- 在 `XR Device Simulator` 這個 Prefab 實例上加入 `m_IsActive: 0`,預設停用。
- 其他物件沒有更動。
- 在 Unity 裡也可以手動做:Hierarchy 點 `XR Device Simulator`,取消 Inspector 最上方名稱左邊的勾選。要在電腦上測試時再勾回去。

> 場景檔容易和別人的修改衝突,若要合併,建議只在說明中提醒「請停用 XR Device Simulator」,由專案作者自己在 Unity 裡操作。

## 在頭盔上測試的建議步驟

1. 確認 `XR Device Simulator` 已停用(Hierarchy 裡名稱為灰色)。
2. Quest「設定 → 實體空間 → 邊界」,改成**房間尺度 (Roomscale)** 並**重設地板高度**。
3. 完成房間掃描(標記桌子、沙發、門窗等)。
4. Unity 選單 File → Build Settings,平台 Android,Run Device 選 Quest 3,按 Build and Run。
5. 進入 app 後走動、轉頭,看殭屍是否固定在房間裡、走近走遠大小是否改變。
6. 看 DebugPanel 的 `TRACKING:` 與 `SCAN:` 兩行,`headY` 應隨動作改變;按右手 A 鍵,平面應貼著牆與地板。

## 尚未解決 / 注意

- 若停用 `XR Device Simulator` 後平面仍和真實空間差一個固定高度,請提供 `TRACKING:` 那一行,可據此判斷是追蹤模式或頭盔地板高度的問題。
- 專案同時安裝了 `com.meta.xr.sdk.all`(Meta XR SDK)與 Unity OpenXR: Meta 兩套 SDK,場景只用到 OpenXR 這一套。兩者並存的影響尚未確認。
- OpenXR 的 Android 設定中,**Meta Quest Touch Plus Controller Profile** 目前是關閉的。Quest 3 的手把是 Touch Plus,建議啟用,否則部分按鍵綁定可能對不上。
- `ding.apk`、`TempAssembly.dll`、`MR_Room_BurstDebugInformation_DoNotShip` 是打包產物,不應放在 repository 裡,我沒有更動它們。
