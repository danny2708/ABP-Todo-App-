import type { AuditedEntityDto } from '@abp/ng.core';

export interface CreateUpdateProjectDto {
  name: string;
  description?: string;
  projectManagerId: string;
  memberIds?: string[];
}

export interface ProjectDto extends AuditedEntityDto<string> {
  name?: string;
  description?: string;
  projectManagerId?: string;
  projectManagerName?: string;
  progress?: number;
  taskCount?: number;
  completedTaskCount?: number;
}
