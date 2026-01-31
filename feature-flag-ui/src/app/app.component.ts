import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FeatureListComponent } from './FeatureList/feature-list-component/feature-list-component.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, FeatureListComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Feature Flag Management';
}
