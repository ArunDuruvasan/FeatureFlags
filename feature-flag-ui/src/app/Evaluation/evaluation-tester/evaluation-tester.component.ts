import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FeatureService, EvaluationResponse, FeatureFlag, Override } from '../../Services/feature-service.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-evaluation-tester',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './evaluation-tester.component.html',
  styleUrl: './evaluation-tester.component.css'
})
export class EvaluationTesterComponent implements OnInit, OnChanges {
  @Input() prefillFeatureName: string | null = null;
  @Input() prefillFeatureId: string | null = null;
  
  form!: FormGroup;
  result: EvaluationResponse | null = null;
  features: FeatureFlag[] = [];
  overrides: Override[] = [];
  availableUserIds: string[] = [];
  availableGroupIds: string[] = [];
  availableRegions: string[] = [];
  isLoading = false;
  isEvaluating = false;
  isLoadingOverrides = false;

  constructor(
    private fb: FormBuilder,
    private service: FeatureService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      userId: [''],
      groupId: [''],
      region: ['']
    });

    this.loadFeatures();
    
    // Prefill feature name if provided
    if (this.prefillFeatureName) {
      this.form.patchValue({ name: this.prefillFeatureName });
    }

    // Watch for feature name changes to load overrides
    this.form.get('name')?.valueChanges.subscribe(featureName => {
      if (featureName && this.features.length > 0) {
        const feature = this.features.find(f => f.name === featureName);
        if (feature) {
          this.loadOverridesForFeature(feature.id);
        } else {
          // Clear overrides if feature not found
          this.overrides = [];
          this.availableUserIds = [];
          this.availableGroupIds = [];
          this.availableRegions = [];
        }
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['prefillFeatureName'] && this.prefillFeatureName && this.form) {
      this.form.patchValue({ name: this.prefillFeatureName });
    }
    
    if (changes['prefillFeatureId'] && this.prefillFeatureId) {
      this.loadOverridesForFeature(this.prefillFeatureId);
    }
  }

  loadFeatures() {
    this.isLoading = true;
    this.service.getFeatures().subscribe({
      next: (data) => {
        this.features = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error('Failed to load features');
        this.isLoading = false;
        console.error(err);
      }
    });
  }

  loadOverridesForFeature(featureId: string) {
    this.isLoadingOverrides = true;
    this.service.getOverrides(featureId).subscribe({
      next: (overrides) => {
        this.overrides = overrides;
        
        // Extract available User IDs, Group IDs, and Regions
        this.availableUserIds = overrides
          .filter(o => o.overrideType === 'User')
          .map(o => o.overrideKey)
          .filter((value, index, self) => self.indexOf(value) === index); // Remove duplicates
        
        this.availableGroupIds = overrides
          .filter(o => o.overrideType === 'Group')
          .map(o => o.overrideKey)
          .filter((value, index, self) => self.indexOf(value) === index);
        
        this.availableRegions = overrides
          .filter(o => o.overrideType === 'Region')
          .map(o => o.overrideKey)
          .filter((value, index, self) => self.indexOf(value) === index);
        
        this.isLoadingOverrides = false;
        
        // Prefill the first available override if any exist
        if (this.availableUserIds.length > 0 && !this.form.get('userId')?.value) {
          this.form.patchValue({ userId: this.availableUserIds[0] });
        }
        if (this.availableGroupIds.length > 0 && !this.form.get('groupId')?.value) {
          this.form.patchValue({ groupId: this.availableGroupIds[0] });
        }
        if (this.availableRegions.length > 0 && !this.form.get('region')?.value) {
          this.form.patchValue({ region: this.availableRegions[0] });
        }
      },
      error: (err) => {
        this.toastr.error('Failed to load overrides');
        this.isLoadingOverrides = false;
        console.error(err);
      }
    });
  }

  evaluateFeature() {
    if (this.form.valid) {
      this.isEvaluating = true;
      this.result = null;
      
      const { name, userId, groupId, region } = this.form.value;
      this.service.evaluateFeature(
        name,
        userId || undefined,
        groupId || undefined,
        region || undefined
      ).subscribe({
        next: (res) => {
          this.result = res;
          this.isEvaluating = false;
          this.toastr.success(`Feature evaluation completed: ${res.isEnabled ? 'Enabled' : 'Disabled'}`);
        },
        error: (err) => {
          const errorMessage = err.error?.message || err.error || 'Failed to evaluate feature';
          this.toastr.error(errorMessage);
          this.result = null;
          this.isEvaluating = false;
          console.error(err);
        }
      });
    } else {
      this.toastr.warning('Please select or enter a feature name');
    }
  }

  clearResult() {
    this.result = null;
  }

  resetForm() {
    this.form.reset({
      name: this.prefillFeatureName || '',
      userId: '',
      groupId: '',
      region: ''
    });
    this.result = null;
  }
}
