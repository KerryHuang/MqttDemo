namespace MqttDemo
{
    public enum Status
    {
        /// <summary>
        /// 正常運作（稼動中）
        /// </summary>
        Operation,
        /// <summary>
        /// 停止
        /// </summary>
        Stop,
        /// <summary>
        /// 手動操作
        /// </summary>
        Manual,
        /// <summary>
        /// 緊急狀態
        /// </summary>
        Emergency,
        /// <summary>
        /// 警報
        /// </summary>
        Alarm,
        /// <summary>
        /// 急停
        /// </summary>
        EmergencyStop,
        /// <summary>
        /// 連線中斷
        /// </summary>
        Disconnect
    }
}
