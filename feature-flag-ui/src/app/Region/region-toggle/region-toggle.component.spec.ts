import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegionToggleComponent } from './region-toggle.component';

describe('RegionToggleComponent', () => {
  let component: RegionToggleComponent;
  let fixture: ComponentFixture<RegionToggleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegionToggleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegionToggleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
