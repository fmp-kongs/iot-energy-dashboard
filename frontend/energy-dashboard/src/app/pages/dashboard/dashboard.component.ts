import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, ViewChild } from '@angular/core';
import { TelemetryService, Telemetrypayload } from '../../services/telemetry.service';
import { Chart } from 'chart.js/auto';
import * as signalR from '@microsoft/signalr';

interface AnomalyAlert {
  deviceId: string;
  type: string;
  message: string;
  timestamp: string;
  severity: 'low' | 'medium' | 'high';
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements AfterViewInit {
  telemetryData: Telemetrypayload[] = [];
  currentAlerts: AnomalyAlert[] = [];

  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;
  chart!: Chart;

  private hubConnection!: signalR.HubConnection;
  isSignalRConnected = false;

  constructor(private readonly telemetryService: TelemetryService) {}

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.initChart();
      this.initSignalR();
    }, 100);
  }

  private initChart(): void {
    if (!this.chartCanvas?.nativeElement) {
      console.error('Chart canvas not available');
      return;
    }
    
    console.log('Initializing chart for Device001...');
    
    const datasets = [{
      label: 'Device001 Power Consumption',
      data: [],
      borderColor: '#3b82f6',
      backgroundColor: 'rgba(59, 130, 246, 0.1)',
      tension: 0.3,
      borderWidth: 2,
      fill: false,
      pointRadius: 4,
      pointHoverRadius: 6,
      pointBackgroundColor: '#3b82f6',
    }];

    this.chart = new Chart(this.chartCanvas.nativeElement, {
      type: 'line',
      data: { labels: [], datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: {
          duration: 200
        },
        scales: {
          x: { 
            title: { display: true, text: 'Time' },
            ticks: { maxRotation: 45 }
          },
          y: { 
            title: { display: true, text: 'Power (W)' },
            beginAtZero: true
          },
        },
        plugins: { 
          legend: { display: true, position: 'top' },
          tooltip: {
            mode: 'index',
            intersect: false
          }
        },
      },
    });
    
    console.log('Chart initialized');
  }

  private initSignalR(): void {
    console.log('Initializing SignalR connection...');
    
    // Connect to the alertsHub
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7126/alertsHub', {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        withCredentials: false // Don't send credentials to avoid CORS issues
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Debug) // Add detailed logging
      .build();

    // Listen for telemetry messages
    this.hubConnection.on('ReceiveTelemetry', (payload: Telemetrypayload) => {
      if (payload?.deviceIdentifier === 'Device001') {
        this.addTelemetryData(payload);
      }
    });

    // Listen for anomaly alerts (SignalR converts method names to lowercase)
    this.hubConnection.on('receivealert', (alert: AnomalyAlert) => {
      this.handleAnomalyAlert(alert);
    });



    // Connection event handlers
    this.hubConnection.onreconnecting(() => {
      this.isSignalRConnected = false;
    });

    this.hubConnection.onreconnected(() => {
      this.isSignalRConnected = true;
    });

    this.hubConnection.onclose(() => {
      this.isSignalRConnected = false;
    });

    this.hubConnection
      .start()
      .then(() => {
        console.log('✅ [DASHBOARD] Connected to SignalR alertsHub');
        console.log('👂 [DASHBOARD] Listening for telemetry messages from Device001');
        this.isSignalRConnected = true;
      })
      .catch((err) => {
        console.error('❌ [DASHBOARD] SignalR connection error:', err);
      });
  }

  private addTelemetryData(payload: Telemetrypayload): void {
    this.telemetryData.push(payload);
    
    // Keep only last 50 points for better visualization
    if (this.telemetryData.length > 50) {
      this.telemetryData.shift();
    }
    
    // Check for anomalies
    this.checkForAnomalies(payload);
    
    this.updateChart();
  }

  private updateChart(): void {
    if (!this.chart || this.telemetryData.length === 0) {
      return;
    }
    
    // Prepare data for chart
    const labels = this.telemetryData.map(entry => 
      new Date(entry.timestamp).toLocaleTimeString()
    );
    const powerData = this.telemetryData.map(entry => entry.power);
    
    // Update chart data
    this.chart.data.labels = labels;
    this.chart.data.datasets[0].data = powerData;
    
    // Update chart
    this.chart.update('none');
  }

  private checkForAnomalies(payload: Telemetrypayload): void {
    // Power consumption anomaly detection
    const normalPowerRange = { min: 1800, max: 3000 };
    if (payload.power < normalPowerRange.min || payload.power > normalPowerRange.max) {
      this.showAnomalyAlert({
        deviceId: payload.deviceIdentifier,
        type: 'Power Anomaly',
        message: `Unusual power consumption: ${payload.power}W (Normal: ${normalPowerRange.min}-${normalPowerRange.max}W)`,
        timestamp: payload.timestamp,
        severity: payload.power > normalPowerRange.max * 1.5 || payload.power < normalPowerRange.min * 0.5 ? 'high' : 'medium'
      });
    }

    // Voltage anomaly detection
    const normalVoltageRange = { min: 200, max: 250 };
    if (payload.voltage < normalVoltageRange.min || payload.voltage > normalVoltageRange.max) {
      this.showAnomalyAlert({
        deviceId: payload.deviceIdentifier,
        type: 'Voltage Anomaly',
        message: `Voltage out of range: ${payload.voltage}V (Normal: ${normalVoltageRange.min}-${normalVoltageRange.max}V)`,
        timestamp: payload.timestamp,
        severity: payload.voltage < 180 || payload.voltage > 280 ? 'high' : 'medium'
      });
    }

    // Current anomaly detection
    if (payload.current > 15 || payload.current < 5) {
      this.showAnomalyAlert({
        deviceId: payload.deviceIdentifier,
        type: 'Current Anomaly',
        message: `Current anomaly detected: ${payload.current}A`,
        timestamp: payload.timestamp,
        severity: payload.current > 20 || payload.current < 2 ? 'high' : 'medium'
      });
    }
  }

  private handleAnomalyAlert(alert: any): void {
    this.showAnomalyAlert(alert);
  }

  private showAnomalyAlert(alert: AnomalyAlert): void {
    // Add alert to display list
    this.currentAlerts.unshift(alert);
    
    // Keep only last 5 alerts
    if (this.currentAlerts.length > 5) {
      this.currentAlerts = this.currentAlerts.slice(0, 5);
    }
    
    console.log('🚨 Anomaly detected:', alert);
  }

  clearAlerts(): void {
    this.currentAlerts = [];
  }

  getAlertClass(severity: string): string {
    switch (severity) {
      case 'high': return 'alert-high';
      case 'medium': return 'alert-medium';
      case 'low': return 'alert-low';
      default: return 'alert-medium';
    }
  }

  trackAlert(index: number, alert: AnomalyAlert): string {
    return `${alert.deviceId}-${alert.timestamp}`;
  }

  formatTime(timestamp: string): string {
    return new Date(timestamp).toLocaleTimeString();
  }
}