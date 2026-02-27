import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../services/notification.service';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzListModule } from 'ng-zorro-antd/list';
import { CoreModule } from '@abp/ng.core';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule, CoreModule, NzBadgeModule, NzDropDownModule, NzIconModule, NzListModule],
  template: `
    <div class="bell-wrapper" nz-dropdown [nzDropdownMenu]="menu" nzTrigger="click" (click)="service.clearUnread()">
      <nz-badge [nzCount]="service.unreadCount" [nzOffset]="[5, 0]">
        <i nz-icon nzType="bell" class="bell-icon"></i>
      </nz-badge>
    </div>

    <nz-dropdown-menu #menu="nzDropdownMenu">
      <div class="notification-dropdown shadow-lg bg-white rounded border">
        <div class="p-3 border-bottom d-flex justify-content-between align-items-center bg-light">
          <span class="fw-bold">{{ 'TaskManagement::Notifications' | abpLocalization }}</span>
          <span class="badge bg-primary rounded-pill">{{ service.notifications.length }}</span>
        </div>
        
        <nz-list [nzDataSource]="service.notifications" nzSize="small" class="notification-list">
          <nz-list-item *ngFor="let item of service.notifications">
            <div class="d-flex flex-column gap-1 w-100">
              <div class="message-text small text-dark">{{ item.message }}</div>
              <div class="time-text x-small text-muted">
                <i nz-icon nzType="clock-circle" class="me-1"></i>
                {{ item.creationTime | date:'HH:mm dd/MM' }}
              </div>
            </div>
          </nz-list-item>
          
          <nz-list-empty *ngIf="service.notifications.length === 0" [nzNoResult]="noData"></nz-list-empty>
          <ng-template #noData>
             <div class="p-4 text-center text-muted small">{{ 'TaskManagement::NoNotifications' | abpLocalization }}</div>
          </ng-template>
        </nz-list>
      </div>
    </nz-dropdown-menu>
  `,
  styles: [`
    .bell-wrapper { cursor: pointer; padding: 0 12px; transition: opacity 0.3s; }
    .bell-wrapper:hover { opacity: 0.7; }
    .bell-icon { font-size: 20px; color: #595959; }
    .notification-dropdown { width: 320px; max-height: 450px; overflow: hidden; }
    .notification-list { max-height: 380px; overflow-y: auto; }
    .message-text { line-height: 1.4; font-weight: 500; }
    .x-small { font-size: 11px; }
  `]
})
export class NotificationBellComponent {
  public readonly service = inject(NotificationService);
}