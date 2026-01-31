import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FeatureService, FeatureFlag, CreateFeatureFlag, Override, CreateOverride, EvaluationResponse } from './feature-service.service';

describe('FeatureService', () => {
  let service: FeatureService;
  let httpMock: HttpTestingController;
  const apiUrl = 'https://localhost:7119/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [FeatureService]
    });
    service = TestBed.inject(FeatureService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getFeatures', () => {
    it('should return list of features', () => {
      const mockFeatures: FeatureFlag[] = [
        { id: '1', name: 'feature1', defaultState: true },
        { id: '2', name: 'feature2', defaultState: false }
      ];

      service.getFeatures().subscribe(features => {
        expect(features.length).toBe(2);
        expect(features).toEqual(mockFeatures);
      });

      const req = httpMock.expectOne(`${apiUrl}/features`);
      expect(req.request.method).toBe('GET');
      req.flush(mockFeatures);
    });
  });

  describe('getFeature', () => {
    it('should return a single feature', () => {
      const mockFeature: FeatureFlag = {
        id: '1',
        name: 'test-feature',
        description: 'Test',
        defaultState: true
      };

      service.getFeature('1').subscribe(feature => {
        expect(feature).toEqual(mockFeature);
      });

      const req = httpMock.expectOne(`${apiUrl}/features/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockFeature);
    });
  });

  describe('createFeature', () => {
    it('should create a new feature', () => {
      const newFeature: CreateFeatureFlag = {
        name: 'new-feature',
        description: 'New feature',
        defaultState: true
      };

      const mockResponse: FeatureFlag = {
        id: '1',
        ...newFeature
      };

      service.createFeature(newFeature).subscribe(feature => {
        expect(feature).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/features`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newFeature);
      req.flush(mockResponse);
    });
  });

  describe('updateFeature', () => {
    it('should update a feature', () => {
      const updateData = {
        description: 'Updated',
        defaultState: false
      };

      service.updateFeature('1', updateData).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/features/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateData);
      req.flush(null);
    });
  });

  describe('deleteFeature', () => {
    it('should delete a feature', () => {
      service.deleteFeature('1').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/features/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('getOverrides', () => {
    it('should return list of overrides', () => {
      const mockOverrides: Override[] = [
        {
          id: '1',
          featureId: '1',
          overrideType: 'User',
          overrideKey: 'user1',
          state: true
        }
      ];

      service.getOverrides('1').subscribe(overrides => {
        expect(overrides.length).toBe(1);
        expect(overrides).toEqual(mockOverrides);
      });

      const req = httpMock.expectOne(`${apiUrl}/overrides/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockOverrides);
    });
  });

  describe('createOverride', () => {
    it('should create a new override', () => {
      const newOverride: CreateOverride = {
        overrideType: 'User',
        overrideKey: 'user1',
        state: true
      };

      const mockResponse: Override = {
        id: '1',
        featureId: '1',
        ...newOverride
      };

      service.createOverride('1', newOverride).subscribe(override => {
        expect(override).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/overrides/1`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newOverride);
      req.flush(mockResponse);
    });
  });

  describe('evaluateFeature', () => {
    it('should evaluate feature with all parameters', () => {
      const mockResponse: EvaluationResponse = {
        isEnabled: true,
        appliedOverride: 'User',
        appliedOverrideKey: 'user1'
      };

      service.evaluateFeature('test-feature', 'user1', 'group1', 'US').subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(
        req => req.url === `${apiUrl}/features/evaluate` &&
               req.params.get('featureName') === 'test-feature' &&
               req.params.get('userId') === 'user1' &&
               req.params.get('groupId') === 'group1' &&
               req.params.get('region') === 'US'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should evaluate feature with only feature name', () => {
      const mockResponse: EvaluationResponse = {
        isEnabled: false,
        appliedOverride: 'Default',
        appliedOverrideKey: undefined
      };

      service.evaluateFeature('test-feature').subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(
        req => req.url === `${apiUrl}/features/evaluate` &&
               req.params.get('featureName') === 'test-feature'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });
});
