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
import { NzDrawerModule } from 'ng-zorro-antd/drawer'; // Thêm Drawer
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';

import { ProjectService, ProjectDto } from 'src/app/proxy/projects';
import { TaskService } from 'src/app/proxy/tasks';

@Component({
  selector: 'app-project',
  standalone: true,
  templateUrl: './project.html',
  styleUrls: ['../style/global.scss'],
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzCardModule, NzProgressModule, NzButtonModule, NzIconModule, NzAvatarModule,
    NzInputModule, NzDrawerModule, NzFormModule, NzSelectModule, NzToolTipModule
  ],
})
export class ProjectComponent implements OnInit {
  projectData: PagedResultDto<ProjectDto> = { items: [], totalCount: 0 };
  users: any[] = [];
  loading = false;
  isModalOpen = false;
  isEditMode = false;
  saving = false;
  form!: FormGroup;
  filterText = '';
  
  // Logic sắp xếp
  sorting = 'CreationTime DESC';
  isCreationSortDesc = true;

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
      return this.projectService.getList({ 
        ...query, 
        filterText: this.filterText,
        sorting: this.sorting // Gửi kèm logic sắp xếp lên BE
      });
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.projectData = res;
      this.loading = false;
    });
  }

  // Toggle sắp xếp tăng/giảm dần
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
    this.isModalOpen = true; // Kích hoạt Drawer
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