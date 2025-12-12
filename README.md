# MqttDemo

MQTT 客戶端示範專案，用於連接各客戶的 MQTT Broker 進行機台訊號接收與發送。

## 專案結構

```
MqttDemo/
├── Program.cs              # 主程式入口 (WDMIS/景利/鑫型)
├── MachineSignalDto.cs     # 機台訊號資料傳輸物件
├── Status.cs               # 機台狀態列舉
├── TowerlightData.cs       # 三色燈資料模型 (MP-468 宇均)
└── TowerlightTestProgram.cs # 宇均三色燈連線驗證程式
```

## 使用方式

### 執行原始機台訊號程式 (WDMIS/景利/鑫型)

```bash
dotnet run
```

### 執行宇均三色燈測試 (MP-468)

```bash
dotnet run -- --towerlight
# 或
dotnet run -- -t
```

## MQTT Broker 設定

| 客戶 | 主機 | Port | Topic 格式 | 用途 |
|------|------|------|------------|------|
| WDMIS | 172.20.10.152 | 1883 | `{customer}/machine-signal/all` | 機台訊號 |
| 景利 (ginlee) | 192.168.1.237 | 1883 | `ginlee/machine-signal/all` | 機台訊號 |
| 鑫型 (shinmold) | 192.168.1.244 | 1883 | `shinmold/machine-signal/all` | 機台訊號 |
| 宇均 | 172.16.1.22 | 20085 | `{機台代碼}/module/towerlight` | 三色燈 |

## 資料格式

### 機台訊號 (MachineSignalDto)

```json
{
  "MachineId": "Z001",
  "Status": "Operation",
  "SignalTime": "2025-12-12T14:00:00",
  "ProgramName": "MainProc",
  "SubProgramName": "SubProcA"
}
```

**Status 狀態值:**
- `Operation` - 正常運作（稼動中）
- `Stop` - 停止
- `Manual` - 手動操作
- `Emergency` - 緊急狀態
- `Alarm` - 警報
- `EmergencyStop` - 急停
- `Disconnect` - 連線中斷

### 三色燈資料 (TowerlightData)

```json
{
  "id": "TLID949C12BD9710",
  "manufacturerId": "ntu",
  "data": [
    { "sensor": "LED_ch1", "label": "LED_ch1", "value": ["0,0,0,1"] },
    { "sensor": "LED_ch2", "label": "LED_ch2", "value": ["0,0,0,0"] },
    { "sensor": "LED_ch3", "label": "LED_ch3", "value": ["0,0,0,0"] },
    { "sensor": "ioin", "label": "io", "value": ["0,0,0,0"] }
  ]
}
```

**三色燈狀態判定:**
- LED_ch1 亮 (綠燈) = 加工中
- LED_ch2 亮 (黃燈) = 暫停中
- LED_ch3 亮 (紅燈) = 停止中
- 全滅 = 斷線中

**機台對應表 (Topic Code -> 機台名稱):**

| Topic Code | 機台名稱 |
|------------|----------|
| A68 | MC-002 |
| A56 | EDM-020 |
| A50 | MC-005 |
| A60 | WC-003 |
| AA0 | EDM-019 |
| A69 | MC-003 |
| A5E | MC-004 |
| A47 | WC-005 |

## 相關 Linear 票據

- **MP-468** - 三色燈機連網 (宇均)
