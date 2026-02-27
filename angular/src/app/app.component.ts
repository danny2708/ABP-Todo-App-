import { Component, OnInit, inject } from '@angular/core';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { LoaderBarComponent } from '@abp/ng.theme.shared';
import { NotificationService } from './shared/services/notification.service';

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

  ngOnInit(): void {
    this.notificationService.startConnection();
  }
}