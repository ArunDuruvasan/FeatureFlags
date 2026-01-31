
import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { FeatureService, Override } from '../../Services/feature-service.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-region-toggle',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './region-toggle.component.html',
  styleUrl: './region-toggle.component.css'
})
export class RegionToggleComponent implements OnInit, OnChanges {
  @Input() featureId: string | null = null;
  regions = ['US', 'EU', 'IN', 'APAC', 'LATAM'];
  regionStates: { [key: string]: boolean } = {};
  regionOverrides: { [key: string]: Override | null } = {};

  constructor(
    private service: FeatureService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.regions.forEach(region => {
      this.regionStates[region] = false;
      this.regionOverrides[region] = null;
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['featureId'] && this.featureId) {
      this.loadRegionOverrides();
    }
  }

  loadRegionOverrides() {
    if (!this.featureId) return;

    this.service.getOverrides(this.featureId).subscribe({
      next: (overrides) => {
        this.regions.forEach(region => {
          this.regionStates[region] = false;
          this.regionOverrides[region] = null;
        });

        overrides
          .filter(o => o.overrideType === 'Region')
          .forEach(override => {
            this.regionStates[override.overrideKey] = override.state;
            this.regionOverrides[override.overrideKey] = override;
          });
      },
      error: (err) => {
        console.error('Failed to load region overrides', err);
      }
    });
  }

  toggleRegion(region: string) {
    if (!this.featureId) return;

    const newState = !this.regionStates[region];
    const existingOverride = this.regionOverrides[region];

    if (existingOverride) {
      this.service.updateOverride(existingOverride.id, {
        overrideType: 'Region',
        overrideKey: region,
        state: newState
      }).subscribe({
        next: () => {
          this.regionStates[region] = newState;
          this.toastr.success(`Region ${region} updated to ${newState ? 'Enabled' : 'Disabled'}`);
          this.loadRegionOverrides();
        },
        error: (err) => {
          this.toastr.error(`Failed to update region ${region}`);
          console.error(err);
        }
      });
    } else {
      this.service.createOverride(this.featureId, {
        overrideType: 'Region',
        overrideKey: region,
        state: newState
      }).subscribe({
        next: () => {
          this.regionStates[region] = newState;
          this.toastr.success(`Region ${region} set to ${newState ? 'Enabled' : 'Disabled'}`);
          this.loadRegionOverrides();
        },
        error: (err) => {
          this.toastr.error(`Failed to create region override for ${region}`);
          console.error(err);
        }
      });
    }
  }

  removeRegionOverride(region: string) {
    const override = this.regionOverrides[region];
    if (!override) return;

    if (confirm(`Remove override for region ${region}?`)) {
      this.service.deleteOverride(override.id).subscribe({
        next: () => {
          this.regionStates[region] = false;
          this.regionOverrides[region] = null;
          this.toastr.success(`Override removed for region ${region}`);
        },
        error: (err) => {
          this.toastr.error(`Failed to remove override for region ${region}`);
          console.error(err);
        }
      });
    }
  }
}
