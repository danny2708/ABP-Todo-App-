import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

import { List } from './list/list';

const routes: Routes = [{ path: '', component: List }];

@NgModule({
  // List is standalone so DO NOT declare it
  imports: [
    CommonModule,
    RouterModule.forChild(routes),

    // Import the standalone component (it brings its own imports)
    List,
  ],
})
export class TasksModule {}
