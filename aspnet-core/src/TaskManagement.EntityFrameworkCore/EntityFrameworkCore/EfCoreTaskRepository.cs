using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core; // Quan trọng để dùng string sorting
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TaskManagement.Tasks
{
    public class EfCoreTaskRepository : EfCoreRepository<TaskManagementDbContext, AppTask, Guid>, ITaskRepository
    {
        public EfCoreTaskRepository(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
            : base(dbContextProvider) { }

        public async Task<AppTask> GetTaskByIdAsync(Guid id) 
            => await (await GetDbSetAsync()).FirstOrDefaultAsync(x => x.Id == id);

        public async Task<List<AppTask>> GetListAsync(
            int skipCount, int maxResultCount, string sorting, 
            string? filter = null, TaskStatus? status = null, Guid? assignedUserId = null)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x => x.Title.Contains(filter) || x.Description.Contains(filter))
                .WhereIf(status.HasValue, x => x.Status == status)
                .WhereIf(assignedUserId.HasValue, x => x.AssignedUserId == assignedUserId)
                .OrderBy(string.IsNullOrWhiteSpace(sorting) ? "Title asc" : sorting) // Xử lý sắp xếp
                .PageBy(skipCount, maxResultCount)
                .ToListAsync();
        }

        public async Task<long> GetTotalCountAsync(string? filter = null, TaskStatus? status = null, Guid? assignedUserId = null)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x => x.Title.Contains(filter) || x.Description.Contains(filter))
                .WhereIf(status.HasValue, x => x.Status == status)
                .WhereIf(assignedUserId.HasValue, x => x.AssignedUserId == assignedUserId)
                .LongCountAsync();
        }

        public async Task<AppTask> CreateTaskAsync(AppTask task) => await InsertAsync(task, autoSave: true);
        public async Task<AppTask> UpdateTaskAsync(AppTask task) => await UpdateAsync(task, autoSave: true);
        public async Task DeleteTaskAsync(Guid id) => await DeleteAsync(id, autoSave: true);
    }
}