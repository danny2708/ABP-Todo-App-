import type { TaskStatus } from './task-status.enum';
import type { AuditedEntityDto, EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateTaskDto {
  title: string;
  description?: string;
  status: TaskStatus;
  assignedUserId?: string | null;
}

export interface GetTasksInput extends PagedAndSortedResultRequestDto {
  filterText?: string | null;
  status?: TaskStatus | null;
  assignedUserId?: string | null;
}

export interface TaskDto extends AuditedEntityDto<string> {
  title?: string;
  description?: string;
  status?: TaskStatus;
  assignedUserId?: string | null;
}

export interface UserLookupDto extends EntityDto<string> {
  userName?: string;
}
