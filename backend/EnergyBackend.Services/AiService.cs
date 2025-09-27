using Microsoft.ML;
using Microsoft.ML.Data;
using EnergyBackend.Domain.Models;

namespace EnergyBackend.Services;

public class TelemetryData
{
    public float Voltage { get; set; }
    public float Current { get; set; }
    public float Power { get; set; }
    public float PowerFactor { get; set; }
    public float Efficiency { get; set; }
}

public class PowerPrediction
{
    [ColumnName("Score")]
    public float PredictedPower { get; set; }
}

public class AnomalyResult
{
    public bool IsAnomaly { get; set; }
    public double Score { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "low";
    public string Method { get; set; } = string.Empty;
}

public class TelemetryStats
{
    public double Mean { get; set; }
    public double StdDev { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Q1 { get; set; }
    public double Q3 { get; set; }
}

public class AiService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly List<TelemetryData> _historicalData;
    private readonly int _maxHistorySize = 1000;
    private readonly int _minTrainingSize = 50;
    private DateTime _lastTraining = DateTime.MinValue;
    private readonly TimeSpan _retrainingInterval = TimeSpan.FromMinutes(30);

    public AiService()
    {
        _mlContext = new MLContext(seed: 42);
        _model = null;
        _historicalData = new List<TelemetryData>();
    }

    public void AddTelemetryData(DeviceTelemetry telemetry)
    {
        var data = new TelemetryData
        {
            Voltage = (float)telemetry.Voltage,
            Current = (float)telemetry.Current,
            Power = (float)telemetry.Power,
            PowerFactor = (float)(telemetry.Power / (telemetry.Voltage * telemetry.Current)),
            Efficiency = (float)(telemetry.Power / (telemetry.Voltage * telemetry.Current) * 100)
        };

        _historicalData.Add(data);

        // Maintain sliding window
        if (_historicalData.Count > _maxHistorySize)
        {
            _historicalData.RemoveAt(0);
        }

        // Auto-train model if needed
        TrainModelIfReady();
    }

    public List<AnomalyResult> DetectAnomalies(DeviceTelemetry telemetry)
    {
        var anomalies = new List<AnomalyResult>();

        if (_historicalData.Count < 10) // Need minimum data for statistics
            return anomalies;

        // Statistical anomaly detection
        anomalies.AddRange(DetectStatisticalAnomalies(telemetry));

        // ML-based anomaly detection
        var mlAnomaly = DetectMLAnomaly(telemetry);
        if (mlAnomaly != null)
            anomalies.Add(mlAnomaly);

        return anomalies;
    }

    private List<AnomalyResult> DetectStatisticalAnomalies(DeviceTelemetry telemetry)
    {
        var anomalies = new List<AnomalyResult>();

        // Z-score based detection for each metric
        var voltageStats = CalculateStats(_historicalData.Select(d => (double)d.Voltage));
        var currentStats = CalculateStats(_historicalData.Select(d => (double)d.Current));
        var powerStats = CalculateStats(_historicalData.Select(d => (double)d.Power));

        // Voltage anomaly detection
        var voltageZScore = Math.Abs((telemetry.Voltage - voltageStats.Mean) / voltageStats.StdDev);
        if (voltageZScore > 2.5)
        {
            anomalies.Add(new AnomalyResult
            {
                IsAnomaly = true,
                Score = voltageZScore,
                Type = "Voltage Anomaly",
                Method = "Z-Score Statistical",
                Message = $"Voltage {telemetry.Voltage:F1}V is {voltageZScore:F1} standard deviations from normal ({voltageStats.Mean:F1}±{voltageStats.StdDev:F1}V)",
                Severity = voltageZScore > 3.5 ? "high" : "medium"
            });
        }

        // Current anomaly detection
        var currentZScore = Math.Abs((telemetry.Current - currentStats.Mean) / currentStats.StdDev);
        if (currentZScore > 2.5)
        {
            anomalies.Add(new AnomalyResult
            {
                IsAnomaly = true,
                Score = currentZScore,
                Type = "Current Anomaly",
                Method = "Z-Score Statistical",
                Message = $"Current {telemetry.Current:F1}A is {currentZScore:F1} standard deviations from normal ({currentStats.Mean:F1}±{currentStats.StdDev:F1}A)",
                Severity = currentZScore > 3.5 ? "high" : "medium"
            });
        }

        // Power anomaly detection
        var powerZScore = Math.Abs((telemetry.Power - powerStats.Mean) / powerStats.StdDev);
        if (powerZScore > 2.5)
        {
            anomalies.Add(new AnomalyResult
            {
                IsAnomaly = true,
                Score = powerZScore,
                Type = "Power Anomaly",
                Method = "Z-Score Statistical",
                Message = $"Power {telemetry.Power:F1}W is {powerZScore:F1} standard deviations from normal ({powerStats.Mean:F1}±{powerStats.StdDev:F1}W)",
                Severity = powerZScore > 3.5 ? "high" : "medium"
            });
        }

        // IQR-based detection for power
        var powerIQR = powerStats.Q3 - powerStats.Q1;
        var powerLowerBound = powerStats.Q1 - 1.5 * powerIQR;
        var powerUpperBound = powerStats.Q3 + 1.5 * powerIQR;
        
        if (telemetry.Power < powerLowerBound || telemetry.Power > powerUpperBound)
        {
            var distance = Math.Min(Math.Abs(telemetry.Power - powerLowerBound), Math.Abs(telemetry.Power - powerUpperBound));
            anomalies.Add(new AnomalyResult
            {
                IsAnomaly = true,
                Score = distance,
                Type = "Power Outlier",
                Method = "IQR Statistical",
                Message = $"Power {telemetry.Power:F1}W is an outlier (Normal range: {powerLowerBound:F1}-{powerUpperBound:F1}W)",
                Severity = distance > powerIQR * 2 ? "high" : "medium"
            });
        }

        return anomalies;
    }

    private AnomalyResult? DetectMLAnomaly(DeviceTelemetry telemetry)
    {
        if (_model == null || _historicalData.Count < _minTrainingSize)
            return null;

        try
        {
            var predictedPower = PredictPower((float)telemetry.Voltage, (float)telemetry.Current);
            var predictionError = Math.Abs(telemetry.Power - predictedPower);
            var relativeError = predictionError / Math.Max(predictedPower, 1); // Avoid division by zero

            if (relativeError > 0.15) // 15% prediction error threshold
            {
                return new AnomalyResult
                {
                    IsAnomaly = true,
                    Score = relativeError,
                    Type = "ML Power Prediction Anomaly",
                    Method = "Machine Learning",
                    Message = $"Power {telemetry.Power:F1}W deviates {relativeError:P1} from ML prediction {predictedPower:F1}W",
                    Severity = relativeError > 0.30 ? "high" : "medium"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ML anomaly detection error: {ex.Message}");
        }

        return null;
    }

    private TelemetryStats CalculateStats(IEnumerable<double> values)
    {
        var sortedValues = values.OrderBy(v => v).ToArray();
        var mean = sortedValues.Average();
        var variance = sortedValues.Select(v => Math.Pow(v - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        
        var q1Index = (int)(sortedValues.Length * 0.25);
        var q3Index = (int)(sortedValues.Length * 0.75);
        
        return new TelemetryStats
        {
            Mean = mean,
            StdDev = stdDev,
            Min = sortedValues.First(),
            Max = sortedValues.Last(),
            Q1 = sortedValues[q1Index],
            Q3 = sortedValues[q3Index]
        };
    }

    private void TrainModelIfReady()
    {
        if (_historicalData.Count >= _minTrainingSize && 
            (DateTime.Now - _lastTraining) > _retrainingInterval)
        {
            try
            {
                TrainModel(_historicalData);
                _lastTraining = DateTime.Now;
                Console.WriteLine($"✅ AI model retrained with {_historicalData.Count} data points");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Model training failed: {ex.Message}");
            }
        }
    }

    public void TrainModel(IEnumerable<TelemetryData> data)
    {
        var trainingData = _mlContext.Data.LoadFromEnumerable(data);
        var pipeline = _mlContext.Transforms.Concatenate("Features", new[] { "Voltage", "Current", "PowerFactor", "Efficiency" })
            .Append(_mlContext.Regression.Trainers.FastTree(labelColumnName: "Power", numberOfTrees: 100));
        _model = pipeline.Fit(trainingData);
    }

    public float PredictPower(float voltage, float current)
    {
        if (_model == null)
            throw new InvalidOperationException("Model has not been trained yet.");
            
        var powerFactor = current > 0 ? (voltage * current) / Math.Max(voltage * current, 1) : 0;
        var efficiency = current > 0 ? ((voltage * current) / Math.Max(voltage * current, 1)) * 100 : 0;
        
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<TelemetryData, PowerPrediction>(_model);
        var input = new TelemetryData 
        { 
            Voltage = voltage, 
            Current = current,
            PowerFactor = (float)powerFactor,
            Efficiency = (float)efficiency
        };
        var prediction = predictionEngine.Predict(input);
        return prediction.PredictedPower;
    }

    public int GetHistoricalDataCount() => _historicalData.Count;
    public bool IsModelTrained() => _model != null;
}
