import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface FeatureFlag {
  id: string;
  name: string;
  description?: string;
  defaultState: boolean;
}

export interface CreateFeatureFlag {
  name: string;
  description?: string;
  defaultState: boolean;
}

export interface UpdateFeatureFlag {
  description?: string;
  defaultState: boolean;
}

export interface Override {
  id: string;
  featureId: string;
  overrideType: string;
  overrideKey: string;
  state: boolean;
}

export interface CreateOverride {
  overrideType: string;
  overrideKey: string;
  state: boolean;
}

export interface EvaluationResponse {
  isEnabled: boolean;
  appliedOverride?: string;
  appliedOverrideKey?: string;
}

@Injectable({ providedIn: 'root' })
export class FeatureService {
  private apiUrl = 'https://localhost:7119/api';

  constructor(private http: HttpClient) {}

  // Feature CRUD
  getFeatures(): Observable<FeatureFlag[]> {
    return this.http.get<FeatureFlag[]>(`${this.apiUrl}/features`);
  }

  getFeature(id: string): Observable<FeatureFlag> {
    return this.http.get<FeatureFlag>(`${this.apiUrl}/features/${id}`);
  }

  createFeature(flag: CreateFeatureFlag): Observable<FeatureFlag> {
    return this.http.post<FeatureFlag>(`${this.apiUrl}/features`, flag);
  }

  updateFeature(id: string, flag: UpdateFeatureFlag): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/features/${id}`, flag);
  }

  deleteFeature(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/features/${id}`);
  }

  // Overrides
  getOverrides(featureId: string): Observable<Override[]> {
    return this.http.get<Override[]>(`${this.apiUrl}/overrides/${featureId}`);
  }

  createOverride(featureId: string, override: CreateOverride): Observable<Override> {
    return this.http.post<Override>(`${this.apiUrl}/overrides/${featureId}`, override);
  }

  updateOverride(id: string, override: CreateOverride): Observable<Override> {
    return this.http.put<Override>(`${this.apiUrl}/overrides/${id}`, override);
  }

  deleteOverride(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/overrides/${id}`);
  }

  // Evaluation
  evaluateFeature(
    name: string, 
    userId?: string, 
    groupId?: string, 
    region?: string
  ): Observable<EvaluationResponse> {
    const params = new URLSearchParams();
    params.set('featureName', name);
    if (userId) params.set('userId', userId);
    if (groupId) params.set('groupId', groupId);
    if (region) params.set('region', region);
    
    return this.http.get<EvaluationResponse>(
      `${this.apiUrl}/features/evaluate?${params.toString()}`
    );
  }
}

