import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OverrideFormComponent } from './override-form.component';

describe('OverrideFormComponent', () => {
  let component: OverrideFormComponent;
  let fixture: ComponentFixture<OverrideFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OverrideFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OverrideFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
