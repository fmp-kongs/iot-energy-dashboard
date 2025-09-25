import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TelemetryService } from '../../services/telemetry.service';

@Component({
  selector: 'app-telemetry-form',
  imports: [FormsModule, CommonModule],
  templateUrl: './telemetry-form.component.html',
  styleUrls: ['./telemetry-form.component.scss']
})
export class TelemetryFormComponent {

  deviceIdentifier = 'Device001';
  voltage = 230;
  current = 5;
  power = 1150;
  energy = 0;

  constructor(private readonly telemetry: TelemetryService) { }

  async send() {
    try {
      const payload = {
        id: 0, // Let the backend assign the ID
        deviceIdentifier: this.deviceIdentifier,
        timestamp: new Date().toISOString(),
        voltage: this.voltage,
        current: this.current,
        power: this.power,
        energy: this.energy
      };
      await this.telemetry.sendTelemetryData(payload);
      alert('Telemetry data sent successfully!');
    } catch (error) {
      console.error('Failed to send telemetry data:', error);
      alert('Failed to send telemetry data. Please check the console for more details.');
    }
  }
}
