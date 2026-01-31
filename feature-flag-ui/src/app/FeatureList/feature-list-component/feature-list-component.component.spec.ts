import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ToastrService } from 'ngx-toastr';
import { of, throwError } from 'rxjs';
import { FeatureListComponent } from './feature-list-component.component';
import { FeatureService, FeatureFlag } from '../../Services/feature-service.service';

describe('FeatureListComponent', () => {
  let component: FeatureListComponent;
  let fixture: ComponentFixture<FeatureListComponent>;
  let service: jasmine.SpyObj<FeatureService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  const mockFeatures: FeatureFlag[] = [
    { id: '1', name: 'feature1', description: 'Test 1', defaultState: true },
    { id: '2', name: 'feature2', description: 'Test 2', defaultState: false }
  ];

  beforeEach(async () => {
    const serviceSpy = jasmine.createSpyObj('FeatureService', ['getFeatures', 'deleteFeature', 'updateFeature']);
    const toastrSpy = jasmine.createSpyObj('ToastrService', ['success', 'error']);

    await TestBed.configureTestingModule({
      imports: [FeatureListComponent, HttpClientTestingModule],
      providers: [
        { provide: FeatureService, useValue: serviceSpy },
        { provide: ToastrService, useValue: toastrSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FeatureListComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(FeatureService) as jasmine.SpyObj<FeatureService>;
    toastr = TestBed.inject(ToastrService) as jasmine.SpyObj<ToastrService>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load features on init', () => {
    service.getFeatures.and.returnValue(of(mockFeatures));

    component.ngOnInit();
    fixture.detectChanges();

    expect(service.getFeatures).toHaveBeenCalled();
    expect(component.features).toEqual(mockFeatures);
  });

  it('should handle error when loading features', () => {
    service.getFeatures.and.returnValue(throwError(() => new Error('Failed')));

    component.ngOnInit();

    expect(toastr.error).toHaveBeenCalledWith('Failed to load features');
  });

  it('should delete feature when confirmed', () => {
    service.getFeatures.and.returnValue(of(mockFeatures));
    service.deleteFeature.and.returnValue(of(undefined));
    spyOn(window, 'confirm').and.returnValue(true);

    component.ngOnInit();
    component.deleteFeature('1');

    expect(service.deleteFeature).toHaveBeenCalledWith('1');
    expect(toastr.success).toHaveBeenCalledWith('Feature deleted successfully');
  });

  it('should not delete feature when not confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.deleteFeature('1');

    expect(service.deleteFeature).not.toHaveBeenCalled();
  });

  it('should set editing feature', () => {
    const feature = mockFeatures[0];
    component.editFeature(feature);

    expect(component.editingFeature).toEqual(feature);
  });

  it('should cancel edit', () => {
    component.editingFeature = mockFeatures[0];
    component.cancelEdit();

    expect(component.editingFeature).toBeNull();
  });

  it('should save edit', () => {
    const feature = { ...mockFeatures[0] };
    component.editingFeature = feature;
    service.updateFeature.and.returnValue(of(undefined));
    service.getFeatures.and.returnValue(of(mockFeatures));

    component.saveEdit();

    expect(service.updateFeature).toHaveBeenCalledWith(feature.id, {
      description: feature.description,
      defaultState: feature.defaultState
    });
    expect(toastr.success).toHaveBeenCalledWith('Feature updated successfully');
    expect(component.editingFeature).toBeNull();
  });

  it('should set selected feature ID for overrides', () => {
    component.manageOverrides('1');

    expect(component.selectedFeatureId).toBe('1');
  });
});
