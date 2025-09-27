import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AlertService } from './services/alert.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'energy-dashboard';

  constructor(private alertService: AlertService) {}
  ngOnInit(): void {
    // Subscribe to alerts to ensure the service is active
    // AlertService will be initialized and start receiving alerts
    console.log('AppComponent initialized, AlertService is active.');
  }
}
