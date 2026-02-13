using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore; 
using TaskManagement.Tasks;

namespace TaskManagement.Projects;

[Authorize]
public class ProjectAppService : ApplicationService, IProjectAppService
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Tasks.AppTask, Guid> _taskRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager; // Dùng để lọc Role PM

    public ProjectAppService(
        IRepository<Project, Guid> projectRepository,
        IRepository<Tasks.AppTask, Guid> taskRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IdentityUserManager userManager)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _userManager = userManager;
    }

    public async Task<ProjectDto> GetAsync(Guid id)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        await CheckProjectAccessAsync(project);
        var dto = ObjectMapper.Map<Project, ProjectDto>(project);
        
        // FIX: Tính toán tiến độ ngay tại đây để Detail Page hiển thị đúng %
        var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == id && t.IsApproved);
        dto.TaskCount = tasks.Count;
        dto.CompletedTaskCount = tasks.Count(t => t.Status == Tasks.TaskStatus.Completed);
        dto.Progress = dto.TaskCount > 0 ? (float)dto.CompletedTaskCount / dto.TaskCount * 100 : 0;

        var manager = await _userRepository.FindAsync(project.ProjectManagerId);
        dto.ProjectManagerName = manager?.UserName ?? "Unknown";
        dto.MemberIds = project.Members.Select(m => m.UserId).ToList();
        
        return dto;
    }

    // Cập nhật: Sử dụng GetProjectsInput để nhận tham số FilterText
    public async Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);

        // 1. FIX LỖI TÌM KIẾM: Áp dụng bộ lọc từ thanh search
        queryable = queryable.WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), 
            x => x.Name.Contains(input.FilterText) || (x.Description != null && x.Description.Contains(input.FilterText)));

        // 2. PHÂN QUYỀN TÀNG HÌNH: Nhân viên/PM chỉ thấy dự án thuộc quyền hạn
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (!isAdmin)
        {
            queryable = queryable.Where(p => 
                p.ProjectManagerId == currentUserId || 
                p.Members.Any(m => m.UserId == currentUserId));
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var projects = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(input.Sorting ?? "CreationTime DESC").PageBy(input.SkipCount, input.MaxResultCount)
        );

        var projectDtos = ObjectMapper.Map<List<Project>, List<ProjectDto>>(projects);

        foreach (var dto in projectDtos)
        {
            var projectEntity = projects.First(p => p.Id == dto.Id);
            dto.MemberIds = projectEntity.Members.Select(m => m.UserId).ToList();

            var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == dto.Id && t.IsApproved);
            dto.TaskCount = tasks.Count;
            dto.CompletedTaskCount = tasks.Count(t => t.Status == Tasks.TaskStatus.Completed);
            dto.Progress = dto.TaskCount > 0 ? (float)dto.CompletedTaskCount / dto.TaskCount * 100 : 0;
            
            var manager = await _userRepository.FindAsync(dto.ProjectManagerId);
            dto.ProjectManagerName = manager?.UserName;
        }

        return new PagedResultDto<ProjectDto>(totalCount, projectDtos);
    }

    // TÍNH NĂNG MỚI: Chỉ lấy danh sách người có Role "Project manager"
    public async Task<ListResultDto<UserLookupDto>> GetProjectManagersLookupAsync()
    {
        var pmUsers = await _userManager.GetUsersInRoleAsync("Project manager");
        return new ListResultDto<UserLookupDto>(
            pmUsers.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList()
        );
    }

    [Authorize(TaskManagementPermissions.Projects.Create)]
    public async Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input)
    {
        var project = new Project(GuidGenerator.Create(), input.Name, input.ProjectManagerId) { Description = input.Description };
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }
        await _projectRepository.InsertAsync(project, autoSave: true);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (!isAdmin && project.ProjectManagerId != CurrentUser.Id)
            throw new UserFriendlyException(L["TaskManagement::NoPermissionToEditProject"]);

        project.Name = input.Name;
        project.Description = input.Description;
        project.ProjectManagerId = input.ProjectManagerId;

        project.Members.Clear();
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }

        await _projectRepository.UpdateAsync(project, autoSave: true);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    [Authorize(TaskManagementPermissions.Projects.Create)]
    public async Task DeleteAsync(Guid id) => await _projectRepository.DeleteAsync(id);

    public async Task<ListResultDto<UserLookupDto>> GetMembersLookupAsync(Guid projectId)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        await CheckProjectAccessAsync(project);

        var memberIds = project.Members.Select(m => m.UserId).ToList();
        memberIds.Add(project.ProjectManagerId); 

        var users = await _userRepository.GetListAsync(u => memberIds.Contains(u.Id));
        return new ListResultDto<UserLookupDto>(users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList());
    }

    private async Task CheckProjectAccessAsync(Project project)
    {
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (isAdmin) return;
        if (project.ProjectManagerId != CurrentUser.Id && !project.Members.Any(m => m.UserId == CurrentUser.Id))
            throw new UserFriendlyException(L["TaskManagement::NoPermissionToAccessProject"]);
    }
}