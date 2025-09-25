import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";


export interface Telemetrypayload {
    id: number;
    deviceIdentifier: string;
    timestamp: string;
    voltage: number;
    current: number;
    power: number;
    energy: number;
}

@Injectable({providedIn: 'root'})
export class TelemetryService {
    private simulatorIntervalId: any = null;
    telemetryLog: Telemetrypayload[] = [];
    private readonly apiUrl = 'https://localhost:7126/api/Telemetry';

    constructor(private readonly http: HttpClient) {}

    async sendTelemetryData(payload: Telemetrypayload): Promise<void> {
        try {
            const headers = {
                'Content-Type': 'application/json',
                'Accept': '*/*'
            };
            const response = await firstValueFrom(this.http.post(this.apiUrl, payload, { headers }));
            console.log('Telemetry data sent successfully:', response);
        } catch (error) {
            console.error('Error sending telemetry data:', error);
            throw error; // Re-throw to allow component to handle the error
        }
    }

    startSimulator(devices: string[], intervalMs: number = 5000): void {
        if (this.simulatorIntervalId) {
            console.warn('Simulator is already running.');
            return;
        }
        this.simulatorIntervalId = setInterval( async () => {
            for (const deviceId of devices) {
                const payload: Telemetrypayload = {
                    deviceIdentifier: deviceId,
                    timestamp: new Date().toISOString(),
                    voltage: parseFloat((Math.random() * (240 - 220) + 220).toFixed(2)),
                    current: parseFloat((Math.random() * (10 - 5) + 5).toFixed(2)),
                    power: 0, // Will be calculated
                    energy: 0, // Will be calculated
                    id: 0 // Placeholder, will be set by backend
                };
                payload.power = parseFloat((payload.voltage * payload.current).toFixed(2));
                payload.energy = parseFloat((payload.power * (intervalMs / 3600000)).toFixed(4)); // kWh for the interval
                try {
                    await this.sendTelemetryData(payload);
                    this.logTelemetryData(payload);
                } catch (error) {
                    console.error('Error sending telemetry data:', error);
                }
            }
        }, intervalMs);
    }

    stopSimulator(): void{
        if (this.simulatorIntervalId) {
            clearInterval(this.simulatorIntervalId);
            this.simulatorIntervalId = null;
            console.log('Simulator stopped.');
        }
    }

    logTelemetryData(payload: Telemetrypayload): void {
        this.telemetryLog.unshift(payload);
        if (this.telemetryLog.length > 100) {
            this.telemetryLog.pop();
        }
    }
}