using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tasks
{
    // Đổi TaskItem thành AppTask ở đây
    public interface ITaskRepository : IRepository<AppTask, Guid>
    {
        Task<List<AppTask>> GetTasksByAssignedUserAndStatusAsync(
            Guid assignedUserId,
            TaskStatus status
        );
    }
}