import { Component } from '@angular/core';
import { TelemetryService } from '../../services/telemetry.service';
import { CommonModule, DatePipe } from '@angular/common';

@Component({
  selector: 'app-telemetry-simulator',
  imports: [DatePipe, CommonModule],
  templateUrl: './telemetry-simulator.component.html',
  styleUrls: ['./telemetry-simulator.component.scss']
})
export class TelemetrySimulatorComponent{
  isRunning = false;
  private devices = ['Device001', 'Device002', 'Device003'];

  constructor(private telemetry: TelemetryService) {}

  startSimulation(): void {
    if (this.isRunning) {
      alert('Simulation is already running.');
      return;
    }
    this.isRunning = true;
    this.telemetry.startSimulator(this.devices, 5000);
    alert('Telemetry simulation started for devices: ' + this.devices.join(', '));
  }

  stopSimulation(): void {
    this.telemetry.stopSimulator();
    alert('Telemetry simulation stopped.');
    this.isRunning = false;
  }

  get telemetryLog() {
    return this.telemetry.telemetryLog;
  }

}
