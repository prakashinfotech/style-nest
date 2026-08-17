import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';
import { BreadcrumbComponent } from '../../shared/components/breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, BreadcrumbComponent],
  template: `
    <div class="flex h-screen overflow-hidden bg-bg">
      <app-sidebar />
      <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
        <app-topbar />
        <main class="flex-1 overflow-y-auto p-6">
          <app-breadcrumb />
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class AdminLayoutComponent {}
