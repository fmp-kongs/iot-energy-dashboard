# System Design Document
## IoT Energy Dashboard with AI-Powered Anomaly Detection

### Version: 1.3.0
### Date: September 28, 2025

---

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Design](#architecture-design)
3. [Component Details](#component-details)
4. [AI Service Deep Dive](#ai-service-deep-dive)
5. [Anomaly Detection Mechanisms](#anomaly-detection-mechanisms)
6. [Data Flow](#data-flow)
7. [API Design](#api-design)
8. [Database Schema](#database-schema)
9. [Security Considerations](#security-considerations)
10. [Performance & Scalability](#performance--scalability)

---

## System Overview

The IoT Energy Dashboard is a comprehensive real-time monitoring system designed for IoT energy devices. It combines traditional rule-based monitoring with advanced AI-powered anomaly detection to provide intelligent insights into energy consumption patterns.

### Key Objectives
- **Real-time Monitoring**: Continuous data collection and visualization
- **Intelligent Anomaly Detection**: Multi-algorithm approach for accurate anomaly identification
- **Scalable Architecture**: Designed to handle thousands of IoT devices
- **User-friendly Interface**: Intuitive dashboard for operators and analysts
- **Predictive Analytics**: ML-based forecasting and trend analysis

---

## Architecture Design

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        IoT Energy Dashboard                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │   Frontend   │    │   Backend    │    │  AI Service  │      │
│  │   Angular    │◀──▶│  ASP.NET     │◀──▶│   ML.NET     │      │
│  │   Dashboard  │    │   Core API   │    │   Analytics  │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
│                              │                    │             │
│                              ▼                    ▼             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │   SignalR    │    │    MQTT      │    │   InfluxDB   │      │
│  │   Real-time  │    │   Service    │    │  Time Series │      │
│  │ Communication│    │  IoT Gateway │    │   Database   │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
│                              ▲                                  │
├─────────────────────────────────────────────────────────────────┤
│                      External Systems                           │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │ IoT Devices  │    │  HiveMQ      │    │  Monitoring  │      │
│  │ Simulators   │───▶│  Cloud       │    │   Tools      │      │
│  │              │    │  MQTT Broker │    │              │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

### Technology Stack

#### Backend Components
- **ASP.NET Core 9.0**: Main API framework with dependency injection
- **ML.NET**: Machine learning framework for predictive analytics
- **MQTTnet**: MQTT client library for IoT communication
- **SignalR**: Real-time bidirectional communication
- **Entity Framework Core**: ORM for data persistence
- **InfluxDB.Client**: Time-series database client

#### Frontend Components
- **Angular 18**: SPA framework with TypeScript
- **Chart.js**: Data visualization library
- **SignalR Client**: Real-time data reception
- **RxJS**: Reactive programming for data streams
- **SCSS**: Styling with responsive design

#### Infrastructure
- **HiveMQ Cloud**: Managed MQTT broker service
- **InfluxDB**: Time-series database for telemetry storage
- **Docker**: Containerization for deployment
- **Git**: Version control system

---

## Component Details

### 1. MQTT Service (`MqttService.cs`)

**Purpose**: Central IoT communication hub responsible for device connectivity and data ingestion.

**Key Responsibilities**:
- Establish and maintain MQTT broker connections
- Subscribe to device telemetry topics
- Parse and validate incoming telemetry data
- Trigger downstream processing (storage, analysis, alerts)
- Handle connection failures and reconnection logic

**Implementation Details**:
```csharp
public class MqttService
{
    private readonly IMqttClient _mqttClient;
    private readonly InfluxDbService _influxDbService;
    private readonly IHubContext<AlertsHub> _hubContext;
    private readonly AiService _aiService;
    
    // Connection configuration
    // Message processing pipeline
    // Error handling and logging
}
```

**Key Features**:
- **Auto-reconnection**: Automatic reconnection with exponential backoff
- **Topic Wildcards**: Supports device discovery via wildcard subscriptions
- **Message Validation**: JSON schema validation for telemetry data
- **Error Resilience**: Graceful handling of malformed messages

### 2. AI Service (`AiService.cs`)

**Purpose**: Advanced analytics engine providing machine learning capabilities and statistical anomaly detection.

**Core Algorithms**:

#### Statistical Methods
1. **Z-Score Analysis**
   - Threshold: 2.5 standard deviations
   - Applied to: Voltage, Current, Power metrics
   - Updates: Real-time statistical calculations

2. **Interquartile Range (IQR) Detection**
   - Outlier Definition: Q1 - 1.5×IQR or Q3 + 1.5×IQR
   - Primary Use: Power consumption outliers
   - Advantage: Robust to extreme values

#### Machine Learning Methods
1. **Regression Model**
   - Algorithm: FastTree Regression (ML.NET)
   - Features: Voltage, Current, Power Factor, Efficiency
   - Target: Power consumption prediction
   - Training: Automatic retraining every 30 minutes

2. **Feature Engineering**
   ```csharp
   Power Factor = Power / (Voltage × Current)
   Efficiency = (Power / (Voltage × Current)) × 100
   ```

**Data Management**:
- **Sliding Window**: Maintains last 1000 data points
- **Memory Optimization**: Automatic cleanup of old data
- **Training Triggers**: Minimum 50 points for initial training

### 3. Real-time Communication (SignalR)

**Hub Configuration**:
```csharp
public class AlertsHub : Hub
{
    // Connection management
    // Group subscriptions
    // Authentication handling
}
```

**Event Types**:
- `ReceiveTelemetry`: Live sensor data broadcast
- `receivealert`: Anomaly alert notifications
- `ConnectionStatus`: Client connectivity updates

### 4. Data Storage Strategy

#### InfluxDB Schema
```
Measurement: device_telemetry
Tags:
  - device_id: string
  - location: string (optional)
Fields:
  - voltage: float
  - current: float
  - power: float
  - energy: float
  - timestamp: timestamp
```

**Retention Policies**:
- Raw data: 30 days
- Aggregated hourly: 1 year
- Aggregated daily: 5 years

---

## AI Service Deep Dive

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     AI Service Architecture                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│ │   Data      │    │ Statistical │    │  Machine    │     │
│ │ Collection  │───▶│  Analysis   │───▶│  Learning   │     │
│ │  Module     │    │   Module    │    │   Module    │     │
│ └─────────────┘    └─────────────┘    └─────────────┘     │
│        │                   │                   │           │
│        ▼                   ▼                   ▼           │
│ ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│ │ Historical  │    │   Z-Score   │    │  Regression │     │
│ │   Data      │    │ IQR Analysis│    │    Model    │     │
│ │  Storage    │    │ Calculations│    │  Prediction │     │
│ └─────────────┘    └─────────────┘    └─────────────┘     │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                    Anomaly Detection Output                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│ │   Alert     │    │  Severity   │    │ Confidence  │     │
│ │ Generation  │    │   Scoring   │    │   Metrics   │     │
│ │             │    │             │    │             │     │
│ └─────────────┘    └─────────────┘    └─────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### Data Processing Pipeline

#### 1. Data Ingestion
```csharp
public void AddTelemetryData(DeviceTelemetry telemetry)
{
    // Feature engineering
    var data = new TelemetryData
    {
        Voltage = (float)telemetry.Voltage,
        Current = (float)telemetry.Current,
        Power = (float)telemetry.Power,
        PowerFactor = CalculatePowerFactor(telemetry),
        Efficiency = CalculateEfficiency(telemetry)
    };
    
    // Add to sliding window
    _historicalData.Add(data);
    
    // Maintain window size
    if (_historicalData.Count > _maxHistorySize)
        _historicalData.RemoveAt(0);
    
    // Trigger training if conditions met
    TrainModelIfReady();
}
```

#### 2. Statistical Analysis
```csharp
private TelemetryStats CalculateStats(IEnumerable<double> values)
{
    var sortedValues = values.OrderBy(v => v).ToArray();
    var mean = sortedValues.Average();
    var variance = sortedValues.Select(v => Math.Pow(v - mean, 2)).Average();
    var stdDev = Math.Sqrt(variance);
    
    return new TelemetryStats
    {
        Mean = mean,
        StdDev = stdDev,
        Q1 = sortedValues[(int)(sortedValues.Length * 0.25)],
        Q3 = sortedValues[(int)(sortedValues.Length * 0.75)]
    };
}
```

#### 3. ML Model Training
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
            numberOfTrees: 100
        ));
    
    _model = pipeline.Fit(trainingData);
}
```

---

## Anomaly Detection Mechanisms

### Detection Algorithms

#### 1. Z-Score Statistical Detection

**Formula**: `Z = (X - μ) / σ`

**Thresholds**:
- Normal: |Z| ≤ 2.5
- Medium Anomaly: 2.5 < |Z| ≤ 3.5
- High Anomaly: |Z| > 3.5

**Applied To**:
- Voltage deviations
- Current fluctuations
- Power consumption patterns

#### 2. IQR Outlier Detection

**Formula**: 
- Lower Bound: Q1 - 1.5 × IQR
- Upper Bound: Q3 + 1.5 × IQR

**Advantages**:
- Robust to extreme values
- Effective for skewed distributions
- Less sensitive to outliers in training data

#### 3. ML Prediction Error Analysis

**Process**:
1. Predict expected power using ML model
2. Calculate prediction error: `|Actual - Predicted|`
3. Compute relative error: `Error / max(Predicted, 1)`
4. Flag as anomaly if relative error > 15%

**Severity Levels**:
- Medium: 15% - 30% deviation
- High: > 30% deviation

### Ensemble Scoring

The system combines multiple detection methods to provide comprehensive anomaly detection:

```csharp
public List<AnomalyResult> DetectAnomalies(DeviceTelemetry telemetry)
{
    var anomalies = new List<AnomalyResult>();
    
    // Statistical methods
    anomalies.AddRange(DetectStatisticalAnomalies(telemetry));
    
    // ML-based detection
    var mlAnomaly = DetectMLAnomaly(telemetry);
    if (mlAnomaly != null)
        anomalies.Add(mlAnomaly);
    
    return anomalies;
}
```

### Alert Classification

#### Severity Levels
- **Low**: Minor deviations, informational alerts
- **Medium**: Significant deviations requiring attention
- **High**: Critical deviations requiring immediate action

#### Alert Types
- **Voltage Anomaly**: Statistical deviation from normal voltage ranges
- **Current Anomaly**: Unusual current patterns
- **Power Anomaly**: Both statistical and ML-based power anomalies
- **Power Outlier**: IQR-based power outliers
- **ML Prediction Anomaly**: Deviations from ML model predictions

---

## Data Flow

### Real-time Data Pipeline

```
IoT Device → MQTT Broker → MQTT Service → AI Service
                              │              │
                              ▼              ▼
                         InfluxDB      Anomaly Detection
                              │              │
                              ▼              ▼
                         Historical     SignalR Hub
                          Storage          │
                              │            ▼
                              └──────▶ Angular UI
```

### Data Processing Steps

1. **Data Ingestion**
   - MQTT message reception
   - JSON deserialization
   - Data validation

2. **Storage**
   - InfluxDB write operation
   - Time-series indexing
   - Retention policy application

3. **AI Analysis**
   - Add to historical dataset
   - Statistical calculation updates
   - ML model inference
   - Anomaly detection execution

4. **Real-time Broadcast**
   - SignalR telemetry broadcast
   - Anomaly alert generation
   - Client notification delivery

---

## API Design

### RESTful Endpoints

#### Device Management
```
GET    /api/devices              # List all devices
POST   /api/devices              # Register new device
GET    /api/devices/{id}         # Get device details
PUT    /api/devices/{id}         # Update device
DELETE /api/devices/{id}         # Remove device
```

#### Telemetry Data
```
GET    /api/telemetry/{deviceId}           # Get device telemetry
GET    /api/telemetry/{deviceId}/latest    # Get latest readings
POST   /api/telemetry                      # Submit telemetry data
GET    /api/telemetry/aggregated           # Get aggregated data
```

#### Analytics & Anomalies
```
GET    /api/analytics/anomalies/{deviceId} # Get anomaly history
GET    /api/analytics/predictions          # Get ML predictions
GET    /api/analytics/statistics           # Get statistical metrics
POST   /api/analytics/train                # Trigger model training
```

#### System Health
```
GET    /health                             # System health check
GET    /health/detailed                    # Detailed health metrics
```

### SignalR Events

#### Client to Server
```javascript
connection.invoke("JoinDeviceGroup", deviceId);
connection.invoke("LeaveDeviceGroup", deviceId);
```

#### Server to Client
```javascript
connection.on("ReceiveTelemetry", (data) => { ... });
connection.on("receivealert", (alert) => { ... });
connection.on("SystemStatus", (status) => { ... });
```

---

## Database Schema

### InfluxDB Measurements

#### device_telemetry
```
Fields:
- voltage: float64
- current: float64  
- power: float64
- energy: float64

Tags:
- device_id: string
- location: string
- device_type: string

Timestamp: nanosecond precision
```

#### anomaly_alerts
```
Fields:
- alert_type: string
- severity: string
- confidence_score: float64
- message: string

Tags:
- device_id: string
- detection_method: string
- alert_id: string

Timestamp: nanosecond precision
```

### SQL Database (Entity Framework)

#### Devices Table
```sql
CREATE TABLE Devices (
    Id INT PRIMARY KEY IDENTITY,
    DeviceIdentifier NVARCHAR(100) UNIQUE NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Location NVARCHAR(200),
    DeviceType NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

---

## Security Considerations

### Authentication & Authorization
- **JWT Tokens**: Stateless authentication
- **Role-based Access**: Admin, Operator, Viewer roles
- **API Key Management**: Device authentication

### Data Security
- **MQTT TLS**: Encrypted MQTT communication
- **HTTPS Enforcement**: All API communication encrypted
- **Input Validation**: Comprehensive data validation
- **SQL Injection Prevention**: Parameterized queries

### Infrastructure Security
- **Network Segmentation**: Isolated IoT network
- **Firewall Rules**: Restrictive access policies
- **Container Security**: Docker security best practices
- **Secrets Management**: Environment-based configuration

---

## Performance & Scalability

### Performance Metrics

#### Target Performance
- **Message Throughput**: 10,000 messages/second
- **API Response Time**: < 200ms for 95th percentile
- **Real-time Latency**: < 100ms for SignalR broadcasts
- **Database Write Rate**: 50,000 points/second (InfluxDB)

#### Current Optimizations
- **Connection Pooling**: Database connection management
- **Caching Strategy**: In-memory caching for frequent queries
- **Async Processing**: Non-blocking I/O operations
- **Batch Operations**: Bulk database writes

### Scalability Strategy

#### Horizontal Scaling
- **Load Balancers**: Distribute API requests
- **Multiple Instances**: Scale backend services
- **Database Sharding**: Partition data by device/time
- **Message Queues**: Decouple processing components

#### Vertical Scaling
- **Resource Optimization**: CPU and memory tuning
- **Database Indexing**: Optimized query performance
- **Connection Limits**: Efficient resource utilization

### Monitoring & Observability

#### Application Metrics
- **Performance Counters**: Response times, throughput
- **Error Rates**: Exception tracking and alerting
- **Resource Usage**: CPU, memory, disk utilization
- **Business Metrics**: Device count, anomaly rates

#### Logging Strategy
- **Structured Logging**: JSON-formatted logs
- **Correlation IDs**: Request tracing
- **Log Levels**: Debug, Info, Warning, Error, Critical
- **Centralized Logging**: Aggregated log collection

---

## Conclusion

The IoT Energy Dashboard represents a comprehensive solution for real-time energy monitoring with advanced AI capabilities. The system's modular architecture ensures scalability, maintainability, and extensibility for future enhancements.

### Key Strengths
- **Real-time Processing**: Sub-second data processing and alerting
- **AI-Powered Analytics**: Multi-algorithm anomaly detection
- **Scalable Architecture**: Designed for enterprise-scale deployments
- **User Experience**: Intuitive and responsive dashboard
- **Reliability**: Fault-tolerant design with graceful degradation

### Future Roadmap
- **Advanced ML Models**: LSTM networks for time-series forecasting
- **Edge Computing**: Local processing capabilities
- **Mobile Applications**: Native mobile dashboard
- **Integration APIs**: Third-party system integration
- **Advanced Analytics**: Predictive maintenance algorithms

This system design provides a solid foundation for IoT energy monitoring applications while maintaining flexibility for future enhancements and scaling requirements.