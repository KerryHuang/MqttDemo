using MqttDemo;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;

/*
 * MP-468 宇均三色燈機連網 MQTT 連線驗證程式
 *
 * MQTT Broker: 172.16.1.23:20085
 * Topic 格式: {機台代碼}/module/towerlight (例如: A68/module/towerlight)
 *
 * 使用方式: dotnet run -- --towerlight
 */

public static class TowerlightTestProgram
{
    // JSON 序列化選項 (屬性名稱不區分大小寫)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 解析三色燈狀態
    /// </summary>
    public static TowerlightStatus ParseStatus(TowerlightData? data)
    {
        if (data?.Data == null) return TowerlightStatus.Unknown;

        var led1 = data.Data.FirstOrDefault(d => d.Sensor == "LED_ch1")?.Value?.FirstOrDefault();
        var led2 = data.Data.FirstOrDefault(d => d.Sensor == "LED_ch2")?.Value?.FirstOrDefault();
        var led3 = data.Data.FirstOrDefault(d => d.Sensor == "LED_ch3")?.Value?.FirstOrDefault();

        // 判斷燈號狀態 (value 格式: "0,0,0,1" 表示亮)
        bool isLed1On = led1?.EndsWith(",1") ?? false;
        bool isLed2On = led2?.EndsWith(",1") ?? false;
        bool isLed3On = led3?.EndsWith(",1") ?? false;

        // 根據三色燈定義判斷狀態
        if (isLed1On && !isLed2On && !isLed3On) return TowerlightStatus.Processing;
        if (!isLed1On && isLed2On && !isLed3On) return TowerlightStatus.Paused;
        if (!isLed1On && !isLed2On && isLed3On) return TowerlightStatus.Stopped;
        if (!isLed1On && !isLed2On && !isLed3On) return TowerlightStatus.Disconnected;

        return TowerlightStatus.Mixed;
    }

    /// <summary>
    /// 取得狀態描述文字
    /// </summary>
    public static string GetStatusDescription(TowerlightStatus status)
    {
        return status switch
        {
            TowerlightStatus.Processing => "加工中 (綠燈)",
            TowerlightStatus.Paused => "暫停中 (黃燈)",
            TowerlightStatus.Stopped => "停止中 (紅燈)",
            TowerlightStatus.Disconnected => "斷線中 (無燈)",
            TowerlightStatus.Mixed => "混合狀態",
            _ => "未知"
        };
    }

    /// <summary>
    /// 執行三色燈連線測試
    /// </summary>
    public static async Task RunAsync(int waitSeconds = 30)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  MP-468 宇均三色燈機連網 MQTT 連線驗證");
        Console.WriteLine("===========================================\n");

        var factory = new MqttFactory();
        var mqttClient = factory.CreateManagedMqttClient();

        // 統計資訊
        int messageCount = 0;
        var receivedMachines = new HashSet<string>();

        // 註冊連線狀態事件
        mqttClient.ConnectedAsync += e =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] MQTT 連線成功！");
            return Task.CompletedTask;
        };

        mqttClient.DisconnectedAsync += e =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] MQTT 連線中斷: {e.Exception?.Message ?? "未知原因"}");
            return Task.CompletedTask;
        };

        mqttClient.ConnectingFailedAsync += e =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] MQTT 連線失敗: {e.Exception?.Message ?? "未知原因"}");
            return Task.CompletedTask;
        };

        // 註冊訊息發送成功事件
        mqttClient.ApplicationMessageProcessedAsync += e =>
        {
            if (e.Exception == null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 訊息發送成功: {e.ApplicationMessage?.ApplicationMessage?.Topic}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 訊息發送失敗: {e.Exception.Message}");
            }
            return Task.CompletedTask;
        };

        // 註冊訊息接收事件
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            messageCount++;
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());

            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 收到訊息 #{messageCount}");
            Console.WriteLine($"  Topic: {topic}");

            // 從 topic 取得機台代碼 (例如 A68/module/towerlight -> A68)
            var topicParts = topic.Split('/');
            var machineCode = topicParts.Length > 0 ? topicParts[0] : "未知";
            var machineName = TowerlightMachineMapping.GetMachineName(machineCode);
            receivedMachines.Add(machineCode);

            Console.WriteLine($"  機台代碼: {machineCode}");
            Console.WriteLine($"  機台名稱: {machineName}");

            try
            {
                var data = JsonSerializer.Deserialize<TowerlightData>(payload, JsonOptions);
                if (data != null)
                {
                    Console.WriteLine($"  三色燈ID: {data.Id}");
                    Console.WriteLine($"  製造商ID: {data.ManufacturerId}");

                    var status = ParseStatus(data);
                    Console.WriteLine($"  狀態判定: {GetStatusDescription(status)}");

                    if (data.Data != null)
                    {
                        Console.WriteLine("  感測器資料:");
                        foreach (var sensor in data.Data)
                        {
                            var valueStr = sensor.Value != null ? string.Join(", ", sensor.Value) : "無";
                            Console.WriteLine($"    - {sensor.Label}: {valueStr}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  原始資料: {payload}");
                Console.WriteLine($"  解析錯誤: {ex.Message}");
            }

            Console.WriteLine("  -------------------------------------------");
            return Task.CompletedTask;
        };

        try
        {
            // 宇均 MQTT Broker 設定
            var brokerHost = "172.16.1.22";
            var brokerPort = 20085;
            var clientId = $"mp468-towerlight-test-{Guid.NewGuid():N}";

            Console.WriteLine($"MQTT Broker: {brokerHost}:{brokerPort}");
            Console.WriteLine($"Client ID: {clientId}");
            Console.WriteLine();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(brokerHost, brokerPort)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithTimeout(TimeSpan.FromSeconds(10))
                .Build();

            var options = new ManagedMqttClientOptions
            {
                ClientOptions = clientOptions,
                AutoReconnectDelay = TimeSpan.FromSeconds(5)
            };

            Console.WriteLine("正在連線至 MQTT Broker...");
            await mqttClient.StartAsync(options);
            await Task.Delay(2000);

            // 訂閱所有三色燈主題
            await mqttClient.SubscribeAsync("+/module/towerlight");
            Console.WriteLine("已訂閱主題: +/module/towerlight (所有機台三色燈)");

            // 訂閱特定已知機台
            foreach (var machineCode in TowerlightMachineMapping.Machines.Keys)
            {
                await mqttClient.SubscribeAsync($"{machineCode}/module/towerlight");
            }
            Console.WriteLine($"已訂閱 {TowerlightMachineMapping.Machines.Count} 台已知機台的三色燈主題");

            Console.WriteLine($"\n===========================================");
            Console.WriteLine($"測試自己推送、自己接收 ({waitSeconds}秒)...");
            Console.WriteLine($"===========================================\n");

            // 發送測試訊息
            var testData = new TowerlightData
            {
                Id = "TEST-DEVICE-001",
                ManufacturerId = "test",
                Data = new List<TowerlightSensorData>
                {
                    new() { Sensor = "LED_ch1", Label = "LED_ch1", Value = new List<string> { "0,0,0,1" } },
                    new() { Sensor = "LED_ch2", Label = "LED_ch2", Value = new List<string> { "0,0,0,0" } },
                    new() { Sensor = "LED_ch3", Label = "LED_ch3", Value = new List<string> { "0,0,0,0" } },
                    new() { Sensor = "ioin", Label = "io", Value = new List<string> { "0,0,0,0" } }
                }
            };

            var testJson = JsonSerializer.Serialize(testData, JsonOptions);
            var testTopic = "TEST/module/towerlight";

            var testMessage = new ManagedMqttApplicationMessage
            {
                ApplicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(testTopic)
                    .WithPayload(Encoding.UTF8.GetBytes(testJson))
                    .Build()
            };

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 發送測試訊息至 {testTopic}...");
            await mqttClient.EnqueueAsync(testMessage);

            await Task.Delay(waitSeconds * 1000);

            // 顯示統計
            Console.WriteLine("\n===========================================");
            Console.WriteLine("  連線驗證結果");
            Console.WriteLine("===========================================");
            Console.WriteLine($"  總共收到訊息數: {messageCount}");
            Console.WriteLine($"  已收到資料的機台: {(receivedMachines.Count > 0 ? string.Join(", ", receivedMachines) : "無")}");
            Console.WriteLine($"  未收到資料的機台: {string.Join(", ", TowerlightMachineMapping.Machines.Keys.Except(receivedMachines))}");

            if (messageCount > 0)
            {
                Console.WriteLine("\n  MQTT 連線驗證成功！已成功收到三色燈資料。");
            }
            else
            {
                Console.WriteLine("\n  未收到任何三色燈資料，請檢查:");
                Console.WriteLine("    1. MQTT Broker 是否正常運作");
                Console.WriteLine("    2. 三色燈裝置是否正常發送資料");
                Console.WriteLine("    3. 網路連線是否正常");
            }

            await mqttClient.StopAsync();
            Console.WriteLine("\n已斷線");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n發生例外：{ex.Message}");
            Console.WriteLine($"  堆疊追蹤: {ex.StackTrace}");
        }

        Console.WriteLine("\n程式結束");
    }
}
