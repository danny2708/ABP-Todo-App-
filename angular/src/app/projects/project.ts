import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService, PagedResultDto, CoreModule, PermissionService } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { Router } from '@angular/router';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { ProjectService } from '../proxy/projects/project.service';
import { ProjectDto } from '../proxy/projects/models';
import { TaskService } from '../proxy/tasks/task.service';

@Component({
  selector: 'app-project',
  standalone: true,
  templateUrl: './project.html',
  styleUrls: ['../style/global.scss'],
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzCardModule, NzProgressModule, NzButtonModule, NzIconModule, NzAvatarModule,
    NzInputModule, NzDrawerModule, NzFormModule, NzSelectModule, NzToolTipModule, NzSpinModule
  ],
})
export class ProjectComponent implements OnInit {
  public readonly list = inject(ListService);
  private projectService = inject(ProjectService);
  private taskService = inject(TaskService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private message = inject(NzMessageService);
  public permission = inject(PermissionService);

  projectData: PagedResultDto<ProjectDto> = { items: [], totalCount: 0 };
  users: any[] = []; // Danh sách nhân viên (MemberIds)
  projectManagers: any[] = []; // Danh sách sếp (ProjectManagerId)
  loading = false;
  isModalOpen = false;
  isEditMode = false;
  saving = false;
  form!: FormGroup;
  filterText = '';
  sorting = 'CreationTime DESC';
  isCreationSortDesc = true;

  ngOnInit(): void {
    this.buildForm();
    this.loadUsers();
    this.loadProjectManagers(); // Chỉ load những người có Role PM
    this.loadProjects();
  }

  loadProjects(): void {
    const streamCreator = (query) => {
      this.loading = true;
      // FIX: Gửi filterText lên Backend để tìm kiếm "cid"
      return this.projectService.getList({ 
        ...query, 
        filterText: this.filterText, 
        sorting: this.sorting 
      });
    };
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.projectData = res;
      this.loading = false;
    });
  }

  loadProjectManagers(): void {
    // Gọi API lọc theo Role "Project manager"
    this.projectService.getProjectManagersLookup().subscribe(res => {
      this.projectManagers = res.items;
    });
  }

  loadUsers(): void {
    this.taskService.getUserLookup().subscribe(res => this.users = res.items);
  }

  toggleCreationSort(): void {
    this.isCreationSortDesc = !this.isCreationSortDesc;
    this.sorting = `CreationTime ${this.isCreationSortDesc ? 'DESC' : 'ASC'}`;
    this.list.get();
  }

  buildForm(): void {
    this.form = this.fb.group({
      id: [null],
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: [''],
      projectManagerId: [null, Validators.required],
      memberIds: [[]] // Mảng ID thành viên để phục vụ multi-select
    });
  }

  openTasks(projectId: string): void {
    this.router.navigate(['/tasks/details'], { queryParams: { projectId } });
  }

  createProject(): void {
    this.isEditMode = false;
    this.form.reset({ memberIds: [] });
    this.isModalOpen = true;
  }

  editProject(event: Event, project: ProjectDto): void {
    event.stopPropagation();
    this.isEditMode = true;
    
    // Lấy chi tiết để có danh sách MemberIds nhằm tick sẵn các ô chọn
    this.projectService.get(project.id).subscribe(res => {
      this.form.patchValue({
        ...res,
        memberIds: res.memberIds || []
      });
      this.isModalOpen = true;
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const request = this.isEditMode 
      ? this.projectService.update(this.form.value.id, this.form.value)
      : this.projectService.create(this.form.value);

    request.subscribe(() => {
      this.message.success(this.l('TaskManagement::SaveSuccess'));
      this.isModalOpen = false;
      this.saving = false;
      this.list.get();
    });
  }

  deleteProject(event: Event, id: string): void {
    event.stopPropagation();
    this.projectService.delete(id).subscribe(() => {
      this.message.success(this.l('TaskManagement::DeletedSuccess'));
      this.list.get();
    });
  }

  handleCancel(): void { this.isModalOpen = false; }

  private l(key: string): string { return key; }
}