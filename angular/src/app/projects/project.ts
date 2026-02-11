import { Component, OnInit } from '@angular/core';
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
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzMessageService } from 'ng-zorro-antd/message';

import { ProjectService, ProjectDto } from 'src/app/proxy/projects';
import { TaskService } from 'src/app/proxy/tasks';

@Component({
  selector: 'app-project',
  standalone: true,
  templateUrl: './project.html',
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzCardModule, NzProgressModule, NzButtonModule, NzIconModule, NzAvatarModule,
    NzInputModule, NzModalModule, NzFormModule, NzSelectModule
  ],
})
export class ProjectComponent implements OnInit {
  projectData: PagedResultDto<ProjectDto> = { items: [], totalCount: 0 };
  users: any[] = [];
  loading = false;
  isModalOpen = false;
  isEditMode = false;
  form!: FormGroup;
  filterText = '';

  constructor(
    public readonly list: ListService,
    private projectService: ProjectService,
    private taskService: TaskService,
    private fb: FormBuilder,
    private router: Router,
    private message: NzMessageService,
    public permission: PermissionService
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadUsers();
    this.loadProjects();
  }

  loadProjects(): void {
    const streamCreator = (query) => {
      this.loading = true;
      return this.projectService.getList({ ...query, filterText: this.filterText });
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.projectData = res;
      this.loading = false;
    });
  }

  loadUsers(): void {
    this.taskService.getUserLookup().subscribe(res => this.users = res.items);
  }

  buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: [''],
      projectManagerId: [null, Validators.required],
      memberIds: [[]]
    });
  }

  openTasks(projectId: string): void {
    this.router.navigate(['/tasks'], { queryParams: { projectId } });
  }

  createProject(): void {
    this.isEditMode = false;
    this.form.reset();
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
    const request = this.isEditMode 
      ? this.projectService.update(this.form.value.id, this.form.value)
      : this.projectService.create(this.form.value);

    request.subscribe(() => {
      this.message.success('Thành công!');
      this.isModalOpen = false;
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
}