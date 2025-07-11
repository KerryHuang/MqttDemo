namespace MqttDemo
{
    /// <summary>
    /// 機台訊號資料傳輸物件
    /// </summary>
    public class MachineSignalDto
    {
        /// <summary>
        /// 機台編號
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 狀態
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 訊號時間（ISO 8601）
        /// </summary>
        public DateTime SignalTime { get; set; }
        /// <summary>
        /// 主程式名稱
        /// </summary>
        public string ProgramName { get; set; }
        /// <summary>
        /// 子程式名稱
        /// </summary>
        public string SubProgramName { get; set; }
    }
}
