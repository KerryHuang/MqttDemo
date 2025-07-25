using MqttDemo;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
/*
 * MQTTnet 5.x 已整合 Client 相關 API，無需額外引用 Options/Subscribing/Publishing 命名空間。
 * 下方 Main 函式示範 MQTT 客戶端連線、訂閱、發佈與斷線流程。
 */

using System.Text.Json;

class Program
{
    // 固定10台機台編號
    static readonly List<string> FixedMachineIds = Enumerable.Range(1, 10)
        .Select(i => $"Z{i:000}").ToList();

    /// <summary>
    /// 亂數產生多筆 MachineSignalDto 實體（從固定10台機台中隨機選取）
    /// </summary>
    static List<MachineSignalDto> GenerateRandomSignals(int count)
    {
        var programList = new[] { "MainProc", "AuxProc", "TestProc" };
        var subProgramList = new[] { "SubProcA", "SubProcB", "SubProcC" };
        var rand = new Random();
        var statusValues = Enum.GetValues(typeof(Status));
        var list = new List<MachineSignalDto>();
        for (int i = 0; i < count; i++)
        {
            var status = (Status)statusValues.GetValue(rand.Next(statusValues.Length));
            list.Add(new MachineSignalDto
            {
                MachineId = FixedMachineIds[rand.Next(FixedMachineIds.Count)],
                Status = status.ToString(),
                SignalTime = DateTime.Now,
                ProgramName = programList[rand.Next(programList.Length)],
                SubProgramName = subProgramList[rand.Next(subProgramList.Length)]
            });
        }
        return list;
    }

    /// <summary>
    /// 將 DTO 或 DTO 陣列轉為 JSON 格式
    /// </summary>
    static string SerializeToJson(object dto)
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
                // 嘗試反序列化為多筆資料
                var dtos = JsonSerializer.Deserialize<List<MachineSignalDto>>(payload);
                if (dtos != null)
                {
                    Console.WriteLine($"[收到訊息] Topic: {e.ApplicationMessage.Topic}，共 {dtos.Count} 筆");
                    foreach (var dto in dtos)
                    {
                        Console.WriteLine($"  機台編號: {dto?.MachineId}");
                        Console.WriteLine($"  狀態: {dto?.Status}");
                        Console.WriteLine($"  訊號時間: {dto?.SignalTime:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"  主程式名稱: {dto?.ProgramName}");
                        Console.WriteLine($"  子程式名稱: {dto?.SubProgramName}");
                        Console.WriteLine("------------------------");
                    }
                }
                else
                {
                    // 若不是 List，嘗試單筆
                    var dto = JsonSerializer.Deserialize<MachineSignalDto>(payload);
                    Console.WriteLine($"[收到訊息] Topic: {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"  機台編號: {dto?.MachineId}");
                    Console.WriteLine($"  狀態: {dto?.Status}");
                    Console.WriteLine($"  訊號時間: {dto?.SignalTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  主程式名稱: {dto?.ProgramName}");
                    Console.WriteLine($"  子程式名稱: {dto?.SubProgramName}");
                }
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
                .WithTcpServer("172.20.10.152", 1883) // WDMIS: 172.20.10.152, 景利  MQTT broker :192.168.1.237:1883, 鑫型  MQTT broker:   192.168.1.244:1883
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
                var rand = new Random();
                while (!cts.Token.IsCancellationRequested)
                {
                    // 每次亂數產生 1~5 筆訊號
                    int count = rand.Next(1, 6);
                    var signals = GenerateRandomSignals(count);
                    var json = SerializeToJson(signals);

                    var managedMessage = new ManagedMqttApplicationMessage
                    {
                        ApplicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("shinmold/machine-signal/all")
                            .WithPayload(Encoding.UTF8.GetBytes(json))
                            .Build()
                    };

                    await mqttClient.EnqueueAsync(managedMessage);
                    Console.WriteLine($"[定時發佈] {DateTime.Now:HH:mm:ss} 已發佈多筆訊息（{signals.Count} 筆）");
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
