import { Routes } from '@angular/router';
import { DeviceComponent } from './pages/device/device.component';
import { TelemetryFormComponent } from './pages/telemetry-form/telemetry-form.component';
import { TelemetrySimulatorComponent } from './components/telemetry-simulator/telemetry-simulator.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const routes: Routes = [
    { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
    { path: 'telemetry', component: TelemetryFormComponent },
    { path: 'device', component: DeviceComponent },
    { path: 'dashboard', component: DashboardComponent },
    { path: 'telemetry-simulator', component: TelemetrySimulatorComponent }
];
