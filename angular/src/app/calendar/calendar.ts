import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';

// Ng-Zorro
import { NzCalendarModule } from 'ng-zorro-antd/calendar';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSelectModule } from 'ng-zorro-antd/select'; 
import { NzModalModule } from 'ng-zorro-antd/modal';

// Services
import { CalendarService } from '../proxy/controllers/calendar.service'; 
import { TaskDto, TaskStatus } from '../proxy/tasks/models';
import { ProjectService } from '../proxy/projects/project.service';

interface CalendarEventGroup {
  isGroup: boolean;
  tasks: TaskDto[];
  topPx: number;
  heightPx: number;
}

@Component({
  selector: 'app-calendar',
  standalone: true,
  templateUrl: './calendar.html',
  styleUrls: ['../style/calendar.scss'],
  imports: [
    CommonModule, FormsModule, CoreModule, ThemeSharedModule,
    NzCalendarModule, NzBadgeModule, NzSelectModule, NzCardModule, 
    NzTagModule, NzToolTipModule, NzSpinModule, NzDividerModule,
    NzRadioModule, NzInputModule, NzIconModule, NzEmptyModule, NzModalModule
  ],
})
export class CalendarComponent implements OnInit {
  private calendarService = inject(CalendarService);
  private projectService = inject(ProjectService);

  loading = false;
  viewMode: 'month' | 'week' | 'list' = 'week'; 
  
  selectedDate = new Date(); 
  currentWeekStart = this.getStartOfWeek(new Date()); 
  
  searchTerm = '';
  allTasks: TaskDto[] = [];
  displayTasks: TaskDto[] = [];
  
  // Nâng cấp Dropdown dự án
  projects: any[] = [];
  selectedProjectIds: string[] = []; 
  
  PX_PER_HOUR = 45;
  START_HOUR = 0; 
  END_HOUR = 23;
  timeSlots = Array.from({ length: 24 }, (_, i) => `${i.toString().padStart(2, '0')}:00`);

  // BIẾN TỐI ƯU HIỆU NĂNG (Tránh vòng lặp vô hạn ở HTML)
  weekDaysList: Date[] = [];
  tasksByDayMap = new Map<string, CalendarEventGroup[]>();
  tasksForMonthMap = new Map<string, TaskDto[]>();
  listGroupedTasks: [string, TaskDto[]][] = [];

  // Quản lý Modal
  isTaskModalVisible = false;
  selectedGroupTasks: TaskDto[] = [];

  ngOnInit(): void {
    this.generateWeekDays();
    this.loadProjects();
    this.loadCalendarData();
  }

  loadProjects(): void {
    this.projectService.getList({ maxResultCount: 1000 }).subscribe(res => {
      this.projects = res.items;
      this.selectedProjectIds = this.projects.map(p => p.id); // Mặc định chọn tất cả
      this.applyFilter(); 
    });
  }

  loadCalendarData(): void {
    this.loading = true;
    const startDate = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth() - 1, 1).toISOString();
    const endDate = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth() + 2, 0).toISOString();

    this.calendarService.getCalendarTasks(startDate, endDate).subscribe(res => {
      this.allTasks = res;
      this.applyFilter(); 
      this.loading = false;
    });
  }

  applyFilter(): void {
    if (this.projects.length === 0) return;

    // 1. Lọc data
    this.displayTasks = this.allTasks.filter(t => {
      const matchProject = this.selectedProjectIds.includes(t.projectId);
      const matchSearch = !this.searchTerm || 
                          t.title.toLowerCase().includes(this.searchTerm.toLowerCase()) || 
                          (t.assignedUserName && t.assignedUserName.toLowerCase().includes(this.searchTerm.toLowerCase()));
      return matchProject && matchSearch;
    });

    // 2. Tiền xử lý dữ liệu (Pre-compute) để UI không bị giật lag
    this.buildDataMaps();
  }

  // Tiền xử lý dữ liệu 1 lần duy nhất mỗi khi filter đổi
  buildDataMaps() {
    this.tasksByDayMap.clear();
    this.tasksForMonthMap.clear();

    const listMap = new Map<string, TaskDto[]>();

    this.displayTasks.forEach(task => {
      const dateKey = this.formatDateKey(new Date(task.dueDate));
      
      // Map cho Month View & List View
      if (!this.tasksForMonthMap.has(dateKey)) this.tasksForMonthMap.set(dateKey, []);
      this.tasksForMonthMap.get(dateKey)!.push(task);
      
      if (!listMap.has(dateKey)) listMap.set(dateKey, []);
      listMap.get(dateKey)!.push(task);
    });

    this.listGroupedTasks = Array.from(listMap.entries()).sort((a, b) => a[0].localeCompare(b[0]));

    // Gộp nhóm cho Week View
    this.weekDaysList.forEach(day => {
      const dateKey = this.formatDateKey(day);
      const tasksOnThisDay = this.tasksForMonthMap.get(dateKey) || [];

      // Gom nhóm theo Giờ + Phút
      const timeGroupMap = new Map<string, TaskDto[]>();
      tasksOnThisDay.forEach(t => {
        const d = new Date(t.dueDate);
        const timeKey = `${d.getHours()}:${d.getMinutes()}`;
        if (!timeGroupMap.has(timeKey)) timeGroupMap.set(timeKey, []);
        timeGroupMap.get(timeKey)!.push(t);
      });

      const groups: CalendarEventGroup[] = [];
      timeGroupMap.forEach(tasks => {
        const { topPx, heightPx } = this.calculatePosition(new Date(tasks[0].dueDate));
        groups.push({
          isGroup: tasks.length > 1,
          tasks: tasks,
          topPx,
          heightPx
        });
      });

      this.tasksByDayMap.set(dateKey, groups);
    });
  }

  calculatePosition(date: Date) {
    const startMinutes = date.getHours() * 60 + date.getMinutes();
    const topPx = ((startMinutes - this.START_HOUR * 60) / 60) * this.PX_PER_HOUR;
    return { topPx: Math.max(0, topPx), heightPx: this.PX_PER_HOUR }; 
  }

  /* ---------- HELPER DATE ---------- */
  getStartOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day;
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  generateWeekDays() {
    this.weekDaysList = Array.from({ length: 7 }).map((_, i) => {
      const d = new Date(this.currentWeekStart);
      d.setDate(d.getDate() + i);
      return d;
    });
  }

  formatDateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  navigateWeek(direction: 'prev' | 'next' | 'today'): void {
    if (direction === 'today') {
      this.currentWeekStart = this.getStartOfWeek(new Date());
    } else {
      const d = new Date(this.currentWeekStart);
      d.setDate(d.getDate() + (direction === 'next' ? 7 : -7));
      this.currentWeekStart = d;
    }
    this.generateWeekDays();
    this.buildDataMaps();
  }

  /* ---------- TRUY XUẤT NHANH TRÊN HTML ---------- */
  getTasksForMonth(date: Date): TaskDto[] {
    return this.tasksForMonthMap.get(this.formatDateKey(date)) || [];
  }

  getGroupsForWeekDay(date: Date): CalendarEventGroup[] {
    return this.tasksByDayMap.get(this.formatDateKey(date)) || [];
  }

  // Mở Modal xem chi tiết
  openGroupModal(group: CalendarEventGroup) {
    this.selectedGroupTasks = group.tasks;
    this.isTaskModalVisible = true;
  }

  getStatusType(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.New: return 'processing';
      case TaskStatus.InProgress: return 'warning';
      case TaskStatus.Completed: return 'success';
      default: return 'default';
    }
  }
}