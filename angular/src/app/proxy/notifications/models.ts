import type { EntityDto } from '@abp/ng.core';

export interface NotificationDto extends EntityDto<string> {
  title?: string;
  message?: string;
  type?: string;
  isRead?: boolean;
  relatedTaskId?: string | null;
  creationTime?: string;
}
