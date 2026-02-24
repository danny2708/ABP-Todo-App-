using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Projects;

namespace TaskManagement.Tasks;

[Authorize]
public class CalendarAppService : ApplicationService, ICalendarAppService
{
    private readonly IRepository<AppTask, Guid> _taskRepository;
    private readonly IRepository<Project, Guid> _projectRepository;

    public CalendarAppService(
        IRepository<AppTask, Guid> taskRepository,
        IRepository<Project, Guid> projectRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
    }

    public async Task<List<TaskDto>> GetCalendarTasksAsync(GetCalendarInput input)
    {
        var currentUserId = CurrentUser.Id;
        
        // 1. Lấy Queryable nạp kèm thông tin người nhận việc
        var taskQuery = await _taskRepository.WithDetailsAsync(t => t.Assignments);
        
        // 2. Chỉ lấy các Task đã phê duyệt và có ngày hạn (DueDate)
        var queryable = taskQuery.Where(t => t.IsApproved && t.DueDate != null);

        // 3. Lọc theo khoảng thời gian người dùng đang xem trên lịch
        if (input.StartDate.HasValue)
            queryable = queryable.Where(t => t.DueDate >= input.StartDate.Value);
        if (input.EndDate.HasValue)
            queryable = queryable.Where(t => t.DueDate <= input.EndDate.Value);

        // 4. LOGIC PHÂN QUYỀN "TÀNG HÌNH"
        bool isAdmin = await AuthorizationService.IsGrantedAsync("TaskManagement.Tasks.Approve");

        if (!isAdmin)
        {
            // Sử dụng Queryable trực tiếp từ Repository để tránh lỗi Type Inference
            var projectQuery = await _projectRepository.GetQueryableAsync();
            
            // PM: Lấy IQueryable chứa ID các dự án đang quản lý
            var managedProjectIds = projectQuery
                .Where(p => p.ProjectManagerId == currentUserId)
                .Select(p => p.Id);

            // Lọc: Nếu là PM của dự án HOẶC là Employee được giao task
            queryable = queryable.Where(t =>
                managedProjectIds.Contains(t.ProjectId) || 
                t.Assignments.Any(a => a.UserId == currentUserId)
            );
        }

        var tasks = await AsyncExecuter.ToListAsync(queryable);
        var taskDtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

        // 5. Gán thêm tên dự án để hiển thị nhãn trên lịch trình
        foreach (var dto in taskDtos)
        {
            var project = await _projectRepository.FindAsync(dto.ProjectId);
            dto.AssignedUserName = project?.Name; // Hiển thị tên dự án thay vì tên nhân viên trên lịch
        }

        return taskDtos;
    }
}