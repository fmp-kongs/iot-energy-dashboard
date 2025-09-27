using MQTTnet;
using MQTTnet.Client;
using EnergyBackend.Domain.Models;
using EnergyBackend.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EnergyBackend.Services;

public class MqttService
{
    private readonly IMqttClient _mqttClient;
    private readonly InfluxDbService _influxDbService;
    private readonly IHubContext<AlertsHub> _hubContext;
    private readonly MqttClientOptions _options;
    private readonly AiService _aiService;

    public MqttService(InfluxDbService influxDbService, IHubContext<AlertsHub> hubContext)
    {
        _influxDbService = influxDbService;
        _hubContext = hubContext;
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        _aiService = new AiService();

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer("47ca8b74c12949529a67a9f78cef8f10.s1.eu.hivemq.cloud", 8883) // HiveMQ Cloud host and TLS port
            .WithCredentials("hivemq.webclient.1758909235193", "jYlCB8H@1.#?y9Fb5Gas")
            .WithCleanSession()
            .WithTls() // important for secure connection
            .Build();

        _mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("✅ Connected to HiveMQ Cloud broker!");
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("telemetry/device/+")
                .Build());
        };

        _mqttClient.DisconnectedAsync += async e =>
        {
            Console.WriteLine("⚠️ Disconnected from HiveMQ Cloud broker!");
            // Optionally implement reconnection logic here
            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await _mqttClient.ConnectAsync(_options, CancellationToken.None);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"❌ Reconnection failed: {ex.Message}");
            }
        };

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                Console.WriteLine($"📩 Message received: {payload}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var telemetry = JsonSerializer.Deserialize<DeviceTelemetry>(payload, options);
                if (telemetry != null)
                {
                    // Save to InfluxDB
                    await _influxDbService.WriteTelemetryAsync(
                        telemetry.DeviceIdentifier,
                        telemetry.Timestamp,
                        telemetry.Voltage,
                        telemetry.Current,
                        telemetry.Power
                    );

                    // Broadcast to all SignalR clients
                    await _hubContext.Clients.All.SendAsync("ReceiveTelemetry", telemetry);
                    Console.WriteLine($"🚀 Telemetry broadcasted via SignalR for device {telemetry.DeviceIdentifier}");

                    // Check for anomalies and send alerts (AI-enhanced)
                    await CheckForAnomalies(telemetry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");
            }
        };
    }

    public async Task ConnectAsync()
    {
        await _mqttClient.ConnectAsync(_options, CancellationToken.None);
    }

    public async Task DisconnectAsync()
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
        }
    }

    private async Task CheckForAnomalies(DeviceTelemetry telemetry)
    {
        // Feed data to AI service for continuous learning
        _aiService.AddTelemetryData(telemetry);

        // Use AI service for comprehensive anomaly detection
        var aiAnomalies = _aiService.DetectAnomalies(telemetry);
        
        // Send AI-detected anomalies as alerts
        foreach (var anomaly in aiAnomalies)
        {
            var alert = new
            {
                deviceId = telemetry.DeviceIdentifier,
                type = anomaly.Type,
                method = anomaly.Method,
                message = anomaly.Message,
                timestamp = telemetry.Timestamp,
                severity = anomaly.Severity,
                score = anomaly.Score
            };

            await _hubContext.Clients.All.SendAsync("receivealert", alert);
            Console.WriteLine($"🤖 AI Anomaly detected - {anomaly.Method}: {anomaly.Message} (Score: {anomaly.Score:F2})");
        }

        // Log AI service status periodically
        if (DateTime.Now.Second % 30 == 0) // Every 30 seconds
        {
            Console.WriteLine($"📊 AI Service Status - Data Points: {_aiService.GetHistoricalDataCount()}, Model Trained: {_aiService.IsModelTrained()}");
        }
    }
}
