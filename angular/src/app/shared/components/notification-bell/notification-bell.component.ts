import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../services/notification.service';
import { NotificationDto } from '../../../proxy/notifications/models';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { CoreModule } from '@abp/ng.core';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule, CoreModule, NzBadgeModule, NzDropDownModule, NzIconModule, NzButtonModule],
  template: `
    <div class="bell-wrapper" nz-dropdown [nzDropdownMenu]="menu" nzTrigger="click" nzPlacement="bottomRight">
      <nz-badge [nzCount]="service.unreadCount" [nzOffset]="[5, 5]">
        <i nz-icon nzType="bell" class="bell-icon"></i>
      </nz-badge>
    </div>

    <nz-dropdown-menu #menu="nzDropdownMenu">
      <div class="notification-dropdown shadow-lg rounded bg-white">
        <div class="p-3 border-bottom d-flex justify-content-between align-items-center">
          <h6 class="mb-0 fw-bold">{{ 'TaskManagement::Notifications' | abpLocalization }}</h6>
          
          <button *ngIf="service.unreadCount > 0" 
                  nz-button nzType="link" nzSize="small" 
                  class="p-0 text-primary d-flex align-items-center"
                  (click)="service.markAllAsRead()">
            <i nz-icon nzType="check" class="me-1"></i> 
            {{ 'TaskManagement::Mark All As Read' | abpLocalization }}
          </button>
        </div>

        <div class="d-flex p-2 border-bottom gap-1">
          <button class="filter-btn" [class.active]="filter === 'all'" (click)="setFilter('all')">
            {{ 'TaskManagement::All' | abpLocalization }}
          </button>
          <button class="filter-btn" [class.active]="filter === 'unread'" (click)="setFilter('unread')">
            {{ 'TaskManagement::Unread' | abpLocalization }}
            <span *ngIf="service.unreadCount > 0" class="badge bg-danger ms-1">{{ service.unreadCount }}</span>
          </button>
          <button class="filter-btn" [class.active]="filter === 'read'" (click)="setFilter('read')">
            {{ 'TaskManagement::Read' | abpLocalization }}
          </button>
        </div>
        
        <div class="notification-list">
          <div *ngFor="let item of filteredNotifications" 
               class="notification-item border-bottom d-flex align-items-start"
               [ngClass]="{'unread-bg': !item.isRead}"
               (click)="service.markAsRead(item.id)">
            
            <div class="icon-wrapper me-3" [style.color]="getIconColor(item.type)">
              <i nz-icon [nzType]="getIconType(item.type)"></i>
            </div>

            <div class="flex-grow-1 min-w-0">
              <div class="message-text small text-dark" [ngClass]="{'fw-bold': !item.isRead}">
                {{ item.message }}
              </div>
              <div class="time-text x-small text-muted mt-1 d-flex align-items-center">
                <i nz-icon nzType="clock-circle" class="me-1"></i>
                {{ item.creationTime | date:'HH:mm dd/MM' }}
              </div>
            </div>

            <div *ngIf="!item.isRead" class="unread-dot ms-2 flex-shrink-0"></div>
          </div>
          
          <div *ngIf="filteredNotifications.length === 0" class="p-4 text-center text-muted">
            <i nz-icon nzType="bell" class="fs-1 opacity-50 mb-2"></i>
            <p class="small mb-0">
              {{ filter === 'unread' ? ('TaskManagement::No Unread Notifications' | abpLocalization) : 
                 filter === 'read' ? ('TaskManagement::No Read Notifications' | abpLocalization) : 
                 ('TaskManagement::No Notifications' | abpLocalization) }}
            </p>
          </div>
        </div>
      </div>
    </nz-dropdown-menu>
  `,
  styles: [`
    .bell-wrapper { cursor: pointer; padding: 0 15px; display: flex; align-items: center; height: 100%; transition: opacity 0.3s; }
    .bell-wrapper:hover { opacity: 0.7; }
    
    /* MÀU XANH ĐẬM ĐỂ NỔI BẬT HƠN */
    .bell-icon { font-size: 20px; color: #001529; } 
    
    .notification-dropdown { width: 360px; overflow: hidden; border: 1px solid #f0f0f0; }
    .notification-list { max-height: 400px; overflow-y: auto; }
    .notification-list::-webkit-scrollbar { width: 5px; }
    .notification-list::-webkit-scrollbar-thumb { background: #d9d9d9; border-radius: 4px; }

    .filter-btn {
      flex: 1; border: none; background: transparent; padding: 6px 12px;
      font-size: 13px; font-weight: 500; border-radius: 6px; color: #595959;
      transition: all 0.2s; cursor: pointer;
    }
    .filter-btn:hover { background: #e6f7ff; color: #1890ff; }
    .filter-btn.active { background: #1890ff; color: #fff; box-shadow: 0 2px 4px rgba(24,144,255,0.2); }

    .notification-item { padding: 14px 16px; transition: background-color 0.2s; cursor: pointer; }
    .notification-item:hover { background-color: #f5f5f5; }
    
    .unread-bg { background-color: #f0f8ff !important; }
    .unread-bg:hover { background-color: #e6f7ff !important; }
    .unread-dot { width: 10px; height: 10px; background-color: #1890ff; border-radius: 50%; margin-top: 5px; }

    .icon-wrapper {
      width: 36px; height: 36px; border-radius: 50%; background: #fff; border: 1px solid #f0f0f0;
      display: flex; align-items: center; justify-content: center; font-size: 18px; flex-shrink: 0;
    }
    .message-text { line-height: 1.4; word-wrap: break-word; }
    .x-small { font-size: 11px; }
  `]
})
export class NotificationBellComponent implements OnInit {
  public readonly service = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  public filter: 'all' | 'unread' | 'read' = 'all';

  ngOnInit(): void {
    this.service.startConnection();
    this.service.onNotificationReceived$.subscribe(() => {
      this.cdr.detectChanges();
    });
  }

  setFilter(f: 'all' | 'unread' | 'read') {
    this.filter = f;
  }

  get filteredNotifications() {
    return this.service.notifications.filter(n => {
      if (this.filter === 'unread') return !n.isRead;
      if (this.filter === 'read') return n.isRead;
      return true;
    });
  }

  getIconType(type: string): string {
    switch(type) {
      case 'TaskApproved': return 'check-circle';
      case 'NewTaskProposed': return 'plus-circle';
      case 'TaskRejected': return 'close-circle';
      case 'TaskOverdue': return 'warning';
      case 'ProjectAssigned': return 'project';
      case 'ProjectRemoved': return 'user-delete';
      default: return 'info-circle';
    }
  }

  getIconColor(type: string): string {
    switch(type) {
      case 'TaskApproved': return '#52c41a'; 
      case 'NewTaskProposed': return '#1890ff'; 
      case 'TaskRejected': return '#ff4d4f'; 
      case 'TaskOverdue': return '#faad14'; 
      case 'ProjectAssigned': return '#722ed1'; // Tím cho dự án
      default: return '#1890ff';
    }
  }
}