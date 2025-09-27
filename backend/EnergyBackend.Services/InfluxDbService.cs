using EnergyBackend.Domain.Models;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;

namespace EnergyBackend.Services;

public class InfluxDbService
{
    private readonly InfluxDBClient _influxDbClient;
    private readonly string _influxDbOrg;
    private readonly string _influxDbBucket;

    public string Bucket => _influxDbBucket;

    // Constructor for dependency injection
    public InfluxDbService(IConfiguration config, InfluxDBClient client)
    {
        _influxDbClient = client;
        _influxDbOrg = config["InfluxDB:Org"];
        _influxDbBucket = config["InfluxDB:Bucket"];
    }

    // Write a telemetry point to InfluxDB
    public async Task WriteTelemetryAsync(string deviceId, DateTime timestamp, double voltage, double current, double power)
    {
        var writeApi = _influxDbClient.GetWriteApiAsync();

        var point = PointData
            .Measurement("device_telemetry")
            .Tag("device_id", deviceId)
            .Field("voltage", voltage)
            .Field("current", current)
            .Field("power", power)
            .Timestamp(timestamp, WritePrecision.Ns);

        await writeApi.WritePointAsync(point, _influxDbBucket, _influxDbOrg);
    }

    public async Task<List<DeviceTelemetry>> QueryTelemetryAsync(string fluxQuery)
    {
        var queryApi = _influxDbClient.GetQueryApi();
        var tables = await queryApi.QueryAsync(fluxQuery, _influxDbOrg);
        var results = new List<DeviceTelemetry>();
        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                results.Add(new DeviceTelemetry
                {
                    DeviceIdentifier = record.GetValueByKey("device_id")?.ToString(),
                    Timestamp = record.GetTimeInDateTime() ?? DateTime.MinValue,
                    Voltage = record.GetValueByKey("voltage") != null ? Convert.ToDouble(record.GetValueByKey("voltage")) : 0.0,
                    Current = record.GetValueByKey("current") != null ? Convert.ToDouble(record.GetValueByKey("current")) : 0.0,
                    Power = record.GetValueByKey("power") != null ? Convert.ToDouble(record.GetValueByKey("power")) : 0.0,
                    Energy = 0.0 // Placeholder, as energy is not stored in InfluxDB
                });
            }
        }
        return results;
    }
}
