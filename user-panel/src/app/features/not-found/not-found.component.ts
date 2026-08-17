import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-bg flex flex-col items-center justify-center px-4 text-center">
      <p class="text-8xl font-bold text-navy mb-4">404</p>
      <h1 class="text-2xl md:text-3xl font-bold text-dark mb-3">Page Not Found</h1>
      <p class="text-muted text-sm md:text-base mb-8 max-w-sm">
        The page you are looking for doesn't exist or has been moved.
      </p>
      <a
        routerLink="/"
        class="bg-navy text-white px-8 py-3 rounded-lg font-semibold hover:bg-blue transition text-sm"
      >
        Go Home
      </a>
    </div>
  `,
})
export class NotFoundComponent {}
