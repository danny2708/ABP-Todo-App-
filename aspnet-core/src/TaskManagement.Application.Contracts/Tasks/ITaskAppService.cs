using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks
{
    public interface ITaskAppService : IApplicationService
    {
        Task<TaskDto> GetAsync(Guid id);
        Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input);
        Task<TaskDto> CreateAsync(CreateUpdateTaskDto input);
        Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input);
        Task DeleteAsync(Guid id);
        Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
    }
}