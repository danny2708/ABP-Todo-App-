import { TaskStatus } from './task-status.enum';
import type { AuditedEntityDto, EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
export * from './task-status.enum';

export interface CreateUpdateTaskDto {
  projectId: string;
  title: string;
  description?: string;
  status?: TaskStatus;
  assignedUserId?: string | null;
  dueDate?: string | null;
}

export interface GetTasksInput extends PagedAndSortedResultRequestDto {
  projectId?: string | null;
  filterText?: string | null;
  status?: TaskStatus | null;
  assignedUserId?: string | null;
  isApproved?: boolean | null;
}

export interface TaskDto extends AuditedEntityDto<string> {
  title?: string;
  description?: string;
  status?: TaskStatus;
  projectId?: string;
  assignedUserId?: string | null;
  assignedUserName?: string;
  dueDate?: string | null;
  isApproved?: boolean;
}

export interface UserLookupDto extends EntityDto<string> {
  userName?: string;
}
