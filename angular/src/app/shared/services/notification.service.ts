import { Injectable, inject, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject } from 'rxjs';
import { NotificationService as ProxyNotificationService } from '../../proxy/notifications/notification.service';
import { NotificationDto } from '../../proxy/notifications/models';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly oAuthService = inject(OAuthService);
  private readonly nzMessage = inject(NzMessageService);
  private readonly proxyService = inject(ProxyNotificationService); 
  private readonly zone = inject(NgZone);

  private hubConnection: signalR.HubConnection | null = null;
  public notifications: NotificationDto[] = [];
  public onNotificationReceived$ = new Subject<NotificationDto>();

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  startConnection() {
    this.fetchNotifications();

    if (this.hubConnection) return;

    const token = this.oAuthService.getAccessToken();
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apis.default.url}/signalr-hubs/notification`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('SignalR Connected!'))
      .catch(err => console.error('SignalR Error: ', err));

    this.hubConnection.on('ReceiveNotification', (data: NotificationDto) => {
      this.zone.run(() => {
        // 1. LOGIC ĐỒNG BỘ NGẦM (SILENT SYNC)
        // Nếu type là 'SilentTaskSync', ta chỉ phát tín hiệu để các Component load lại data
        if (data.type === 'SilentTaskSync') {
          console.log('Đã nhận tín hiệu đồng bộ ngầm (Silent Sync) từ server');
          this.onNotificationReceived$.next(data); 
          return; // Kết thúc hàm, không hiện Toast và không thêm vào danh sách quả chuông
        }

        // 2. LOGIC THÔNG BÁO BÌNH THƯỜNG
        const newNotif: NotificationDto = {
          ...data,
          // Đảm bảo định dạng string ISO cho creationTime để khớp DTO
          creationTime: data.creationTime ? data.creationTime : new Date().toISOString()
        };
        
        // Cập nhật mảng để nảy số quả chuông
        this.notifications = [newNotif, ...this.notifications]; 
        
        // Hiển thị thông báo Toast cho người dùng
        this.nzMessage.info(newNotif.message || 'Bạn có thông báo mới', { nzDuration: 5000 });
        
        // Phát tín hiệu cho các component (như task.ts hoặc project.ts) refresh giao diện
        this.onNotificationReceived$.next(newNotif);
      });
    });
  }

  fetchNotifications() {
    this.proxyService.getMyNotifications().subscribe({
      next: (res) => { 
        this.notifications = res || []; 
      },
      error: (err) => console.error('Lỗi khi lấy danh sách thông báo:', err)
    });
  }

  markAsRead(id: string) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif && !notif.isRead) {
      notif.isRead = true; 
      this.proxyService.markAsRead(id).subscribe();
    }
  }

  markAllAsRead() {
    const unreadIds = this.notifications.filter(n => !n.isRead).map(n => n.id);
    this.notifications.forEach(n => n.isRead = true);

    unreadIds.forEach(id => {
      if (id) this.proxyService.markAsRead(id).subscribe();
    });
  }
}