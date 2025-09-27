import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import * as signalR from "@microsoft/signalr";
import { BehaviorSubject } from "rxjs";


@Injectable({providedIn: 'root'})
export class AlertService {
    private hubConnection!: signalR.HubConnection;
    public alert$ = new BehaviorSubject<string[]>([]); // Observable for alerts


    constructor() {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl('https://localhost:7126/alertsHub', {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets,
                withCredentials: false // Include credentials for CORS
            })
            .withAutomaticReconnect()
            .build();
        
        this.hubConnection.on('ReceiveAlert', (message: string) => {
            const currentAlerts = this.alert$.getValue();
            this.alert$.next([message, ...currentAlerts].slice(0, 5)); // Keep only the latest 5 alerts
            console.log('New alert received:', message);
        })

        this.startConnection();
    }

    private startConnection(): void {
        this.hubConnection.start()
            .then(() => console.log('Connected to SignalR hub'))
            .catch(err => {
                console.error('Error connecting to SignalR hub:', err);
                setTimeout(() => this.startConnection(), 5000); // Retry after 5 seconds
            });
    }
}