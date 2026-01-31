import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { FeatureService } from '../../Services/feature-service.service';

@Component({
  selector: 'app-feature-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './feature-form.component.html',
  styleUrl: './feature-form.component.css'
})
export class FeatureFormComponent implements OnInit {
  @Output() featureCreated = new EventEmitter<void>();

  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private service: FeatureService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
      defaultState: [false]
    });
  }

  submitForm() {
    if (this.form.valid) {
      this.service.createFeature(this.form.value).subscribe({
        next: () => {
          this.toastr.success('Feature created successfully');
          this.form.reset();
          this.featureCreated.emit();
        },
        error: (err) => {
          const errorMessage = err.error?.message || err.error || 'Failed to create feature';
          this.toastr.error(errorMessage);
          console.error(err);
        }
      });
    } else {
      this.toastr.warning('Please fill in all required fields');
    }
  }
}
