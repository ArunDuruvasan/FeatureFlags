import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeatureService, FeatureFlag } from '../../Services/feature-service.service';
import { FeatureFormComponent } from '../../Feature/feature-form/feature-form.component';
import { ToastrService } from 'ngx-toastr';
import { OverrideFormComponent } from '../../override/override-form/override-form.component';
import { EvaluationTesterComponent } from '../../Evaluation/evaluation-tester/evaluation-tester.component';
import { RegionToggleComponent } from '../../Region/region-toggle/region-toggle.component';

@Component({
  selector: 'app-feature-list-component',
  standalone: true,
  imports: [CommonModule, FormsModule, FeatureFormComponent, OverrideFormComponent, EvaluationTesterComponent, RegionToggleComponent],
  templateUrl: './feature-list-component.html',
  styleUrl: './feature-list-component.css'
})
export class FeatureListComponent implements OnInit {
  features: FeatureFlag[] = [];
  showForm = false;
  selectedFeatureId: string | null = null;
  editingFeature: FeatureFlag | null = null;
  showEvaluationTester = false;
  selectedFeatureForEvaluation: string | null = null;
  isToggling: string | null = null;

  constructor(
    private service: FeatureService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.loadFeatures();
  }

  loadFeatures() {
    this.service.getFeatures().subscribe({
      next: (data) => {
        this.features = data || [];
        console.log('Features loaded:', this.features.length);
      },
      error: (err) => {
        this.toastr.error('Failed to load features', 'Error');
        console.error('Load features error:', err);
        this.features = [];
      }
    });
  }

  deleteFeature(id: string, event?: Event) {
    if (confirm('Are you sure you want to delete this feature flag? This action cannot be undone.')) {
      // Close any open sections for this feature
      const featureToDelete = this.features.find(f => f.id === id);
      if (this.selectedFeatureId === id) {
        this.selectedFeatureId = null;
      }
      if (featureToDelete && this.selectedFeatureForEvaluation === featureToDelete.name) {
        this.closeEvaluationTester();
      }
      
      this.service.deleteFeature(id).subscribe({
        next: () => {
          // Success - show message and refresh
          this.toastr.success('Feature deleted successfully', 'Success');
          // Force reload the features list
          setTimeout(() => {
            this.loadFeatures();
          }, 100);
        },
        error: (err) => {
          const errorMessage = err.error?.message || err.error || 'Failed to delete feature';
          this.toastr.error(errorMessage, 'Error');
          console.error('Delete error:', err);
        }
      });
    }
  }

  editFeature(feature: FeatureFlag) {
    this.editingFeature = { ...feature };
  }

  cancelEdit() {
    this.editingFeature = null;
  }

  toggleDefaultState(feature: FeatureFlag) {
    // Prevent toggling if already in edit mode
    if (this.editingFeature?.id === feature.id) {
      return;
    }

    this.isToggling = feature.id;
    const newState = !feature.defaultState;

    this.service.updateFeature(feature.id, {
      description: feature.description,
      defaultState: newState
    }).subscribe({
      next: () => {
        // Update local state immediately for better UX
        feature.defaultState = newState;
        this.toastr.success(`Feature ${feature.name} ${newState ? 'enabled' : 'disabled'}`, 'Success');
        this.isToggling = null;
        // Optionally reload to ensure sync with server
        // this.loadFeatures();
      },
      error: (err) => {
        // Revert the toggle on error
        feature.defaultState = !newState;
        const errorMessage = err.error?.message || err.error || 'Failed to update feature';
        this.toastr.error(errorMessage, 'Error');
        this.isToggling = null;
        console.error('Toggle error:', err);
      }
    });
  }

  saveEdit() {
    if (!this.editingFeature) return;

    this.service.updateFeature(this.editingFeature.id, {
      description: this.editingFeature.description,
      defaultState: this.editingFeature.defaultState
    }).subscribe({
      next: () => {
        this.toastr.success('Feature updated successfully');
        this.editingFeature = null;
        this.loadFeatures();
      },
      error: (err) => {
        this.toastr.error('Failed to update feature');
        console.error(err);
      }
    });
  }

  toggleForm() {
    this.showForm = !this.showForm;
  }

  manageOverrides(featureId: string) {
    this.selectedFeatureId = featureId;
  }

  closeManageOverrides() {
    this.selectedFeatureId = null;
  }

  quickEvaluate(featureName: string, featureId: string) {
    // Show evaluation tester and prefill the feature name and ID
    this.selectedFeatureForEvaluation = featureName;
    this.selectedFeatureIdForEvaluation = featureId;
    this.showEvaluationTester = true;
    
    // Close manage overrides if open
    this.selectedFeatureId = null;
    
    // Scroll to evaluation tester after a short delay to ensure it's rendered
    setTimeout(() => {
      const evaluationSection = document.querySelector('app-evaluation-tester');
      if (evaluationSection) {
        evaluationSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
        // Add a highlight effect
        const card = evaluationSection.querySelector('.card');
        if (card) {
          card.classList.add('border-primary', 'shadow-lg');
          setTimeout(() => {
            card.classList.remove('border-primary', 'shadow-lg');
          }, 2000);
        }
      }
    }, 100);
  }

  selectedFeatureIdForEvaluation: string | null = null;

  closeEvaluationTester() {
    this.showEvaluationTester = false;
    this.selectedFeatureForEvaluation = null;
    this.selectedFeatureIdForEvaluation = null;
  }

  onFeatureCreated() {
    this.loadFeatures();
    // Close modal if using Bootstrap
    const modalElement = document.getElementById('featureFormModal');
    if (modalElement) {
      const modal = (window as any).bootstrap?.Modal?.getInstance(modalElement);
      if (modal) {
        modal.hide();
      }
    }
  }
}
