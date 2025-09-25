using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;

namespace EnergyBackend.Services;

public class InfluxDbService
{
    private readonly InfluxDBClient _influxDbClient;
    private readonly string _influxDbUrl;
    private readonly string _influxDbToken;
    private readonly string _influxDbOrg;
    private readonly string _influxDbBucket;

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
}
