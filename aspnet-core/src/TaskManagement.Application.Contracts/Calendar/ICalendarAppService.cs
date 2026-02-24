using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks;

public interface ICalendarAppService : IApplicationService
{
    // Lấy danh sách task cho lịch trình
    Task<List<TaskDto>> GetCalendarTasksAsync(GetCalendarInput input);
}