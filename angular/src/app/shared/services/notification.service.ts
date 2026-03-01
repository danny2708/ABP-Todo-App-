import { Injectable, inject } from '@angular/core';
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
  
  // DÙNG PROXY SERVICE THAY VÌ HTTPCLIENT
  private readonly proxyService = inject(ProxyNotificationService); 
  
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
      // Dữ liệu từ Backend gửi lên đã là NotificationDto chuẩn
      this.notifications.unshift(data); 
      this.nzMessage.info(data.message, { nzDuration: 5000 });
      this.onNotificationReceived$.next(data);
    });
  }

  fetchNotifications() {
    // Gọi API thông qua Proxy (tự động có token bảo mật)
    this.proxyService.getMyNotifications().subscribe({
      next: (res) => { 
        this.notifications = res || []; 
      },
      error: (err) => console.error('Lỗi khi lấy thông báo từ DB', err)
    });
  }

  markAsRead(id: string) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif && !notif.isRead) {
      notif.isRead = true; 
      // Gọi API qua Proxy
      this.proxyService.markAsRead(id).subscribe();
    }
  }

  markAllAsRead() {
    this.notifications.filter(n => !n.isRead).forEach(n => {
      n.isRead = true;
      this.proxyService.markAsRead(n.id).subscribe();
    });
  }
}