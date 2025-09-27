import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-alert-banner',
  imports: [CommonModule],
  templateUrl: './alert-banner.component.html',
  styleUrls: ['./alert-banner.component.scss']
})
export class AlertBannerComponent implements OnInit {
  alerts: string[] = [];

  constructor(private alertService: AlertService) {}

  ngOnInit(): void {
    this.alertService.alert$.subscribe(alerts => {
      this.alerts = alerts;
    });
  }
}
