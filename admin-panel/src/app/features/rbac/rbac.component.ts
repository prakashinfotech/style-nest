import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

interface PermissionRow {
  resource: string;
  action: string;
  superAdmin: boolean | 'partial';
  admin: boolean | 'partial';
  seller: boolean | 'own';
  user: boolean | 'own' | 'verified';
}

@Component({
  selector: 'app-rbac',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-xl font-bold text-dark">RBAC — Permission Matrix</h1>
        <p class="text-sm text-muted mt-0.5">Read-only view of all role permissions across the platform.</p>
      </div>

      <!-- Legend -->
      <div class="flex flex-wrap gap-4 text-xs">
        <span class="flex items-center gap-1.5"><span class="w-4 h-4 rounded-sm bg-success/20 text-success flex items-center justify-center text-[10px]">✓</span> Allowed</span>
        <span class="flex items-center gap-1.5"><span class="w-4 h-4 rounded-sm bg-gold/20 text-gold flex items-center justify-center text-[10px]">◎</span> Own data only</span>
        <span class="flex items-center gap-1.5"><span class="w-4 h-4 rounded-sm bg-blue/20 text-blue flex items-center justify-center text-[10px]">P</span> Partial</span>
        <span class="flex items-center gap-1.5"><span class="w-4 h-4 rounded-sm bg-bg text-mid-gray flex items-center justify-center text-[10px]">—</span> Denied</span>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-navy text-white text-xs uppercase tracking-wide">
                <th class="px-4 py-3 text-left w-44">Resource</th>
                <th class="px-4 py-3 text-left w-40">Action</th>
                <th class="px-4 py-3 text-center w-28">SuperAdmin</th>
                <th class="px-4 py-3 text-center w-28">Admin</th>
                <th class="px-4 py-3 text-center w-28">Seller</th>
                <th class="px-4 py-3 text-center w-28">User</th>
              </tr>
            </thead>
            <tbody>
              @for (row of permissions; track row.resource + row.action; let i = $index) {
                <tr [class]="i % 2 === 0 ? 'bg-white' : 'bg-bg/30'">
                  <td class="px-4 py-2.5 font-medium text-dark text-xs">{{ row.resource }}</td>
                  <td class="px-4 py-2.5 text-muted text-xs">{{ row.action }}</td>
                  <td class="px-4 py-2.5 text-center">
                    <ng-container [ngTemplateOutlet]="cell" [ngTemplateOutletContext]="{ val: row.superAdmin }" />
                  </td>
                  <td class="px-4 py-2.5 text-center">
                    <ng-container [ngTemplateOutlet]="cell" [ngTemplateOutletContext]="{ val: row.admin }" />
                  </td>
                  <td class="px-4 py-2.5 text-center">
                    <ng-container [ngTemplateOutlet]="cell" [ngTemplateOutletContext]="{ val: row.seller }" />
                  </td>
                  <td class="px-4 py-2.5 text-center">
                    <ng-container [ngTemplateOutlet]="cell" [ngTemplateOutletContext]="{ val: row.user }" />
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <ng-template #cell let-val="val">
      @if (val === true) {
        <span class="inline-flex items-center justify-center w-6 h-6 rounded-sm bg-success/15 text-success text-xs font-bold">✓</span>
      } @else if (val === 'own') {
        <span class="inline-flex items-center justify-center w-6 h-6 rounded-sm bg-gold/15 text-gold text-[10px] font-bold">OWN</span>
      } @else if (val === 'partial') {
        <span class="inline-flex items-center justify-center w-6 h-6 rounded-sm bg-blue/15 text-blue text-[10px] font-bold">P</span>
      } @else if (val === 'verified') {
        <span class="inline-flex items-center justify-center w-6 h-6 rounded-sm bg-gold/15 text-gold text-[9px] font-bold">VER</span>
      } @else {
        <span class="text-mid-gray text-xs">—</span>
      }
    </ng-template>
  `,
})
export class RbacComponent {
  readonly permissions: PermissionRow[] = [
    { resource: 'Platform Settings', action: 'Read / Write',         superAdmin: true,      admin: false,     seller: false,    user: false },
    { resource: 'Audit Logs',        action: 'Read',                  superAdmin: true,      admin: false,     seller: false,    user: false },
    { resource: 'RBAC / Roles',      action: 'Read / Write',          superAdmin: true,      admin: false,     seller: false,    user: false },
    { resource: 'Admin Accounts',    action: 'Create / Read / Suspend',superAdmin: true,     admin: false,     seller: false,    user: false },
    { resource: 'All Sellers',       action: 'Read',                  superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'All Sellers',       action: 'Approve / Reject / Suspend',superAdmin: true,  admin: true,      seller: false,    user: false },
    { resource: 'Seller Profile',    action: 'Read / Update',         superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'All Users',         action: 'Read',                  superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'All Users',         action: 'Suspend',               superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'User Profile',      action: 'Read / Update',         superAdmin: true,      admin: true,      seller: false,    user: 'own' },
    { resource: 'All Products',      action: 'Read (incl. inactive)', superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'All Products',      action: 'Approve / Activate',    superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Seller Products',   action: 'Create',                superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'Seller Products',   action: 'Update / Delete',       superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'Products (public)', action: 'Read',                  superAdmin: true,      admin: true,      seller: true,     user: true },
    { resource: 'Categories',        action: 'Read',                  superAdmin: true,      admin: true,      seller: true,     user: true },
    { resource: 'Categories',        action: 'Create / Update',       superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Brands',            action: 'Read',                  superAdmin: true,      admin: true,      seller: true,     user: true },
    { resource: 'Brands',            action: 'Create / Update',       superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Attributes',        action: 'Read',                  superAdmin: true,      admin: true,      seller: true,     user: false },
    { resource: 'Attributes',        action: 'Create / Update',       superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'All Orders',        action: 'Read / Update Status',  superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Seller Orders',     action: 'Read / Update Status',  superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'User Orders',       action: 'Place',                 superAdmin: false,     admin: false,     seller: false,    user: true },
    { resource: 'User Orders',       action: 'Read / Cancel',         superAdmin: true,      admin: true,      seller: false,    user: 'own' },
    { resource: 'Inventory',         action: 'Read / Update',         superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'Banners',           action: 'CRUD',                  superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Coupons',           action: 'CRUD',                  superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Coupons',           action: 'Apply',                 superAdmin: true,      admin: true,      seller: false,    user: true },
    { resource: 'Cart',              action: 'Read / Update',         superAdmin: false,     admin: false,     seller: false,    user: 'own' },
    { resource: 'Wishlist',          action: 'Read / Update',         superAdmin: false,     admin: false,     seller: false,    user: 'own' },
    { resource: 'Wallet',            action: 'Read',                  superAdmin: true,      admin: true,      seller: false,    user: 'own' },
    { resource: 'Wallet',            action: 'Credit (admin)',        superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Reviews',           action: 'Read',                  superAdmin: true,      admin: true,      seller: true,     user: true },
    { resource: 'Reviews',           action: 'Create',                superAdmin: false,     admin: false,     seller: false,    user: 'verified' },
    { resource: 'Reviews',           action: 'Approve / Delete',      superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Analytics (platform)',action:'Read',                 superAdmin: true,      admin: 'partial', seller: false,    user: false },
    { resource: 'Analytics (seller)', action: 'Read',                 superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'Payouts',           action: 'Read',                  superAdmin: true,      admin: true,      seller: 'own',    user: false },
    { resource: 'Payouts',           action: 'Process',               superAdmin: true,      admin: true,      seller: false,    user: false },
    { resource: 'Media Upload',      action: 'Images / Videos',       superAdmin: true,      admin: true,      seller: true,     user: false },
    { resource: 'Notifications',     action: 'Read / Mark Read',      superAdmin: true,      admin: true,      seller: true,     user: 'own' },
  ];
}
