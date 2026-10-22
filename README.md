## 修改了什麼

### 1. 按 A 鍵時平面與邊界框沒有顯示顏色

**問題**:按右手 A 鍵時,DebugPanel 有顯示 `Visualization Mode` 在 0 / 1 / 2 之間切換,但畫面上看不到有顏色的平面或邊界框。

**判斷**:原本的做法是改平面預製物上材質的透明度。預製物用的是 URP 透明材質,外框線用的是 Unity 內建的線材質,這兩者在頭盔打包後不一定會保留。這是推斷,尚未在裝置上驗證。

**修改**:`Assets/Samples/XR Interaction Toolkit/3.0.11/AR Starter Assets/Scripts/SceneController.cs`

- `SetTrackableAlpha`:切換顯示時,為每個平面和邊界框指定一個一定會包進版本的透明材質(`Sprites/Default`),再設顏色與透明度。
- 新增 `UseSpriteMaterial`(指定材質)與 `GetTrackableColor`(依類型決定顏色)。
- 顏色:地板綠、牆壁白、天花板紅、桌面黃、沙發與座位藍、牆上裝飾橘、門洞洋紅、窗洞青、其他灰,邊界框青色。
- 模式 0(關閉)時填色與外框都隱藏。切換順序不變:關閉 → 平面 → 邊界框 → 關閉。

### 2. 新增診斷腳本,確認飄浮的原因

**問題**:殭屍和平面飄在空中。XR Origin 的設定本身是正確的(Tracking Origin Mode = Floor、位置 0、Camera Offset 0),因此懷疑執行時沒有真正進入 Floor 模式。

**修改**:新增 `Assets/Scripts/ScanStatusLogger.cs`

- 啟動時自己建立,不需要在場景裡加物件。
- 每 3 秒在 DebugPanel 印出一行,例如:
  `SCAN: planeMgr=on running=True planes=17 (floor 1, wall 6, table 4, other 6) boxes=10 origin=Floor (asked Floor) originY=0.00 headY=1.26`
- 判讀:
  - `origin=Floor (asked Floor)`:正確。
  - `origin=Device (asked Floor)`:要求 Floor 失敗,退回 Device 模式並加上 Camera Y Offset(1.1176 公尺),整個世界會被抬高,這會造成飄浮。常見原因是 Quest 的邊界設為「站立式」或沒有設定地板高度。
  - `headY` 接近 0 也是 Device 模式的跡象。

### 3. 殭屍依地板高度貼地

**問題**:場景中殭屍(`Zombie1`)的位置是寫死的 `(0, -0.829, 1.5)`,這個 Y 值假設執行時 Y = 0 就是真實地板,假設不成立時殭屍就會飄。

**修改**:新增 `Assets/Scripts/GroundToFloor.cs`

- 啟動時自動加到名為 `Zombie1` 的物件上,不需要修改場景。
- 等待掃描到的地板平面(最多 6 秒),把殭屍的**最低點**放到地板高度;沒有地板平面就用 XR Origin 的高度。
- 完成後在 DebugPanel 印出 `GroundToFloor: Zombie1 placed on floor Y = ...`。

## 在頭盔上測試的建議步驟

1. Quest「設定 → 實體空間 → 邊界」,改成**房間尺度 (Roomscale)** 並**重設地板高度**。
2. 完成房間掃描(標記桌子、沙發、門窗等)。
3. Unity 選單 File → Build Settings,平台 Android,Run Device 選 Quest 3,按 Build and Run。
4. 進入 app 後看 DebugPanel 的 `SCAN:` 那一行,並按右手 A 鍵,確認平面有出現顏色。