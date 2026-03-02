using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR; 
using TaskManagement.Hubs; 
using TaskManagement.Notifications; 
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore; 
using TaskManagement.Tasks;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace TaskManagement.Projects;

[Authorize]
public class ProjectAppService : ApplicationService, IProjectAppService
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Tasks.AppTask, Guid> _taskRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager;
    
    // TIÊM DỊCH VỤ SIGNALR VÀ NOTIFICATION
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IRepository<AppNotification, Guid> _notificationRepository;

    public ProjectAppService(
        IRepository<Project, Guid> projectRepository,
        IRepository<Tasks.AppTask, Guid> taskRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IdentityUserManager userManager,
        IHubContext<NotificationHub> hubContext,
        IRepository<AppNotification, Guid> notificationRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _hubContext = hubContext;
        _notificationRepository = notificationRepository;
    }

    public async Task<ProjectDto> GetAsync(Guid id)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null) 
            throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        await CheckProjectAccessAsync(project);
        
        var dto = ObjectMapper.Map<Project, ProjectDto>(project);
        
        await CalculateProjectStatsAsync(dto);

        var manager = await _userRepository.FindAsync(project.ProjectManagerId);
        dto.ProjectManagerName = manager?.UserName ?? "Unknown";
        dto.MemberIds = project.Members.Select(m => m.UserId).ToList();
        
        return dto;
    }

    public async Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);

        queryable = queryable.WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), 
            x => x.Name.Contains(input.FilterText) || (x.Description != null && x.Description.Contains(input.FilterText)));

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
            dto.MemberCount = dto.MemberIds.Count;

            await CalculateProjectStatsAsync(dto);
            
            var manager = await _userRepository.FindAsync(dto.ProjectManagerId);
            dto.ProjectManagerName = manager?.UserName;
        }

        return new PagedResultDto<ProjectDto>(totalCount, projectDtos);
    }

    private async Task CalculateProjectStatsAsync(ProjectDto dto)
    {
        var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == dto.Id && t.IsApproved);
        
        dto.TaskCount = tasks.Count;
        dto.CompletedTaskCount = tasks.Count(t => t.Status == Tasks.TaskStatus.Completed);

        if (dto.TaskCount > 0)
        {
            int totalWeight = tasks.Sum(t => t.Weight);
            int completedWeight = tasks
                .Where(t => t.Status == Tasks.TaskStatus.Completed)
                .Sum(t => t.Weight);

            dto.Progress = totalWeight > 0 
                ? (float)Math.Round((double)completedWeight / totalWeight * 100, 2) 
                : 0;
        }
        else
        {
            dto.Progress = 0;
        }
    }

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

        foreach (var userId in input.MemberIds.Where(id => id != CurrentUser.Id))
        {
            var notif = new AppNotification(GuidGenerator.Create(), userId, "Dự án mới", $"Bạn được thêm vào dự án: {project.Name}", "ProjectAssigned", project.Id);
            await _notificationRepository.InsertAsync(notif);
            
            var notifDto = ObjectMapper.Map<AppNotification, NotificationDto>(notif);
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notifDto);
        }

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

        var oldMemberIds = project.Members.Select(m => m.UserId).ToList();

        project.Name = input.Name;
        project.Description = input.Description;
        project.ProjectManagerId = input.ProjectManagerId;

        project.Members.Clear();
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }

        var addedMemberIds = input.MemberIds.Except(oldMemberIds).ToList();
        var removedMemberIds = oldMemberIds.Except(input.MemberIds).ToList();

        // Gửi thông báo cho người mới
        foreach (var userId in addedMemberIds)
        {
            var notif = new AppNotification(GuidGenerator.Create(), userId, "Dự án mới", $"Bạn vừa được thêm vào dự án: {project.Name}", "ProjectAssigned", project.Id);
            await _notificationRepository.InsertAsync(notif);
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", ObjectMapper.Map<AppNotification, NotificationDto>(notif));
        }

        // Gửi thông báo cho người bị xóa
        foreach (var userId in removedMemberIds)
        {
            var notif = new AppNotification(GuidGenerator.Create(), userId, "Rời dự án", $"Bạn không còn trong dự án: {project.Name}", "ProjectRemoved", project.Id);
            await _notificationRepository.InsertAsync(notif);
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", ObjectMapper.Map<AppNotification, NotificationDto>(notif));
        }

        if (removedMemberIds.Any())
        {
            var tasks = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var projectTasks = await AsyncExecuter.ToListAsync(tasks.Where(t => t.ProjectId == id));

            foreach (var task in projectTasks)
            {
                bool isTaskUpdated = false;
                foreach (var removedId in removedMemberIds)
                {
                    if (task.Assignments.Any(a => a.UserId == removedId))
                    {
                        task.RemoveAssignment(removedId);
                        isTaskUpdated = true;
                    }
                }

                if (isTaskUpdated) await _taskRepository.UpdateAsync(task);
            }
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