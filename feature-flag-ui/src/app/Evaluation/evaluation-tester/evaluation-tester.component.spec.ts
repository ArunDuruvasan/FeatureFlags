import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { of, throwError } from 'rxjs';
import { EvaluationTesterComponent } from './evaluation-tester.component';
import { FeatureService, EvaluationResponse } from '../../Services/feature-service.service';

describe('EvaluationTesterComponent', () => {
  let component: EvaluationTesterComponent;
  let fixture: ComponentFixture<EvaluationTesterComponent>;
  let service: jasmine.SpyObj<FeatureService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    const serviceSpy = jasmine.createSpyObj('FeatureService', ['evaluateFeature']);
    const toastrSpy = jasmine.createSpyObj('ToastrService', ['error', 'warning']);

    await TestBed.configureTestingModule({
      imports: [EvaluationTesterComponent, HttpClientTestingModule, ReactiveFormsModule],
      providers: [
        { provide: FeatureService, useValue: serviceSpy },
        { provide: ToastrService, useValue: toastrSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EvaluationTesterComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(FeatureService) as jasmine.SpyObj<FeatureService>;
    toastr = TestBed.inject(ToastrService) as jasmine.SpyObj<ToastrService>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty values', () => {
    component.ngOnInit();

    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.get('userId')?.value).toBe('');
    expect(component.form.get('groupId')?.value).toBe('');
    expect(component.form.get('region')?.value).toBe('');
  });

  it('should require feature name', () => {
    component.ngOnInit();
    const nameControl = component.form.get('name');

    expect(nameControl?.hasError('required')).toBeTruthy();
  });

  it('should evaluate feature with valid form', () => {
    const mockResponse: EvaluationResponse = {
      isEnabled: true,
      appliedOverride: 'User',
      appliedOverrideKey: 'user1'
    };

    service.evaluateFeature.and.returnValue(of(mockResponse));
    component.ngOnInit();

    component.form.patchValue({
      name: 'test-feature',
      userId: 'user1',
      groupId: 'group1',
      region: 'US'
    });

    component.evaluateFeature();

    expect(service.evaluateFeature).toHaveBeenCalledWith(
      'test-feature',
      'user1',
      'group1',
      'US'
    );
    expect(component.result).toEqual(mockResponse);
  });

  it('should not evaluate feature with invalid form', () => {
    component.ngOnInit();
    component.form.patchValue({ name: '' }); // Invalid

    component.evaluateFeature();

    expect(service.evaluateFeature).not.toHaveBeenCalled();
    expect(toastr.warning).toHaveBeenCalledWith('Please enter a feature name');
  });

  it('should handle evaluation error', () => {
    service.evaluateFeature.and.returnValue(throwError(() => ({ error: 'Feature not found' })));
    component.ngOnInit();

    component.form.patchValue({ name: 'non-existent' });
    component.evaluateFeature();

    expect(toastr.error).toHaveBeenCalled();
    expect(component.result).toBeNull();
  });

  it('should pass undefined for optional parameters', () => {
    const mockResponse: EvaluationResponse = {
      isEnabled: false,
      appliedOverride: 'Default'
    };

    service.evaluateFeature.and.returnValue(of(mockResponse));
    component.ngOnInit();

    component.form.patchValue({ name: 'test-feature' });
    component.evaluateFeature();

    expect(service.evaluateFeature).toHaveBeenCalledWith(
      'test-feature',
      undefined,
      undefined,
      undefined
    );
  });
});
