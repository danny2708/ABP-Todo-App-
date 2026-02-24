// angular\src\app\tasks\task.ts
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { LocalizationService, ListService, PagedResultDto, CoreModule, PermissionService, ConfigStateService, CurrentUserDto } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';

import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox'; // Thêm để dùng checkbox lọc

import { TaskService } from '../proxy/tasks/task.service';
import { TaskDto, TaskStatus, UserLookupDto } from '../proxy/tasks/models';
import { ProjectService } from '../proxy/projects/project.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-task',
  standalone: true,
  templateUrl: './task.html',
  styleUrls: ['../style/global.scss'],
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzDrawerModule, NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzModalModule, NzFormModule, NzInputModule, NzSelectModule, NzProgressModule,
    NzToolTipModule, NzAvatarModule, NzDatePickerModule, NzSpinModule, NzDividerModule,
    NzCheckboxModule
  ],
})
export class TaskComponent implements OnInit {
  public readonly list = inject(ListService);
  private taskService = inject(TaskService);
  private projectService = inject(ProjectService);
  private fb = inject(FormBuilder);
  private message = inject(NzMessageService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private permissionService = inject(PermissionService);
  private configState = inject(ConfigStateService);
  private localizationService = inject(LocalizationService);

  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  overdueTasks: TaskDto[] = [];
  pendingTasks: TaskDto[] = [];
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;
  currentUser: CurrentUserDto;
  projectId: string;
  projectName: string = '';
  projectProgress: number = 0;

  loading = false;
  saving = false;
  isModalOpen = false;
  isEditMode = false;
  isOverdueModalOpen = false;
  isPendingModalOpen = false;
  isReasonModalOpen = false; 
  
  selectedTaskId: string | null = null;
  deletionReason: string = '';

  hasCreatePermission = false;
  hasApprovePermission = false;

  filterText = '';
  sorting = 'CreationTime DESC';
  pageIndex = 1;
  pageSize = 10;
  inProgressCount = 0;
  completedCount = 0;
  pendingCount = 0;

  // BỘ LỌC CHO MODAL QUÁ HẠN
  showOnlyUncompletedOverdue = true;

  form!: FormGroup;

  ngOnInit(): void {
    this.projectId = this.route.snapshot.queryParams['projectId'];
    if (!this.projectId) { this.goBack(); return; }

    this.currentUser = this.configState.getOne('currentUser');
    this.hasCreatePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Create');
    this.hasApprovePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Approve');

    this.buildForm();
    this.loadProjectInfo();
    this.loadUsers();
    this.loadTasks();
    this.loadOverdueTasks();
    this.loadPendingTasks();
  }

  // Getter để lọc dữ liệu hiển thị trên bảng Overdue
  get filteredOverdueTasks() {
    if (this.showOnlyUncompletedOverdue) {
      return this.overdueTasks.filter(t => t.status !== TaskStatus.Completed);
    }
    return this.overdueTasks;
  }

  goBack(): void { 
    this.router.navigate(['/projects']); 
  }

  private loadProjectInfo(): void {
    this.projectService.get(this.projectId).subscribe(res => {
        this.projectName = res.name;
        this.projectProgress = res.progress > 0 && res.progress <= 1 
            ? Math.round(res.progress * 100) 
            : Math.round(res.progress || 0);
    });
  }

  private loadUsers(): void {
    this.projectService.getMembersLookup(this.projectId).subscribe(res => this.users = res.items);
  }

  private loadTasks(): void {
    const streamCreator = (query: any) => {
      this.loading = true;
      return this.taskService.getList({
        ...query,
        projectId: this.projectId,
        filterText: this.filterText, 
        sorting: this.sorting,
        isApproved: true
      });
    };
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.taskData = res;
      this.calculateStats();
      this.loading = false;
    });
  }

  onSort(sort: { key: string; value: string | null }): void {
    if (sort.value) {
      this.sorting = `${sort.key} ${sort.value === 'descend' ? 'DESC' : 'ASC'}`;
    } else {
      this.sorting = 'CreationTime DESC';
    }
    this.list.get();
  }

  loadOverdueTasks(): void { this.taskService.getOverdueList(this.projectId).subscribe(res => this.overdueTasks = res.items); }

  loadPendingTasks(): void {
    this.taskService.getList({ projectId: this.projectId, isApproved: false, maxResultCount: 100 })
      .subscribe(res => { this.pendingTasks = res.items; this.pendingCount = res.totalCount; });
  }

  onSearch(): void { this.list.page = 0; this.list.get(); }

  onPageChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.list.page = pageIndex - 1;
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.list.maxResultCount = pageSize;
    this.list.get();
  }

  private calculateStats(): void {
    this.inProgressCount = this.taskData.items.filter(t => t.status === TaskStatus.InProgress).length;
    this.completedCount = this.taskData.items.filter(t => t.status === TaskStatus.Completed).length;
  }

  private buildForm(): void {
    this.form = this.fb.group({
      projectId: [this.projectId],
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, Validators.required],
      assignedUserIds: [[]],
      dueDate: [null],
      isApproved: [true]
    });
  }

  createTask(): void {
    this.isEditMode = false;
    this.selectedTaskId = null;
    this.form.reset({ status: TaskStatus.New, projectId: this.projectId, isApproved: this.hasCreatePermission, assignedUserIds: [] });
    this.form.enable(); 
    this.isModalOpen = true;
  }

  editTask(task: TaskDto): void {
    this.isPendingModalOpen = false;
    this.isOverdueModalOpen = false;
    this.isEditMode = true;
    this.selectedTaskId = task.id;
    this.form.patchValue({ ...task, assignedUserIds: task.assignedUserIds || [] });
    
    if (task.status === TaskStatus.Completed && !this.hasApprovePermission) {
        this.form.disable();
    } else if (!task.isApproved) {
        this.form.enable();
        this.form.get('status')?.disable();
    } else {
        this.form.enable();
    }
    this.isModalOpen = true;
  }

  canDeleteTask(task: TaskDto): boolean {
    if (this.hasApprovePermission) return task.status !== TaskStatus.Completed; 
    return !task.isApproved && task.creatorId === this.currentUser.id;
  }

  confirmDelete(id: string): void {
    const task = this.taskData.items.find(t => t.id === id) || this.pendingTasks.find(t => t.id === id);
    if (!task) return;

    if (!this.canDeleteTask(task)) {
        this.message.error(this.l('::NoPermissionToDeleteApprovedTask'));
        return;
    }

    this.selectedTaskId = id;
    this.deletionReason = '';
    this.isReasonModalOpen = true;
  }

  deleteTaskWithReason(): void {
    if (!this.deletionReason.trim()) {
        this.message.warning(this.l('::ReasonRequired'));
        return;
    }
    this.taskService.delete(this.selectedTaskId!, this.deletionReason).subscribe(() => {
        this.message.success(this.l('::DeletedSuccess'));
        this.isReasonModalOpen = false;
        this.refreshData();
        this.isPendingModalOpen = false;    
    });
  }

  approveTask(id: string): void {
    this.taskService.approve(id).subscribe(() => {
      this.message.success(this.l('::ApprovedSuccess'));
      this.refreshData();
      this.isPendingModalOpen = false; this.isModalOpen = false;
    });
  }

  rejectTask(id: string): void {
    this.taskService.reject(id).subscribe(() => {
      this.message.success(this.l('::RejectedSuccess'));
      this.refreshData();
      this.isPendingModalOpen = false; this.isModalOpen = false;
    });
  }

  private refreshData(): void {
    this.list.get(); this.loadOverdueTasks(); this.loadPendingTasks();
  }

  handleCancel(): void { this.isModalOpen = false; }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const formData = this.form.getRawValue();
    const request = this.isEditMode && this.selectedTaskId ? this.taskService.update(this.selectedTaskId, formData) : this.taskService.create(formData);
    request.subscribe({
      next: () => {
        // Sử dụng service chuẩn để hiện tiếng Việt
        const message = this.localizationService.instant('::SaveSuccess');    
        this.message.success(message);
        
        this.isModalOpen = false;
        this.refreshData();
        this.saving = false;
      },
      error: () => this.saving = false
    });
  }

  isOverdue(dueDate: string | null): boolean { return dueDate ? new Date(dueDate) < new Date() : false; }

  getStatusColor(status: TaskStatus | undefined | null): string {
    switch (status) { case TaskStatus.New: return 'blue'; case TaskStatus.InProgress: return 'orange'; case TaskStatus.Completed: return 'green'; default: return 'default'; }
  }

  getStatusKey(status: TaskStatus | undefined | null): string {
    if (status === null || status === undefined) return 'Unassigned';
    return `Enum:TaskStatus:${(TaskStatus as any)[status as number]}`;
  }

  // Cập nhật hàm dịch để dùng LocalizationService thật
  private l(key: string): string { 
    return this.localizationService.instant(key); 
  }
}