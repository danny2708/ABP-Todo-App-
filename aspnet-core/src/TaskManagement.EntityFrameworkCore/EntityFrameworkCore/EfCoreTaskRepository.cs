using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TaskManagement.Tasks
{
    // Kế thừa từ EfCoreRepository để tận dụng các hàm cơ bản của ABP
    public class EfCoreTaskRepository : EfCoreRepository<TaskManagementDbContext, AppTask, Guid>, ITaskRepository
    {
        public EfCoreTaskRepository(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        // Hiện thực hóa hàm tùy chỉnh 
        public async Task<List<AppTask>> GetTasksByAssignedUserAndStatusAsync(Guid assignedUserId, TaskStatus status)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(x => x.AssignedUserId == assignedUserId && x.Status == status)
                .ToListAsync();
        }
    }
}