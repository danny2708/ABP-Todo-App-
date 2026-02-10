import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { ListService, PagedResultDto, CoreModule, PermissionService, ConfigStateService, CurrentUserDto } from '@abp/ng.core';
import { ThemeSharedModule, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';

import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzMessageService } from 'ng-zorro-antd/message';

import { TaskService, TaskDto, TaskStatus, UserLookupDto } from 'src/app/proxy/tasks';
import { OAuthService } from 'angular-oauth2-oidc';
import { Router } from '@angular/router';

@Component({
  selector: 'app-list',
  standalone: true,
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzDrawerModule, NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzModalModule, NzFormModule, NzInputModule, NzSelectModule, NzPopconfirmModule,
    NzToolTipModule, NzAvatarModule,
  ],
})
export class List implements OnInit {
  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;
  currentUser: CurrentUserDto;

  // Search & Sort - Đã bổ sung biến filterText
  filterText = '';
  sorting = 'title asc'; 

  inProgressCount = 0;
  completedCount = 0;

  loading = false;
  saving = false;
  isModalOpen = false;
  isEditMode = false;
  selectedTaskId: string | null = null;

  hasCreatePermission = false;
  hasUpdatePermission = false;
  hasDeletePermission = false;

  filterStatus: TaskStatus | null = null;
  filterAssignedUserId: string | null = null;
  pageIndex = 1;
  pageSize = 10;

  form!: FormGroup;

  constructor(
    public readonly list: ListService,
    private readonly taskService: TaskService,
    private readonly fb: FormBuilder,
    private readonly message: NzMessageService,
    private readonly confirmation: ConfirmationService,
    private readonly modal: NzModalService,
    private readonly oauthService: OAuthService,
    private readonly router: Router,
    private readonly permissionService: PermissionService,
    private readonly configState: ConfigStateService 
  ) {}

  ngOnInit(): void {
    if (!this.isAuthenticated()) {
      this.showLoginRequiredModal();
      return;
    }

    this.currentUser = this.configState.getOne('currentUser');
    this.hasCreatePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Create');
    this.hasUpdatePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Update');
    this.hasDeletePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Delete');

    this.buildForm();
    this.loadUsers();
    this.loadTasks();
  }

  private isAuthenticated(): boolean {
    try {
      return this.oauthService.hasValidAccessToken();
    } catch {
      return false;
    }
  }

  private showLoginRequiredModal(): void {
    this.modal.confirm({
      nzTitle: 'You must be logged in',
      nzContent: 'You need to sign in to view Tasks. Do you want to login now?',
      nzOkText: 'Login',
      nzCancelText: 'Go Home',
      nzOnOk: () => this.oauthService.initLoginFlow(),
      nzOnCancel: () => this.router.navigate(['/']),
    });
  }

  private loadUsers(): void {
    this.taskService.getUserLookup().subscribe(res => {
      this.users = res.items;
    });
  }

  private loadTasks(): void {
    const streamCreator = (query: any) => {
      this.loading = true;
      // SỬA LỖI: Gửi kèm tham số filter và sorting về Backend
      return this.taskService.getList({
        ...query,
        filter: this.filterText, 
        sorting: this.sorting,
        status: this.filterStatus,
        assignedUserId: this.filterAssignedUserId,
      });
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.taskData = res;
      this.calculateStats();
      this.loading = false;
    });
  }

  // Lọc ngay khi nhập liệu
  onSearch(): void {
    this.list.page = 0;
    this.list.get();
  }

  onSortChange(params: { key: string; value: string | null }): void {
    if (params.value) {
      this.sorting = `${params.key} ${params.value === 'descend' ? 'desc' : 'asc'}`;
    } else {
      this.sorting = 'title asc';
    }
    this.list.get();
  }

  private calculateStats(): void {
    this.inProgressCount = this.taskData.items.filter(t => t.status === TaskStatus.InProgress).length;
    this.completedCount = this.taskData.items.filter(t => t.status === TaskStatus.Completed).length;
  }

  private buildForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, Validators.required],
      assignedUserId: [null],
    });
  }

  onFilterChange(): void {
    this.list.get();
  }

  clearFilters(): void {
    this.filterText = '';
    this.filterStatus = null;
    this.filterAssignedUserId = null;
    this.list.get();
  }

  onPageChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.list.page = pageIndex - 1;
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.list.maxResultCount = pageSize;
    this.list.get();
  }

  createTask(): void {
    if (!this.hasCreatePermission) return;
    this.isEditMode = false;
    this.selectedTaskId = null;
    this.form.reset({ status: TaskStatus.New });
    this.form.enable(); 
    this.isModalOpen = true;
  }

  editTask(task: TaskDto): void {
    this.isEditMode = true;
    this.selectedTaskId = task.id;
    this.form.patchValue(task);

    const isAssignedToMe = task.assignedUserId === this.currentUser.id;

    if (!this.hasUpdatePermission) {
      this.form.get('title')?.disable();
      this.form.get('description')?.disable();
      this.form.get('assignedUserId')?.disable();

      if (isAssignedToMe) {
        this.form.get('status')?.enable();
      } else {
        this.form.get('status')?.disable();
      }
    } else {
      this.form.enable();
    }
    
    this.isModalOpen = true;
  }

  handleCancel(): void {
    this.isModalOpen = false;
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;

    const formData = this.form.getRawValue();

    const request = this.isEditMode && this.selectedTaskId
      ? this.taskService.update(this.selectedTaskId, formData)
      : this.taskService.create(formData);

    request.subscribe({
      next: () => {
        this.message.success(this.isEditMode ? 'Updated successfully!' : 'Created successfully!');
        this.isModalOpen = false;
        this.list.get();
        this.saving = false;
      },
      error: (err) => {
        this.saving = false;
        if (err.error?.error?.message) {
          this.message.error(err.error.error.message);
        }
      },
    });
  }

  delete(id: string): void {
    if (!this.hasDeletePermission) return;
    this.confirmation.warn('::AreYouSureToDelete', '::ConfirmDelete').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.taskService.delete(id).subscribe(() => {
          this.message.success('Deleted successfully!');
          this.list.get();
        });
      }
    });
  }

  getStatusColor(status: TaskStatus | undefined | null): string {
    switch (status) {
      case TaskStatus.New: return 'blue';
      case TaskStatus.InProgress: return 'orange';
      case TaskStatus.Completed: return 'green';
      default: return 'default';
    }
  }

  getStatusKey(status: TaskStatus | undefined | null): string {
    if (status === null || status === undefined) return '::Unassigned';
    const name = (TaskStatus as any)[status as number];
    return `Enum:TaskStatus:${name}`;
  }
}