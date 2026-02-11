using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;

namespace TaskManagement.Projects;

[Authorize]
public class ProjectAppService : ApplicationService, IProjectAppService
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Tasks.AppTask, Guid> _taskRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public ProjectAppService(
        IRepository<Project, Guid> projectRepository,
        IRepository<Tasks.AppTask, Guid> taskRepository,
        IRepository<IdentityUser, Guid> userRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
    }

    public async Task<ProjectDto> GetAsync(Guid id)
    {
        var project = await _projectRepository.GetAsync(id);
        var dto = ObjectMapper.Map<Project, ProjectDto>(project);
        
        var manager = await _userRepository.GetAsync(project.ProjectManagerId);
        dto.ProjectManagerName = manager.UserName;
        
        return dto;
    }

    public async Task<PagedResultDto<ProjectDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _projectRepository.GetQueryableAsync();

        // PHÂN QUYỀN: User chỉ thấy dự án mình tham gia, PM thấy dự án mình quản lý, Admin thấy hết
        if (!await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Create)) // Giả sử quyền Create là của Admin/PM
        {
            queryable = queryable.Where(p => p.Members.Any(m => m.UserId == currentUserId) || p.ProjectManagerId == currentUserId);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var projects = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(input.Sorting ?? "CreationTime DESC").PageBy(input.SkipCount, input.MaxResultCount)
        );

        var projectDtos = ObjectMapper.Map<List<Project>, List<ProjectDto>>(projects);

        // TÍNH TOÁN TIẾN ĐỘ VÀ THÔNG TIN PHỤ
        foreach (var dto in projectDtos)
        {
            var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == dto.Id && t.IsApproved);
            dto.TaskCount = tasks.Count;
            dto.CompletedTaskCount = tasks.Count(t => t.Status == Tasks.TaskStatus.Completed);
            dto.Progress = dto.TaskCount > 0 ? (float)dto.CompletedTaskCount / dto.TaskCount * 100 : 0;
            
            var manager = await _userRepository.FindAsync(dto.ProjectManagerId);
            dto.ProjectManagerName = manager?.UserName;
        }

        return new PagedResultDto<ProjectDto>(totalCount, projectDtos);
    }

    [Authorize(TaskManagementPermissions.Tasks.Create)] // Chỉ Admin mới có quyền tạo dự án
    public async Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input)
    {
        var project = new Project(GuidGenerator.Create(), input.Name, input.ProjectManagerId)
        {
            Description = input.Description
        };

        // Thêm các thành viên vào dự án
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }

        await _projectRepository.InsertAsync(project);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
    {
        var project = await _projectRepository.GetAsync(id);
        
        // KIỂM TRA QUYỀN: Chỉ Admin hoặc PM của chính dự án đó mới được sửa
        if (project.ProjectManagerId != CurrentUser.Id && !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Create))
        {
            throw new UserFriendlyException("Bạn không có quyền chỉnh sửa dự BW án này.");
        }

        project.Name = input.Name;
        project.Description = input.Description;
        project.ProjectManagerId = input.ProjectManagerId;

        // Cập nhật danh sách thành viên (xóa cũ thêm mới)
        project.Members.Clear();
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }

        await _projectRepository.UpdateAsync(project);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _projectRepository.DeleteAsync(id);
    }
}