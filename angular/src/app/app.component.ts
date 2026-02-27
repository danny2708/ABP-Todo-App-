import { Component, OnInit, inject } from '@angular/core';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { LoaderBarComponent, NavItemsService } from '@abp/ng.theme.shared'; // SỬA: Import NavItemsService từ đây
import { NotificationService } from './shared/services/notification.service';
import { NotificationBellComponent } from './shared/components/notification-bell/notification-bell.component';
import { eLayoutType } from '@abp/ng.core'; // Thêm enum này để định nghĩa vị trí chuẩn

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
  `,
  standalone: true,
  imports: [LoaderBarComponent, DynamicLayoutComponent],
})
export class AppComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly navItems = inject(NavItemsService); // Bây giờ TypeScript sẽ nhận diện đúng type

  ngOnInit(): void {
    // Khởi tạo kết nối SignalR
    this.notificationService.startConnection();

    // Thêm quả chuông vào thanh điều hướng phía trên (Top Navbar)
    this.navItems.addItems([
      {
        id: 'NotificationBell',
        order: 1, 
        component: NotificationBellComponent,
      },
    ]);
  }
}