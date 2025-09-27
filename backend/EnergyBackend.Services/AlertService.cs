using EnergyBackend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EnergyBackend.Services;

public class AlertService
{
    private readonly InfluxDbService _influxDbService;
    private readonly AiService _aiService;
    private readonly IHubContext<AlertsHub> _hubContext;

    public AlertService(InfluxDbService influxDbService, AiService aiService, IHubContext<AlertsHub> hubContext)
    {
        _influxDbService = influxDbService;
        _aiService = aiService;
        _hubContext = hubContext;
    }

    public async Task CheckForAnomaliesAsync(string deviceId)
    {
        // Query recent telemetry data for the device
        var fluxQuery = $@"
            from(bucket: ""{_influxDbService.Bucket}"")
              |> range(start: -1h)
              |> filter(fn: (r) => r[""device_id""] == ""{deviceId}"")
              |> sort(columns: [""_time""], desc: true)
              |> limit(n: 100)
        ";
        var telemetryData = await _influxDbService.QueryTelemetryAsync(fluxQuery);
        if (telemetryData.Count < 10)
        {
            Console.WriteLine("Not enough data to analyze.");
            return;
        }
        // Prepare data for AI model
        var trainingData = telemetryData.Select(t => new TelemetryData
        {
            Voltage = (float)t.Voltage,
            Current = (float)t.Current,
            Power = (float)t.Power
        });
        // Train the AI model
        _aiService.TrainModel(trainingData);
        // Predict power for the latest data point
        var latest = telemetryData.OrderByDescending(t => t.Timestamp).First();
        var predictedPower = _aiService.PredictPower((float)latest.Voltage, (float)latest.Current);
        // Check for anomaly (e.g., if actual power deviates significantly from predicted)
        var deviation = Math.Abs(latest.Power - predictedPower);
        if (deviation / predictedPower > 0.2) // 20% deviation threshold
        {
            Console.WriteLine($"⚠️ Anomaly detected for device {deviceId}! Actual Power: {latest.Power}, Predicted Power: {predictedPower}");
            // Here you could integrate with an email service or notification system to alert the user
            await _hubContext.Clients.All.SendAsync("ReceiveAlert", $"Anomaly detected for device {deviceId}! Actual Power: {latest.Power}, Predicted Power: {predictedPower}");
        }
        else
        {
            Console.WriteLine($"✅ No anomaly detected for device {deviceId}. Actual Power: {latest.Power}, Predicted Power: {predictedPower}");
        }
    }
}
