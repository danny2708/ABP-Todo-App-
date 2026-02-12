import { Component, OnInit, inject } from '@angular/core'; // Thêm inject
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

// SỬA: Import trực tiếp từ file service để tránh lỗi "No value"
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
  // Inject services theo cách mới
  public readonly list = inject(ListService);
  private projectService = inject(ProjectService);
  private taskService = inject(TaskService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private message = inject(NzMessageService);
  public permission = inject(PermissionService);

  projectData: PagedResultDto<ProjectDto> = { items: [], totalCount: 0 };
  users: any[] = [];
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
    this.loadProjects();
  }

  loadProjects(): void {
    const streamCreator = (query) => {
      this.loading = true;
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

  toggleCreationSort(): void {
    this.isCreationSortDesc = !this.isCreationSortDesc;
    this.sorting = `CreationTime ${this.isCreationSortDesc ? 'DESC' : 'ASC'}`;
    this.list.get();
  }

  loadUsers(): void {
    this.taskService.getUserLookup().subscribe(res => this.users = res.items);
  }

  buildForm(): void {
    this.form = this.fb.group({
      id: [null],
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: [''],
      projectManagerId: [null, Validators.required],
      memberIds: [[]]
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
    this.projectService.get(project.id).subscribe(res => {
      this.form.patchValue(res);
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
      this.message.success('Thành công!');
      this.isModalOpen = false;
      this.saving = false;
      this.list.get();
    });
  }

  deleteProject(event: Event, id: string): void {
    event.stopPropagation();
    this.projectService.delete(id).subscribe(() => {
      this.message.success('Đã xóa!');
      this.list.get();
    });
  }

  handleCancel(): void {
    this.isModalOpen = false;
  }
}