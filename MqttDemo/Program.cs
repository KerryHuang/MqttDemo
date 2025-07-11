using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
/*
 * MQTTnet 5.x 已整合 Client 相關 API，無需額外引用 Options/Subscribing/Publishing 命名空間。
 * 下方 Main 函式示範 MQTT 客戶端連線、訂閱、發佈與斷線流程。
 */

using System.Text.Json;
using MqttDemo;

class Program
{
    /// <summary>
    /// 亂數產生 MachineSignalDto 實體
    /// </summary>
    static MachineSignalDto GenerateRandomSignal()
    {
        // 狀態改為隨機選取 Status enum 成員
        var programList = new[] { "MainProc", "AuxProc", "TestProc" };
        var subProgramList = new[] { "SubProcA", "SubProcB", "SubProcC" };

        var rand = new Random();
        var statusValues = Enum.GetValues(typeof(Status));
        var status = (Status)statusValues.GetValue(rand.Next(statusValues.Length));
        return new MachineSignalDto
        {
            MachineId = Guid.NewGuid().ToString().Substring(0, 8),
            Status = status.ToString(),
            SignalTime = DateTime.Now,
            ProgramName = programList[rand.Next(programList.Length)],
            SubProgramName = subProgramList[rand.Next(subProgramList.Length)]
        };
    }

    /// <summary>
    /// 將 DTO 轉為 JSON 格式
    /// </summary>
    static string SerializeToJson(MachineSignalDto dto)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };
        return JsonSerializer.Serialize(dto, options);
    }

    static async Task Main(string[] args)
    {
        // 建立 Managed MQTT 客戶端
        var factory = new MqttFactory();
        var mqttClient = factory.CreateManagedMqttClient();

        // 註冊訊息接收事件
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
            try
            {
                // 反序列化並顯示內容
                var dto = JsonSerializer.Deserialize<MachineSignalDto>(payload);
                Console.WriteLine($"[收到訊息] Topic: {e.ApplicationMessage.Topic}");
                Console.WriteLine($"  機台編號: {dto?.MachineId}");
                Console.WriteLine($"  狀態: {dto?.Status}");
                Console.WriteLine($"  訊號時間: {dto?.SignalTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  主程式名稱: {dto?.ProgramName}");
                Console.WriteLine($"  子程式名稱: {dto?.SubProgramName}");
            }
            catch
            {
                Console.WriteLine($"[收到訊息] Topic: {e.ApplicationMessage.Topic}, Payload: {payload}");
            }
            return Task.CompletedTask;
        };

        try
        {
            // 建立連線選項 (使用 MqttClientOptionsBuilder)
            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId("dotnet8_demo_client")
                .WithTcpServer("172.20.10.152", 1883)
                .Build();
            var options = new ManagedMqttClientOptions { ClientOptions = clientOptions };

            // 連線至 MQTT Broker
            await mqttClient.StartAsync(options);
            Console.WriteLine("已連線到 MQTT Broker");

            // 訂閱主題
            await mqttClient.SubscribeAsync("shinmold/machine-signal/all");
            Console.WriteLine("已訂閱 shinmold/machine-signal/all");

            // 每5秒發佈一次訊息，直到按下任意鍵
            var cts = new CancellationTokenSource();
            var publishTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var signal = GenerateRandomSignal();
                    var json = SerializeToJson(signal);

                    var managedMessage = new ManagedMqttApplicationMessage
                    {
                        ApplicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("shinmold/machine-signal/all")
                            .WithPayload(Encoding.UTF8.GetBytes(json))
                            .Build()
                    };

                    await mqttClient.EnqueueAsync(managedMessage);
                    Console.WriteLine($"[定時發佈] {DateTime.Now:HH:mm:ss} 已發佈訊息");
                    await Task.Delay(5000, cts.Token);
                }
            });

            Console.WriteLine("等待訊息... 按任意鍵結束");
            Console.ReadKey();
            cts.Cancel();
            await publishTask;

            // 斷線
            await mqttClient.StopAsync();
            Console.WriteLine("已斷線");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發生例外：{ex.Message}");
        }
        /*
         * Main 函式流程說明：
         * 1. 亂數產生 MachineSignalDto 實體並序列化為 JSON。
         * 2. 連線至 MQTT Broker，訂閱主題並發佈訊息。
         * 3. 接收訊息後反序列化並顯示內容。
         * 4. 等待訊息，按任意鍵後斷線。
         */
    }
}
