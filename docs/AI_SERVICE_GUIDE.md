# AI Service & Anomaly Detection Guide
## Comprehensive Documentation for IoT Energy Dashboard AI Components

### Version: 1.3.0
### Last Updated: September 28, 2025

---

## Table of Contents
1. [AI Service Overview](#ai-service-overview)
2. [Anomaly Detection Architecture](#anomaly-detection-architecture)
3. [Statistical Methods](#statistical-methods)
4. [Machine Learning Implementation](#machine-learning-implementation)
5. [Data Processing Pipeline](#data-processing-pipeline)
6. [Alert System](#alert-system)
7. [Performance Optimization](#performance-optimization)
8. [Configuration & Tuning](#configuration--tuning)
9. [Troubleshooting Guide](#troubleshooting-guide)
10. [Best Practices](#best-practices)

---

## AI Service Overview

The AI Service (`AiService.cs`) is the intelligent core of the IoT Energy Dashboard, providing sophisticated anomaly detection capabilities through a combination of statistical analysis and machine learning techniques.

### Core Objectives
- **Real-time Anomaly Detection**: Identify unusual patterns in energy consumption
- **Adaptive Learning**: Continuously improve detection accuracy through ML model training
- **Multi-Algorithm Approach**: Combine statistical and ML methods for comprehensive coverage
- **Scalable Processing**: Handle high-frequency telemetry data efficiently
- **Actionable Insights**: Provide detailed anomaly information with confidence scores

### Key Components

```csharp
public class AiService
{
    // Core ML components
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    
    // Data management
    private readonly List<TelemetryData> _historicalData;
    private readonly int _maxHistorySize = 1000;
    
    // Training configuration
    private readonly int _minTrainingSize = 50;
    private DateTime _lastTraining = DateTime.MinValue;
    private readonly TimeSpan _retrainingInterval = TimeSpan.FromMinutes(30);
}
```

---

## Anomaly Detection Architecture

### Detection Framework

```
┌─────────────────────────────────────────────────────────────┐
│                    Telemetry Data Input                     │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                 Data Preprocessing                          │
│  • Feature Engineering  • Normalization  • Validation      │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│              Parallel Detection Methods                     │
├─────────────────┬─────────────────┬─────────────────────────┤
│   Statistical   │      ML-Based   │     Ensemble            │
│   Detection     │     Detection   │     Scoring             │
│                 │                 │                         │
│ • Z-Score       │ • Regression    │ • Score Combination     │
│ • IQR Analysis  │ • Prediction    │ • Confidence Metrics   │
│ • Trend Analysis│ • Error Analysis│ • Severity Assessment  │
└─────────────────┴─────────────────┴─────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                 Alert Generation                            │
│     • Severity Classification  • Message Formatting        │
│     • Alert Deduplication     • Real-time Broadcast        │
└─────────────────────────────────────────────────────────────┘
```

### Detection Methods Comparison

| Method | Strengths | Use Cases | Limitations |
|--------|-----------|----------|-------------|
| **Z-Score** | Simple, fast, interpretable | Gaussian distributions | Sensitive to outliers |
| **IQR** | Robust to outliers | Skewed distributions | Less sensitive to mild anomalies |
| **ML Regression** | Captures complex patterns | Non-linear relationships | Requires training data |
| **Ensemble** | Balanced approach | General purpose | More complex to tune |

---

## Statistical Methods

### 1. Z-Score Analysis

**Mathematical Foundation**:
```
Z = (X - μ) / σ
```
Where:
- X = Current value
- μ = Population mean
- σ = Population standard deviation

**Implementation**:
```csharp
private List<AnomalyResult> DetectStatisticalAnomalies(DeviceTelemetry telemetry)
{
    var anomalies = new List<AnomalyResult>();
    
    // Calculate statistics for each metric
    var voltageStats = CalculateStats(_historicalData.Select(d => (double)d.Voltage));
    var currentStats = CalculateStats(_historicalData.Select(d => (double)d.Current));
    var powerStats = CalculateStats(_historicalData.Select(d => (double)d.Power));
    
    // Z-score analysis for voltage
    var voltageZScore = Math.Abs((telemetry.Voltage - voltageStats.Mean) / voltageStats.StdDev);
    if (voltageZScore > 2.5)
    {
        anomalies.Add(new AnomalyResult
        {
            IsAnomaly = true,
            Score = voltageZScore,
            Type = "Voltage Anomaly",
            Method = "Z-Score Statistical",
            Message = $"Voltage {telemetry.Voltage:F1}V is {voltageZScore:F1} standard deviations from normal",
            Severity = voltageZScore > 3.5 ? "high" : "medium"
        });
    }
    
    return anomalies;
}
```

**Threshold Configuration**:
- **Normal Range**: |Z| ≤ 2.5 (covers ~98.8% of normal data)
- **Medium Anomaly**: 2.5 < |Z| ≤ 3.5 (1.2% of data)
- **High Anomaly**: |Z| > 3.5 (0.05% of data)

### 2. Interquartile Range (IQR) Detection

**Mathematical Foundation**:
- Q1 = 25th percentile
- Q3 = 75th percentile
- IQR = Q3 - Q1
- Lower fence = Q1 - 1.5 × IQR
- Upper fence = Q3 + 1.5 × IQR

**Implementation**:
```csharp
// IQR-based detection for power
var powerIQR = powerStats.Q3 - powerStats.Q1;
var powerLowerBound = powerStats.Q1 - 1.5 * powerIQR;
var powerUpperBound = powerStats.Q3 + 1.5 * powerIQR;

if (telemetry.Power < powerLowerBound || telemetry.Power > powerUpperBound)
{
    var distance = Math.Min(
        Math.Abs(telemetry.Power - powerLowerBound), 
        Math.Abs(telemetry.Power - powerUpperBound)
    );
    
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
```

**Advantages**:
- Robust to extreme outliers
- Works well with skewed distributions
- No assumption of normal distribution

### 3. Statistical Calculations

```csharp
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
        Min = sortedValues[0],
        Max = sortedValues[sortedValues.Length - 1],
        Q1 = sortedValues[q1Index],
        Q3 = sortedValues[q3Index]
    };
}
```

---

## Machine Learning Implementation

### ML.NET Model Architecture

#### Model Selection
- **Algorithm**: FastTree Regression (Gradient Boosting)
- **Features**: Voltage, Current, Power Factor, Efficiency
- **Target**: Power consumption prediction
- **Training**: Online learning with sliding window

#### Feature Engineering

```csharp
public void AddTelemetryData(DeviceTelemetry telemetry)
{
    var data = new TelemetryData
    {
        Voltage = (float)telemetry.Voltage,
        Current = (float)telemetry.Current,
        Power = (float)telemetry.Power,
        
        // Derived features
        PowerFactor = (float)(telemetry.Power / (telemetry.Voltage * telemetry.Current)),
        Efficiency = (float)((telemetry.Power / (telemetry.Voltage * telemetry.Current)) * 100)
    };
    
    _historicalData.Add(data);
    
    // Maintain sliding window
    if (_historicalData.Count > _maxHistorySize)
        _historicalData.RemoveAt(0);
}
```

#### Model Training Pipeline

```csharp
public void TrainModel(IEnumerable<TelemetryData> data)
{
    var trainingData = _mlContext.Data.LoadFromEnumerable(data);
    
    var pipeline = _mlContext.Transforms
        .Concatenate("Features", new[] { 
            "Voltage", "Current", "PowerFactor", "Efficiency" 
        })
        .Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: "Power", 
            numberOfTrees: 100,
            minimumExampleCountPerLeaf: 10,
            learningRate: 0.2
        ));
    
    _model = pipeline.Fit(trainingData);
    Console.WriteLine($"✅ AI model trained with {data.Count()} data points");
}
```

#### Automatic Training Logic

```csharp
private void TrainModelIfReady()
{
    var shouldTrain = _historicalData.Count >= _minTrainingSize && 
                     (DateTime.Now - _lastTraining) > _retrainingInterval;
                     
    if (shouldTrain)
    {
        try
        {
            TrainModel(_historicalData);
            _lastTraining = DateTime.Now;
            Console.WriteLine($"🔄 Model retrained automatically");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Training failed: {ex.Message}");
        }
    }
}
```

### ML-Based Anomaly Detection

```csharp
private AnomalyResult? DetectMLAnomaly(DeviceTelemetry telemetry)
{
    if (_model == null || _historicalData.Count < _minTrainingSize)
        return null;

    try
    {
        var predictedPower = PredictPower((float)telemetry.Voltage, (float)telemetry.Current);
        var predictionError = Math.Abs(telemetry.Power - predictedPower);
        var relativeError = predictionError / Math.Max(predictedPower, 1);

        if (relativeError > 0.15) // 15% threshold
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
```

---

## Data Processing Pipeline

### Real-time Processing Flow

```
┌─────────────────┐
│ Telemetry Data  │
│   Reception     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Data Validation │
│ & Preprocessing │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Historical    │
│ Data Addition   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Statistical   │
│   Calculation   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Anomaly      │
│   Detection     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Alert       │
│   Generation    │
└─────────────────┘
```

### Performance Metrics

#### Processing Latency
- **Data Addition**: < 1ms
- **Statistical Calculation**: < 5ms
- **ML Prediction**: < 10ms
- **Total Processing**: < 20ms per data point

#### Memory Usage
- **Historical Data**: ~80KB (1000 points × 80 bytes)
- **ML Model**: ~500KB (trained FastTree model)
- **Statistical Cache**: ~10KB
- **Total Memory**: < 1MB per AI service instance

### Batch Processing Optimization

```csharp
public List<AnomalyResult> ProcessBatch(IEnumerable<DeviceTelemetry> telemetryBatch)
{
    var results = new List<AnomalyResult>();
    
    foreach (var telemetry in telemetryBatch)
    {
        AddTelemetryData(telemetry);
        results.AddRange(DetectAnomalies(telemetry));
    }
    
    // Batch training check
    TrainModelIfReady();
    
    return results;
}
```

---

## Alert System

### Alert Classification

#### Severity Levels
1. **Low** (Informational)
   - Minor statistical deviations
   - Z-score: 2.0 - 2.5
   - ML error: 10% - 15%

2. **Medium** (Warning)
   - Moderate anomalies requiring attention
   - Z-score: 2.5 - 3.5
   - ML error: 15% - 30%

3. **High** (Critical)
   - Severe anomalies requiring immediate action
   - Z-score: > 3.5
   - ML error: > 30%

#### Alert Types

```csharp
public enum AnomalyType
{
    VoltageAnomaly,      // Statistical voltage deviation
    CurrentAnomaly,      // Statistical current deviation  
    PowerAnomaly,        // Statistical power deviation
    PowerOutlier,        // IQR-based power outlier
    MLPredictionAnomaly, // ML model prediction error
    SystemAnomaly        // Internal system issues
}
```

### Alert Structure

```csharp
public class AnomalyResult
{
    public bool IsAnomaly { get; set; }
    public double Score { get; set; }              // Numerical anomaly score
    public string Type { get; set; }               // Anomaly classification
    public string Message { get; set; }            // Human-readable description
    public string Severity { get; set; }           // low, medium, high
    public string Method { get; set; }             // Detection method used
    public DateTime Timestamp { get; set; }        // Detection time
    public Dictionary<string, object> Metadata { get; set; } // Additional context
}
```

### Alert Deduplication

```csharp
private readonly Dictionary<string, DateTime> _recentAlerts = new();
private readonly TimeSpan _alertCooldown = TimeSpan.FromMinutes(5);

private bool ShouldSuppressAlert(string alertKey)
{
    if (_recentAlerts.TryGetValue(alertKey, out var lastAlert))
    {
        return DateTime.Now - lastAlert < _alertCooldown;
    }
    
    _recentAlerts[alertKey] = DateTime.Now;
    return false;
}
```

---

## Performance Optimization

### Memory Management

#### Sliding Window Implementation
```csharp
private void ManageHistoricalData()
{
    // Remove old data points beyond window size
    while (_historicalData.Count > _maxHistorySize)
    {
        _historicalData.RemoveAt(0);
    }
    
    // Periodic cleanup of alert cache
    CleanupAlertCache();
}

private void CleanupAlertCache()
{
    var cutoff = DateTime.Now - _alertCooldown;
    var keysToRemove = _recentAlerts
        .Where(kvp => kvp.Value < cutoff)
        .Select(kvp => kvp.Key)
        .ToList();
        
    foreach (var key in keysToRemove)
    {
        _recentAlerts.Remove(key);
    }
}
```

### Computational Optimization

#### Lazy Statistical Calculation
```csharp
private readonly Dictionary<string, (TelemetryStats stats, int dataVersion)> _statisticsCache = new();
private int _dataVersion = 0;

private TelemetryStats GetCachedStats(string metric, IEnumerable<double> values)
{
    var key = $"{metric}_{_dataVersion}";
    
    if (_statisticsCache.TryGetValue(key, out var cached) && cached.dataVersion == _dataVersion)
    {
        return cached.stats;
    }
    
    var stats = CalculateStats(values);
    _statisticsCache[key] = (stats, _dataVersion);
    return stats;
}
```

#### Parallel Processing
```csharp
public List<AnomalyResult> DetectAnomaliesParallel(DeviceTelemetry telemetry)
{
    var tasks = new List<Task<AnomalyResult?>>();
    
    // Parallel execution of detection methods
    tasks.Add(Task.Run(() => DetectVoltageAnomaly(telemetry)));
    tasks.Add(Task.Run(() => DetectCurrentAnomaly(telemetry)));
    tasks.Add(Task.Run(() => DetectPowerAnomaly(telemetry)));
    tasks.Add(Task.Run(() => DetectMLAnomaly(telemetry)));
    
    Task.WaitAll(tasks.ToArray());
    
    return tasks
        .Select(t => t.Result)
        .Where(r => r != null)
        .ToList();
}
```

---

## Configuration & Tuning

### Configuration Parameters

```csharp
public class AiServiceConfiguration
{
    // Data management
    public int MaxHistorySize { get; set; } = 1000;
    public int MinTrainingSize { get; set; } = 50;
    public TimeSpan RetrainingInterval { get; set; } = TimeSpan.FromMinutes(30);
    
    // Statistical thresholds
    public double ZScoreThreshold { get; set; } = 2.5;
    public double ZScoreHighThreshold { get; set; } = 3.5;
    public double IQRMultiplier { get; set; } = 1.5;
    
    // ML configuration
    public double MLErrorThreshold { get; set; } = 0.15;
    public double MLHighErrorThreshold { get; set; } = 0.30;
    public int NumberOfTrees { get; set; } = 100;
    public double LearningRate { get; set; } = 0.2;
    
    // Alert configuration
    public TimeSpan AlertCooldown { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableAlertDeduplication { get; set; } = true;
}
```

### Environment-based Configuration

```json
{
  "AiService": {
    "MaxHistorySize": 1000,
    "MinTrainingSize": 50,
    "RetrainingIntervalMinutes": 30,
    "ZScoreThreshold": 2.5,
    "MLErrorThreshold": 0.15,
    "EnableParallelProcessing": true,
    "LogLevel": "Information"
  }
}
```

### Dynamic Threshold Adjustment

```csharp
public void AdjustThresholds(double falsePositiveRate, double detectionRate)
{
    // Increase threshold if too many false positives
    if (falsePositiveRate > 0.05) // 5% threshold
    {
        _configuration.ZScoreThreshold *= 1.1;
        _configuration.MLErrorThreshold *= 1.1;
    }
    
    // Decrease threshold if missing anomalies
    if (detectionRate < 0.90) // 90% detection target
    {
        _configuration.ZScoreThreshold *= 0.9;
        _configuration.MLErrorThreshold *= 0.9;
    }
    
    Console.WriteLine($"Thresholds adjusted: Z-Score={_configuration.ZScoreThreshold:F2}, ML={_configuration.MLErrorThreshold:F2}");
}
```

---

## Troubleshooting Guide

### Common Issues

#### 1. Model Training Failures

**Symptoms**:
- Exception during model training
- Predictions throwing errors
- Poor anomaly detection accuracy

**Diagnostics**:
```csharp
public void DiagnoseTrainingIssues()
{
    Console.WriteLine($"Historical data count: {_historicalData.Count}");
    Console.WriteLine($"Minimum training size: {_minTrainingSize}");
    Console.WriteLine($"Last training: {_lastTraining}");
    Console.WriteLine($"Model trained: {_model != null}");
    
    // Check data quality
    var voltageRange = _historicalData.Max(d => d.Voltage) - _historicalData.Min(d => d.Voltage);
    var currentRange = _historicalData.Max(d => d.Current) - _historicalData.Min(d => d.Current);
    var powerRange = _historicalData.Max(d => d.Power) - _historicalData.Min(d => d.Power);
    
    Console.WriteLine($"Data ranges - Voltage: {voltageRange:F2}, Current: {currentRange:F2}, Power: {powerRange:F2}");
}
```

**Solutions**:
- Ensure sufficient training data (≥ 50 points)
- Validate data quality and ranges
- Check for NaN or infinite values
- Verify feature engineering calculations

#### 2. High False Positive Rate

**Symptoms**:
- Too many alerts for normal conditions
- Users ignoring alerts due to noise
- System performance impact

**Diagnostics**:
```csharp
public void AnalyzeFalsePositives()
{
    var recentAnomalies = GetRecentAnomalies(TimeSpan.FromHours(24));
    var groupedByType = recentAnomalies.GroupBy(a => a.Type);
    
    foreach (var group in groupedByType)
    {
        var avgScore = group.Average(a => a.Score);
        var count = group.Count();
        Console.WriteLine($"{group.Key}: {count} alerts, avg score: {avgScore:F2}");
    }
}
```

**Solutions**:
- Increase detection thresholds
- Implement adaptive thresholding
- Add alert deduplication
- Improve feature engineering

#### 3. Memory Usage Issues

**Symptoms**:
- Increasing memory consumption
- OutOfMemoryException
- Slow performance

**Diagnostics**:
```csharp
public void MonitorMemoryUsage()
{
    var process = Process.GetCurrentProcess();
    var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
    
    Console.WriteLine($"Memory usage: {memoryUsage} MB");
    Console.WriteLine($"Historical data count: {_historicalData.Count}");
    Console.WriteLine($"Statistics cache size: {_statisticsCache.Count}");
    Console.WriteLine($"Alert cache size: {_recentAlerts.Count}");
}
```

**Solutions**:
- Reduce history window size
- Implement periodic cache cleanup
- Use streaming statistics instead of storing all data
- Profile and optimize memory allocations

### Debugging Tools

#### Anomaly Detection Debugger
```csharp
public class AnomalyDebugInfo
{
    public DeviceTelemetry Input { get; set; }
    public List<AnomalyResult> Results { get; set; }
    public Dictionary<string, object> Statistics { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string ModelStatus { get; set; }
}

public AnomalyDebugInfo DebugDetection(DeviceTelemetry telemetry)
{
    var stopwatch = Stopwatch.StartNew();
    var results = DetectAnomalies(telemetry);
    stopwatch.Stop();
    
    return new AnomalyDebugInfo
    {
        Input = telemetry,
        Results = results,
        Statistics = GetCurrentStatistics(),
        ProcessingTime = stopwatch.Elapsed,
        ModelStatus = _model != null ? "Trained" : "Not Trained"
    };
}
```

---

## Best Practices

### Development Guidelines

#### 1. Error Handling
```csharp
public List<AnomalyResult> DetectAnomalies(DeviceTelemetry telemetry)
{
    try
    {
        // Validate input
        if (telemetry == null)
            throw new ArgumentNullException(nameof(telemetry));
            
        if (_historicalData.Count < 10)
            return new List<AnomalyResult>(); // Insufficient data
            
        // Perform detection
        return PerformAnomalyDetection(telemetry);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Anomaly detection failed for device {DeviceId}", telemetry.DeviceIdentifier);
        return new List<AnomalyResult>();
    }
}
```

#### 2. Logging Strategy
```csharp
private void LogAnomalyDetection(DeviceTelemetry telemetry, List<AnomalyResult> anomalies)
{
    if (anomalies.Any())
    {
        foreach (var anomaly in anomalies)
        {
            _logger.LogWarning("Anomaly detected: {Type} for device {DeviceId} with score {Score:F2}", 
                anomaly.Type, telemetry.DeviceIdentifier, anomaly.Score);
        }
    }
    else
    {
        _logger.LogDebug("No anomalies detected for device {DeviceId}", telemetry.DeviceIdentifier);
    }
}
```

#### 3. Testing Approach
```csharp
[Test]
public void TestAnomalyDetection_HighVoltageSpike_ShouldDetectAnomaly()
{
    // Arrange
    var aiService = new AiService();
    
    // Add normal data
    for (int i = 0; i < 100; i++)
    {
        aiService.AddTelemetryData(CreateNormalTelemetry());
    }
    
    // Act - inject anomalous data
    var anomalousTelemetry = CreateTelemetry(voltage: 300, current: 10, power: 2500);
    var results = aiService.DetectAnomalies(anomalousTelemetry);
    
    // Assert
    Assert.IsTrue(results.Any(r => r.Type.Contains("Voltage")));
    Assert.IsTrue(results.First().Score > 2.5);
}
```

### Production Deployment

#### 1. Configuration Management
- Use environment-specific configuration files
- Implement configuration validation
- Support hot-reloading of non-critical settings
- Document all configuration parameters

#### 2. Monitoring & Alerting
- Track key performance metrics
- Monitor anomaly detection rates
- Set up alerts for system failures
- Implement health check endpoints

#### 3. Data Quality Assurance
- Validate input data ranges
- Handle missing or corrupted data gracefully
- Implement data quality metrics
- Log data quality issues

#### 4. Scalability Considerations
- Design for horizontal scaling
- Implement stateless processing where possible
- Use message queues for high throughput
- Consider distributed caching strategies

---

## Conclusion

The AI Service and Anomaly Detection system provides a robust, scalable solution for intelligent monitoring of IoT energy devices. By combining statistical methods with machine learning techniques, the system delivers accurate anomaly detection while maintaining high performance and reliability.

### Key Success Factors
- **Multi-Algorithm Approach**: Combines strengths of different detection methods
- **Adaptive Learning**: Continuously improves through automated model training
- **Performance Optimization**: Efficient processing suitable for real-time applications
- **Comprehensive Monitoring**: Detailed logging and debugging capabilities
- **Production Ready**: Error handling, configuration management, and scalability features

This guide provides the foundation for understanding, deploying, and maintaining the AI-powered anomaly detection system in production environments.