using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks
{
    // Bỏ ICrudAppService, dùng IApplicationService để tự định nghĩa
    public interface ITaskAppService : IApplicationService
    {
        Task<TaskDto> GetAsync(Guid id);
        Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input);
        Task<TaskDto> CreateAsync(CreateUpdateTaskDto input);
        Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input);
        Task DeleteAsync(Guid id);
        Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
        Task<TaskDto> ApproveAsync(Guid id); // Phê duyệt đề xuất của User
        Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId);
    }
}