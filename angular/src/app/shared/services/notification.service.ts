import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { NzMessageService } from 'ng-zorro-antd/message';
// IMPORT SUBJECT ĐỂ PHÁT TÍN HIỆU REALTIME
import { Subject } from 'rxjs';

export interface NotificationDto {
  type: string;
  message: string;
  taskId?: string;
  creationTime: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly oAuthService = inject(OAuthService);
  private readonly nzMessage = inject(NzMessageService);
  
  private hubConnection: signalR.HubConnection | null = null;
  public notifications: NotificationDto[] = [];
  public unreadCount = 0;

  // LOA PHÁT THANH: Để các Component khác (như Task, Calendar) đăng ký lắng nghe
  public onNotificationReceived$ = new Subject<NotificationDto>();

  startConnection() {
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

    // Lắng nghe sự kiện "ReceiveNotification" từ Backend
    this.hubConnection.on('ReceiveNotification', (data: any) => {
      const newNotification: NotificationDto = {
        type: data.type,
        message: data.message,
        taskId: data.taskId,
        creationTime: new Date()
      };

      // 1. Lưu vào danh sách tạm thời trong RAM
      this.notifications.unshift(newNotification);
      this.unreadCount++;
      
      // 2. Hiển thị Toast thông báo nhanh ở góc màn hình
      this.nzMessage.info(newNotification.message, { nzDuration: 5000 });

      // 3. PHÁT TÍN HIỆU: Báo cho các Component đang mở biết để tự động load lại data
      this.onNotificationReceived$.next(newNotification);
    });
  }

  clearUnread() {
    this.unreadCount = 0;
  }
}