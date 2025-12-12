namespace MqttDemo
{
    /// <summary>
    /// 三色燈資料結構 (MP-468 宇均三色燈機連網)
    /// Topic 格式: {機台代碼}/module/towerlight
    /// </summary>
    public class TowerlightData
    {
        /// <summary>
        /// 三色燈設備 ID
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 製造商 ID
        /// </summary>
        public string? ManufacturerId { get; set; }

        /// <summary>
        /// 感測器資料陣列
        /// </summary>
        public List<TowerlightSensorData>? Data { get; set; }
    }

    /// <summary>
    /// 三色燈感測器資料
    /// </summary>
    public class TowerlightSensorData
    {
        /// <summary>
        /// 感測器名稱 (LED_ch1, LED_ch2, LED_ch3, ioin)
        /// </summary>
        public string? Sensor { get; set; }

        /// <summary>
        /// 標籤
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// 值陣列，格式如 "0,0,0,1" (最後一位為 1 表示亮)
        /// </summary>
        public List<string>? Value { get; set; }
    }

    /// <summary>
    /// 三色燈狀態
    /// </summary>
    public enum TowerlightStatus
    {
        /// <summary>
        /// 加工中 (綠燈亮)
        /// </summary>
        Processing,

        /// <summary>
        /// 暫停中 (黃燈亮)
        /// </summary>
        Paused,

        /// <summary>
        /// 停止中 (紅燈亮)
        /// </summary>
        Stopped,

        /// <summary>
        /// 斷線中 (無燈亮)
        /// </summary>
        Disconnected,

        /// <summary>
        /// 混合狀態
        /// </summary>
        Mixed,

        /// <summary>
        /// 未知
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 機台與三色燈對應表
    /// </summary>
    public static class TowerlightMachineMapping
    {
        /// <summary>
        /// MQTT Topic Code -> 機台名稱 對應表
        /// </summary>
        public static readonly Dictionary<string, string> Machines = new()
        {
            { "A68", "MC-002" },
            { "A56", "EDM-020" },
            { "A50", "MC-005" },
            { "A60", "WC-003" },
            { "AA0", "EDM-019" },
            { "A69", "MC-003" },
            { "A5E", "MC-004" },
            { "A47", "WC-005" }
        };

        /// <summary>
        /// 根據 Topic Code 取得機台名稱
        /// </summary>
        public static string GetMachineName(string topicCode)
        {
            return Machines.TryGetValue(topicCode, out var name) ? name : $"未知機台({topicCode})";
        }
    }
}
