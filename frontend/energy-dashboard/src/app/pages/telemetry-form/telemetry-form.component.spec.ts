import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TelemetryFormComponent } from './telemetry-form.component';

describe('TelemetryFormComponent', () => {
  let component: TelemetryFormComponent;
  let fixture: ComponentFixture<TelemetryFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TelemetryFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TelemetryFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
