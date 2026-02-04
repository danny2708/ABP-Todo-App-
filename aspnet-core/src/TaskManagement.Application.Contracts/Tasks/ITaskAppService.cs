using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks;

public interface ITaskAppService : 
    ICrudAppService< 
        TaskDto, 
        Guid, 
        GetTasksInput, 
        CreateUpdateTaskDto>
{
    // Thêm phương thức để lấy danh sách User cho Dropdown
    Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
}