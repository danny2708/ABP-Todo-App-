using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tasks
{
    public interface ITaskRepository : IRepository<AppTask, Guid>
    {
        Task<AppTask> GetTaskByIdAsync(Guid id);
        
        // Bổ sung tham số filter và sorting
        Task<List<AppTask>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            string? filter = null,
            TaskStatus? status = null,
            Guid? assignedUserId = null
        );

        Task<long> GetTotalCountAsync(string? filter = null, TaskStatus? status = null, Guid? assignedUserId = null);
        
        Task<AppTask> CreateTaskAsync(AppTask task);
        Task<AppTask> UpdateTaskAsync(AppTask task);
        Task DeleteTaskAsync(Guid id);
    }
}