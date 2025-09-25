import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TelemetrySimulatorComponent } from './telemetry-simulator.component';

describe('TelemetrySimulatorComponent', () => {
  let component: TelemetrySimulatorComponent;
  let fixture: ComponentFixture<TelemetrySimulatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TelemetrySimulatorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TelemetrySimulatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
