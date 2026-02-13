import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/',
      // SỬA: Thêm tên Resource 'TaskManagement' vào trước dấu ::
      name: 'TaskManagement::Menu:Home', 
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },
    {
      path: '/tasks',
      // SỬA: Tương tự cho nút Tasks
      name: 'TaskManagement::Menu:Tasks', 
      iconClass: 'fas fa-tasks',
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Tasks',
    },
  ]);
}