import type { NotificationDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private restService = inject(RestService);
  apiName = 'TaskManagement';
  

  getMyNotifications = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotificationDto[]>({
      method: 'GET',
      url: '/api/app/notification/my-notifications',
    },
    { apiName: this.apiName,...config });
  

  markAsRead = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/notification/${id}/mark-as-read`,
    },
    { apiName: this.apiName,...config });
}