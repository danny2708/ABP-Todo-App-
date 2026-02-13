import type { TaskStatus } from './task-status.enum';
import type { AuditedEntityDto, EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateTaskDto {
  projectId?: string;
  title?: string;
  description?: string | null;
  status?: TaskStatus;
  dueDate?: string | null;
  assignedUserIds?: string[];
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
  description?: string | null;
  status?: TaskStatus;
  projectId?: string;
  dueDate?: string | null;
  isApproved?: boolean;
  isRejected?: boolean;
  deletionReason?: string | null;
  assignedUserIds?: string[];
  assignedUserNames?: string[];
  assignedUserName?: string | null;
}

export interface UserLookupDto extends EntityDto<string> {
  userName?: string;
}
