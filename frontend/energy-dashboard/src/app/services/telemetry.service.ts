import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import mqtt from "mqtt";
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

    // MQTT client instance
    private client: mqtt.MqttClient | null = null;

    constructor(private readonly http: HttpClient) {}

    // Connect to HiveMQ Cloud
    connectMqtt(): void {
        this.client = mqtt.connect('wss://47ca8b74c12949529a67a9f78cef8f10.s1.eu.hivemq.cloud:8884/mqtt', {
            username: 'hivemq.webclient.1758909235193',
            password: 'jYlCB8H@1.#?y9Fb5Gas',
            clean: true,
            reconnectPeriod: 1000,
            connectTimeout: 30 * 1000
        });

        this.client.on('connect', () => {
            console.log('✅ Frontend connected to HiveMQ Cloud!');
        });

        this.client.on('error', (err) => {
            console.error('❌ MQTT error:', err);
        });

        this.client.on('close', () => {
            console.warn('⚠️ MQTT connection closed');
        });
    }

    async sendTelemetryData(payload: Telemetrypayload): Promise<void> {

        // via REST API
        // try {
        //     const headers = {
        //         'Content-Type': 'application/json',
        //         'Accept': '*/*'
        //     };
        //     const response = await firstValueFrom(this.http.post(this.apiUrl, payload, { headers }));
        //     console.log('Telemetry data sent successfully:', response);
        // } catch (error) {
        //     console.error('Error sending telemetry data:', error);
        //     throw error; // Re-throw to allow component to handle the error
        // }

        // via MQTT
        if (this.client?.connected) {
            const topic = `telemetry/device/${payload.deviceIdentifier}`;
            const message = JSON.stringify(payload);
            this.client.publish(topic, message, { qos: 1 }, (error) => {
                if (error) {
                    console.error('Error publishing telemetry data:', error);
                } else {
                    console.log('Telemetry data published successfully to topic', topic);
                }   
            });
        } else {
            console.error('MQTT client is not connected. Cannot send telemetry data.');
        }
    }

    async getTelemetry(deviceId: string): Promise<Telemetrypayload[]> {
        try{
            const url = `${this.apiUrl}/${deviceId}`;
            const response = await firstValueFrom(this.http.get<Telemetrypayload[]>(url));
            return response;
        } catch (error) {
            console.error('Error fetching telemetry data:', error);
            throw error;
        }
    }

    startSimulator(devices: string[], intervalMs: number = 5000): void {
        console.log('Starting simulator for devices:', devices);

        if (!this.client) {
            console.log('MQTT client not initialized, connecting first...');
            this.connectMqtt();
        }
        
        this.client?.once('connect', () => {
            console.log('✅ MQTT client connected, starting simulator...');
            this.runSimulator(devices, intervalMs);
        });
        
        if (this.client?.connected) {
            console.log('✅ MQTT client already connected, starting simulator directly...');
            this.runSimulator(devices, intervalMs);
        }
    }

    private runSimulator(devices: string[], intervalMs: number): void {
        if (this.simulatorIntervalId) {
            console.warn('Simulator is already running.');
            return;
        }
        
        console.log(`Starting simulator for ${devices.length} devices with interval ${intervalMs}ms`);

        // helper to inject anamolies occasionally
        const generateAnamoly = (value:number, deviationPercent: number = 0.2): number => {
            if (Math.random() < 0.2) { // 20% chance to generate anamoly
                const deviation = value * deviationPercent;
                const direction = Math.random() < 0.5 ? -1 : 1;
                const result = parseFloat((value + direction * deviation).toFixed(2));
                console.log(`Generated anomaly: ${value} -> ${result}`);
                return result;
            }
            return value;
        }

        this.simulatorIntervalId = setInterval( async () => {
            console.log(`Simulator tick for devices: ${devices.join(', ')}`);
            
            for (const deviceId of devices) {
                console.log(`Generating telemetry for device: ${deviceId}`);
                
                const payload: Telemetrypayload = {
                    deviceIdentifier: deviceId,
                    timestamp: new Date().toISOString(),
                    voltage: generateAnamoly(parseFloat((Math.random() * (240 - 220) + 220).toFixed(2))),
                    current: generateAnamoly(parseFloat((Math.random() * (10 - 5) + 5).toFixed(2))),
                    power: 0, // Will be calculated
                    energy: 0, // Will be calculated
                    id: 0 // Placeholder, will be set by backend
                };
                payload.power = parseFloat((payload.voltage * payload.current).toFixed(2));
                payload.energy = parseFloat((payload.power * (intervalMs / 3600000)).toFixed(4)); // kWh for the interval
                
                console.log(`Generated payload for ${deviceId}:`, payload);
                
                try {
                    await this.sendTelemetryData(payload);
                    this.logTelemetryData(payload);
                    console.log(`✅ Successfully sent telemetry for ${deviceId}`);
                } catch (error) {
                    console.error(`❌ Error sending telemetry data for ${deviceId}:`, error);
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