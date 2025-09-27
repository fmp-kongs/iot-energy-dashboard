import { Component, OnInit } from '@angular/core';
import { TelemetryService } from '../../services/telemetry.service';
import { CommonModule, DatePipe } from '@angular/common';

@Component({
  selector: 'app-telemetry-simulator',
  imports: [DatePipe, CommonModule],
  templateUrl: './telemetry-simulator.component.html',
  styleUrls: ['./telemetry-simulator.component.scss']
})
export class TelemetrySimulatorComponent implements OnInit {
  ngOnInit(): void {
    // Restore running state from sessionStorage
    const running = sessionStorage.getItem('simulatorRunning');
    this.isRunning = running === 'true';
  }
  isRunning = false;
  private devices = ['Device001', 'Device002', 'Device003'];

  constructor(private telemetry: TelemetryService) {}

  startSimulation(): void {
    if (this.isRunning) {
      alert('Simulation is already running.');
      return;
    }
    this.isRunning = true;
    sessionStorage.setItem('simulatorRunning', 'true');
    this.telemetry.startSimulator(this.devices, 5000);
    alert('Telemetry simulation started for devices: ' + this.devices.join(', '));
  }

  stopSimulation(): void {
    this.telemetry.stopSimulator();
    alert('Telemetry simulation stopped.');
    this.isRunning = false;
    sessionStorage.setItem('simulatorRunning', 'false');
  }

  get telemetryLog() {
    return this.telemetry.telemetryLog;
  }

  trackTelemetry(index: number, item: any): string {
    return `${item.deviceIdentifier}-${item.timestamp}`;
  }

  getValueClass(value: number, baseValue: number, threshold: number): string {
    const deviation = Math.abs(value - baseValue) / baseValue;
    if (deviation > threshold * 2) return 'value-danger';
    if (deviation > threshold) return 'value-warning';
    return 'value-normal';
  }

  getPowerClass(power: number): string {
    if (power > 3000 || power < 1800) return 'value-danger';
    if (power > 2800 || power < 2000) return 'value-warning';
    return 'value-normal';
  }

}
