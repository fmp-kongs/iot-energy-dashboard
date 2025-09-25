import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DeviceComponent } from './pages/device/device.component';
import { TelemetryFormComponent } from './pages/telemetry-form/telemetry-form.component';

export const routes: Routes = [
    { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
    { path: 'telemetry', component: TelemetryFormComponent },
    { path: 'device', component: DeviceComponent },
    { path: 'dashboard', component: DashboardComponent },
];
