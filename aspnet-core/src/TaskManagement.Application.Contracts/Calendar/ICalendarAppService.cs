// aspnet-core\src\TaskManagement.Application.Contracts\Calendar\ICalendarAppService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Tasks;
using Volo.Abp; 
using Volo.Abp.Application.Services;

namespace TaskManagement.Calendar;

[RemoteService(IsEnabled = true)] // Ép hệ thống nhận diện
public interface ICalendarAppService : IApplicationService
{
    // Truyền trực tiếp biến thay vì dùng Object
    Task<List<TaskDto>> GetCalendarTasksAsync(DateTime? startDate, DateTime? endDate);
}