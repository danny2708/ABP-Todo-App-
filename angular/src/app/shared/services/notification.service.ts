import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject } from 'rxjs';

// Cập nhật DTO khớp với Database
export interface NotificationDto {
  id: string; // Khóa chính từ DB
  type: string;
  title: string;
  message: string;
  isRead: boolean; // Trạng thái đọc
  taskId?: string;
  creationTime: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly oAuthService = inject(OAuthService);
  private readonly nzMessage = inject(NzMessageService);
  private readonly http = inject(HttpClient); // Dùng HTTP để gọi API
  
  private hubConnection: signalR.HubConnection | null = null;
  public notifications: NotificationDto[] = [];
  public onNotificationReceived$ = new Subject<NotificationDto>();

  // Tính số lượng chưa đọc realtime
  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  startConnection() {
    // 1. TẢI LỊCH SỬ TỪ DATABASE NGAY KHI VÀO APP
    this.fetchNotifications();

    // 2. KẾT NỐI SIGNALR ĐỂ NHẬN THÔNG BÁO MỚI
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

    this.hubConnection.on('ReceiveNotification', (data: any) => {
      const newNotification: NotificationDto = {
        id: data.id || crypto.randomUUID(), // Lấy ID từ Server trả về
        title: data.title || 'Thông báo hệ thống',
        type: data.type,
        message: data.message,
        taskId: data.taskId,
        isRead: false, // Thông báo mới luôn là chưa đọc
        creationTime: new Date()
      };

      this.notifications.unshift(newNotification); // Đẩy lên đầu danh sách
      this.nzMessage.info(newNotification.message, { nzDuration: 5000 });
      this.onNotificationReceived$.next(newNotification);
    });
  }

  // --- GỌI API BACKEND ---

  fetchNotifications() {
    // Gọi API lấy top 20 thông báo mới nhất từ Backend
    this.http.get<NotificationDto[]>(`${environment.apis.default.url}/api/app/notification/my-notifications`)
      .subscribe({
        next: (res) => { this.notifications = res || []; },
        error: (err) => console.error('Failed to load notifications', err)
      });
  }

  markAsRead(id: string) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif && !notif.isRead) {
      notif.isRead = true; // Update UI ngay lập tức cho mượt
      // Gọi API lưu xuống DB
      this.http.post(`${environment.apis.default.url}/api/app/notification/${id}/mark-as-read`, {}).subscribe();
    }
  }

  markAllAsRead() {
    this.notifications.filter(n => !n.isRead).forEach(n => {
      n.isRead = true;
      this.http.post(`${environment.apis.default.url}/api/app/notification/${n.id}/mark-as-read`, {}).subscribe();
    });
  }
}