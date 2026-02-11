import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

// Import các Standalone Components mới
import { ProjectComponent } from '../projects/project'; // Trang danh sách thẻ dự án
import { TaskComponent } from './task';    // Trang danh sách công việc theo dự án

const routes: Routes = [
  { 
    path: '', 
    component: ProjectComponent // Mặc định vào trang danh sách dự án
  },
  { 
    path: 'details', 
    component: TaskComponent // Đường dẫn để xem chi tiết các task trong dự án
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),

    // Vì các Component là Standalone nên chúng ta import trực tiếp vào đây
    ProjectComponent,
    TaskComponent,
  ],
})
export class TasksModule {}