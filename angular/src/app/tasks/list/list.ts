import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { ListService, PagedResultDto } from '@abp/ng.core';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';

import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';

import { TaskService, TaskDto, TaskStatus, UserLookupDto } from 'src/app/proxy/tasks';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NzModalService } from 'ng-zorro-antd/modal';
import { OAuthService } from 'angular-oauth2-oidc';
import { Router } from '@angular/router';

@Component({
  selector: 'app-list',
  standalone: true,
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
  providers: [ListService],
  imports: [
    // Angular
    CommonModule,
    FormsModule,
    ReactiveFormsModule,

    // ABP
    CoreModule,
    ThemeSharedModule,

    // NG-Zorro modules used in template
    NzTableModule,
    NzTagModule,
    NzButtonModule,
    NzIconModule,
    NzModalModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzPopconfirmModule,
    NzToolTipModule,
  ],
})
export class List implements OnInit {
  // ===== Data =====
  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;

  // ===== UI State =====
  loading = false;
  saving = false;
  isModalOpen = false;
  isEditMode = false;
  selectedTaskId: string | null = null;

  // ===== Filters & Paging =====
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
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    if (!this.isAuthenticated()) {
      this.showLoginRequiredModal();
      return;
    }

    this.buildForm();
    this.loadUsers();
    this.loadTasks();
  }

  // ===== Auth =====
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

  // ===== Loaders =====
  private loadUsers(): void {
    this.taskService.getUserLookup().subscribe(res => {
      this.users = res.items;
    });
  }

  private loadTasks(): void {
    const streamCreator = (query: any) => {
      this.loading = true;
      return this.taskService.getList({
        ...query,
        status: this.filterStatus,
        assignedUserId: this.filterAssignedUserId,
      });
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.taskData = res;
      this.loading = false;
    });
  }

  // ===== Form =====
  private buildForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, Validators.required],
      assignedUserId: [null],
    });
  }

  // ===== Filters =====
  onFilterChange(): void {
    this.list.get();
  }

  clearFilters(): void {
    this.filterStatus = null;
    this.filterAssignedUserId = null;
    this.list.get();
  }

  // ===== Pagination =====
  onPageChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.list.page = pageIndex - 1;
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.list.maxResultCount = pageSize;
    this.list.get();
  }

  // ===== CRUD =====
  createTask(): void {
    if (!this.isAuthenticated()) {
      this.showLoginRequiredModal();
      return;
    }

    this.isEditMode = false;
    this.selectedTaskId = null;
    this.form.reset({ status: TaskStatus.New });
    this.isModalOpen = true;
  }

  editTask(task: TaskDto): void {
    if (!this.isAuthenticated()) {
      this.showLoginRequiredModal();
      return;
    }

    this.isEditMode = true;
    this.selectedTaskId = task.id;
    this.form.patchValue(task);
    this.isModalOpen = true;
  }

  handleCancel(): void {
    this.isModalOpen = false;
  }

  save(): void {
    if (this.form.invalid) return;

    this.saving = true;

    const request = this.isEditMode && this.selectedTaskId
      ? this.taskService.update(this.selectedTaskId, this.form.value)
      : this.taskService.create(this.form.value);

    request.subscribe({
      next: () => {
        this.message.success(this.isEditMode ? 'Cập nhật thành công!' : 'Tạo mới thành công!');
        this.isModalOpen = false;
        this.list.get();
        this.saving = false;
      },
      error: () => (this.saving = false),
    });
  }

  delete(id: string): void {
    this.confirmation.warn('::AreYouSureToDelete', '::ConfirmDelete').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.taskService.delete(id).subscribe(() => {
          this.message.success('Đã xóa thành công!');
          this.list.get();
        });
      }
    });
  }

  // ===== Helpers =====
  getStatusColor(status: TaskStatus | undefined | null): string {
    switch (status) {
      case TaskStatus.New:
        return 'blue';
      case TaskStatus.InProgress:
        return 'orange';
      case TaskStatus.Completed:
        return 'green';
      default:
        return 'default';
    }
  }

  // map numeric enum value -> Enum name key for localization (Enum:TaskStatus:New)
  getStatusKey(status: TaskStatus | undefined | null): string {
    if (status === null || status === undefined) {
      return '::Unassigned';
    }
    // TaskStatus[status] returns the name (e.g. "New")
    const name = (TaskStatus as any)[status as number];
    return `Enum:TaskStatus:${name}`;
  }
}
