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
}