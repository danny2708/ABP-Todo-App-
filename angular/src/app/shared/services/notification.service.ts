import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { NzMessageService } from 'ng-zorro-antd/message';

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

      this.notifications.unshift(newNotification);
      this.unreadCount++;
      
      // Hiển thị Toast thông báo nhanh
      this.nzMessage.info(newNotification.message, { nzDuration: 5000 });
    });
  }

  clearUnread() {
    this.unreadCount = 0;
  }
}