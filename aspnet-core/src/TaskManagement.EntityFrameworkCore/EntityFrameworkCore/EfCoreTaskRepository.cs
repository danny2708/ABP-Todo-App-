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
    public class EfCoreTaskRepository : EfCoreRepository<TaskManagementDbContext, AppTask, Guid>, ITaskRepository
    {
        public EfCoreTaskRepository(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
            : base(dbContextProvider) { }

        public async Task<AppTask> GetTaskByIdAsync(Guid id) 
            => await (await GetDbSetAsync()).FirstOrDefaultAsync(x => x.Id == id);

        public async Task<List<AppTask>> GetListAsync(
            int skipCount, 
            int maxResultCount, 
            string sorting, 
            Guid? projectId = null, // Update cấu trúc mới
            string? filter = null, 
            TaskStatus? status = null, 
            Guid? assignedUserId = null,
            bool? isApproved = null) // Update cấu trúc mới
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId) // Lọc theo Project
                .WhereIf(isApproved.HasValue, x => x.IsApproved == isApproved) // Lọc trạng thái phê duyệt
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x => x.Title.Contains(filter) || x.Description.Contains(filter))
                .WhereIf(status.HasValue, x => x.Status == status)
                .WhereIf(assignedUserId.HasValue, x => x.AssignedUserId == assignedUserId)
                .OrderBy(string.IsNullOrWhiteSpace(sorting) ? "CreationTime desc" : sorting)
                .PageBy(skipCount, maxResultCount)
                .ToListAsync();
        }

        public async Task<long> GetTotalCountAsync(
            Guid? projectId = null, // Update cấu trúc mới
            string? filter = null, 
            TaskStatus? status = null, 
            Guid? assignedUserId = null,
            bool? isApproved = null) // Update cấu trúc mới
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId)
                .WhereIf(isApproved.HasValue, x => x.IsApproved == isApproved)
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