import { Component, OnInit, Input, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { FeatureService, Override } from '../../Services/feature-service.service';

@Component({
  selector: 'app-override-form',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './override-form.component.html',
  styleUrl: './override-form.component.css'
})
export class OverrideFormComponent implements OnInit, OnChanges {
  @Input() featureId: string | null = null;
  @Output() overrideSaved = new EventEmitter<void>();
  @Output() closeRequested = new EventEmitter<void>();
  
  form!: FormGroup;
  overrides: Override[] = [];
  editingOverride: Override | null = null;

  constructor(
    private fb: FormBuilder,
    private service: FeatureService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.form = this.fb.group({
      overrideType: ['User', Validators.required],
      overrideKey: ['', [Validators.required, Validators.maxLength(100)]],
      state: [false, Validators.required]
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['featureId'] && this.featureId) {
      this.loadOverrides();
    }
  }

  loadOverrides() {
    if (!this.featureId) return;

    this.service.getOverrides(this.featureId).subscribe({
      next: (data) => {
        this.overrides = data;
      },
      error: (err) => {
        this.toastr.error('Failed to load overrides');
        console.error(err);
      }
    });
  }

  saveOverride() {
    if (!this.featureId || !this.form.valid) {
      this.toastr.warning('Please fill in all required fields');
      return;
    }

    const overrideData = this.form.value;
    
    if (this.editingOverride) {
      // Update existing override
      this.service.updateOverride(this.editingOverride.id, overrideData).subscribe({
        next: () => {
          this.toastr.success('Override updated successfully');
          this.form.reset({ overrideType: 'User', state: false });
          this.editingOverride = null;
          this.loadOverrides();
          // Emit event to close the manage overrides screen after a short delay
          setTimeout(() => {
            this.overrideSaved.emit();
          }, 1500); // Wait 1.5 seconds to show the toaster notification
        },
        error: (err) => {
          const errorMessage = err.error?.message || err.error || 'Failed to update override';
          this.toastr.error(errorMessage);
          console.error(err);
        }
      });
    } else {
      // Create new override
      this.service.createOverride(this.featureId, overrideData).subscribe({
        next: () => {
          this.toastr.success('Override created successfully');
          this.form.reset({ overrideType: 'User', state: false });
          this.loadOverrides();
          // Emit event to close the manage overrides screen after a short delay
          setTimeout(() => {
            this.overrideSaved.emit();
          }, 1500); // Wait 1.5 seconds to show the toaster notification
        },
        error: (err) => {
          const errorMessage = err.error?.message || err.error || 'Failed to create override';
          this.toastr.error(errorMessage);
          console.error(err);
        }
      });
    }
  }

  editOverride(override: Override) {
    this.editingOverride = override;
    this.form.patchValue({
      overrideType: override.overrideType,
      overrideKey: override.overrideKey,
      state: override.state
    });
  }

  cancelEdit() {
    this.editingOverride = null;
    this.form.reset({ overrideType: 'User', state: false });
  }

  closeManageOverrides() {
    this.closeRequested.emit();
  }

  deleteOverride(id: string) {
    if (confirm('Are you sure you want to delete this override?')) {
      this.service.deleteOverride(id).subscribe({
        next: () => {
          this.toastr.success('Override deleted successfully');
          this.loadOverrides();
        },
        error: (err) => {
          this.toastr.error('Failed to delete override');
          console.error(err);
        }
      });
    }
  }
}
