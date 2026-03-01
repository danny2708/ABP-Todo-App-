using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Permissions;
using TaskManagement.Projects;
// BẮT BUỘC: Thêm namespace Notifications
using TaskManagement.Notifications; 
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using TaskManagement.Hubs; 

namespace TaskManagement.Tasks
{
    [Authorize]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<Project, Guid> _projectRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public TaskAppService(
            ITaskRepository repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<Project, Guid> projectRepository,
            IHubContext<NotificationHub> hubContext,
            IRepository<AppNotification, Guid> notificationRepository) 
        {
            _taskRepository = repository;
            _userRepository = userRepository;
            _projectRepository = projectRepository;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository; 
        }

        public async Task<TaskDto> GetAsync(Guid id)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var task = await queryable.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) throw new UserFriendlyException(L["TaskManagement::Task Not Found"]);

            var canView = await CanViewTask(task);
            if (!canView) throw new UserFriendlyException(L["TaskManagement::No Permission"]);

            var dto = ObjectMapper.Map<AppTask, TaskDto>(task);
            
            var assignedUserIds = task.Assignments.Select(a => a.UserId).ToList();
            var users = await _userRepository.GetListAsync(u => assignedUserIds.Contains(u.Id));
            
            dto.AssignedUserIds = assignedUserIds;
            dto.AssignedUserNames = users.Select(u => u.UserName).ToList();
            dto.AssignedUserName = string.Join(", ", dto.AssignedUserNames);
            
            return dto;
        }

        public async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var currentUserId = CurrentUser.Id;
            var isBoss = await IsBossOfProject(input.ProjectId ?? Guid.Empty);

            if (!isBoss)
            {
                queryable = queryable.Where(t => 
                    t.Assignments.Any(a => a.UserId == currentUserId) || 
                    t.CreatorId == currentUserId);
            }

            queryable = queryable.Where(t => t.ProjectId == input.ProjectId && t.IsApproved == (input.IsApproved ?? true));

            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                queryable = queryable.Where(t => t.Title.Contains(input.FilterText));
            }

            var totalCount = await AsyncExecuter.CountAsync(queryable);
            var tasks = await AsyncExecuter.ToListAsync(queryable.OrderBy(input.Sorting ?? "CreationTime DESC").PageBy(input.SkipCount, input.MaxResultCount));

            var taskDtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

            foreach (var dto in taskDtos)
            {
                var taskObj = tasks.First(t => t.Id == dto.Id);
                var assignedIds = taskObj.Assignments.Select(a => a.UserId).ToList();
                var userNames = (await _userRepository.GetListAsync(u => assignedIds.Contains(u.Id))).Select(u => u.UserName);
                
                dto.AssignedUserIds = assignedIds;
                dto.AssignedUserName = userNames.Any() ? string.Join(", ", userNames) : L["Unassigned"];
            }

            return new PagedResultDto<TaskDto>(totalCount, taskDtos);
        }

        public async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            var isDuplicate = await _taskRepository.AnyAsync(t => 
                t.ProjectId == input.ProjectId && 
                t.Title.ToLower() == input.Title.ToLower() &&
                t.DueDate == input.DueDate &&
                t.Weight == input.Weight &&
                t.Description == input.Description
            );

            if (isDuplicate)
            {
                throw new UserFriendlyException(L["TaskManagement::Task Already Exists With Same Details"]);
            }

            var isBoss = await IsBossOfProject(input.ProjectId);
            var hasApprovePermission = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Approve);
            
            if (!isBoss)
            {
                var projectMembers = await _projectRepository.WithDetailsAsync(p => p.Members);
                var isMember = projectMembers.Any(p => p.Id == input.ProjectId && p.Members.Any(m => m.UserId == CurrentUser.Id));
                if (!isMember) throw new UserFriendlyException(L["TaskManagement::No Permission To Create Task"]);
            }

            var task = new AppTask(
                    GuidGenerator.Create(),
                    input.ProjectId,
                    input.Title,
                    input.Status, 
                    input.Weight  
                );

            task.Description = input.Description;
            task.DueDate = input.DueDate;
            task.IsApproved = isBoss || hasApprovePermission;

            var finalAssignedIds = new List<Guid>(input.AssignedUserIds);

            if (!isBoss && !hasApprovePermission)
            {
                var currentUserId = CurrentUser.Id ?? Guid.Empty;
                if (currentUserId != Guid.Empty && !finalAssignedIds.Contains(currentUserId))
                {
                    finalAssignedIds.Add(currentUserId);
                }
            }

            foreach (var userId in finalAssignedIds)
            {
                task.AddAssignment(userId);
            }

            await _taskRepository.InsertAsync(task);
            var resultDto = ObjectMapper.Map<AppTask, TaskDto>(task);

            if (!task.IsApproved)
            {
                // Nếu là đề xuất, gửi cho PM (Project Manager) của dự án đó
                var project = await _projectRepository.GetAsync(task.ProjectId);
                var notif = new AppNotification(GuidGenerator.Create(), project.ProjectManagerId, "Đề xuất mới", $"{CurrentUser.UserName} vừa đề xuất công việc: {task.Title}", "NewTaskProposed", task.Id);
                
                // Lưu vào Database
                await _notificationRepository.InsertAsync(notif);

                // Chuyển sang DTO và Bắn SignalR
                var notifDto = ObjectMapper.Map<AppNotification, NotificationDto>(notif);
                await _hubContext.Clients.User(project.ProjectManagerId.ToString()).SendAsync("ReceiveNotification", notifDto);
            }
            else
            {
                // Nếu PM tạo task đã duyệt, gửi cho tất cả những người được gán (trừ bản thân PM)
                foreach (var userId in finalAssignedIds.Where(id => id != CurrentUser.Id))
                {
                    var notif = new AppNotification(GuidGenerator.Create(), userId, "Công việc mới", $"Bạn được gán công việc: {task.Title}", "TaskAssigned", task.Id);
                    
                    // Lưu vào Database
                    await _notificationRepository.InsertAsync(notif);

                    // Chuyển sang DTO và Bắn SignalR
                    var notifDto = ObjectMapper.Map<AppNotification, NotificationDto>(notif);
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notifDto);
                }
            }

            return resultDto;
        }

        public async Task<TaskDto> ApproveAsync(Guid id)
        {
            var task = await _taskRepository.GetAsync(id);
            if (!await IsBossOfProject(task.ProjectId)) throw new UserFriendlyException(L["TaskManagement::No Permission"]);

            task.IsApproved = true;
            task.IsRejected = false;
            await _taskRepository.UpdateAsync(task);

            if (task.CreatorId.HasValue)
            {
                var notif = new AppNotification(GuidGenerator.Create(), task.CreatorId.Value, "Đã phê duyệt", $"Công việc '{task.Title}' của bạn đã được phê duyệt.", "TaskApproved", task.Id);
                
                // Lưu vào Database
                await _notificationRepository.InsertAsync(notif);

                // Chuyển sang DTO và Bắn SignalR
                var notifDto = ObjectMapper.Map<AppNotification, NotificationDto>(notif);
                await _hubContext.Clients.User(task.CreatorId.Value.ToString()).SendAsync("ReceiveNotification", notifDto);
            }

            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task RejectAsync(Guid id)
        {
            var task = await _taskRepository.GetAsync(id);
            if (!await IsBossOfProject(task.ProjectId)) throw new UserFriendlyException(L["TaskManagement::No Permission"]);

            task.IsRejected = true;
            await _taskRepository.UpdateAsync(task);

            if (task.CreatorId.HasValue)
            {
                var notif = new AppNotification(GuidGenerator.Create(), task.CreatorId.Value, "Bị từ chối", $"Yêu cầu công việc '{task.Title}' đã bị từ chối.", "TaskRejected", task.Id);
                
                // Lưu vào Database
                await _notificationRepository.InsertAsync(notif);

                // Chuyển sang DTO và Bắn SignalR
                var notifDto = ObjectMapper.Map<AppNotification, NotificationDto>(notif);
                await _hubContext.Clients.User(task.CreatorId.Value.ToString()).SendAsync("ReceiveNotification", notifDto);
            }
        }

        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var task = await queryable.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) throw new UserFriendlyException(L["TaskManagement::TaskNotFound"]);

            var isDuplicate = await _taskRepository.AnyAsync(t => 
                t.Id != id && 
                t.ProjectId == input.ProjectId && 
                t.Title.ToLower() == input.Title.ToLower() &&
                t.DueDate == input.DueDate &&
                t.Weight == input.Weight &&
                t.Description == input.Description
            );

            if (isDuplicate)
            {
                throw new UserFriendlyException(L["TaskManagement::Task Already Exists"]);
            }

            bool isBoss = await IsBossOfProject(task.ProjectId);
            bool isAssignedToMe = task.Assignments.Any(a => a.UserId == CurrentUser.Id);
            bool isCreator = task.CreatorId == CurrentUser.Id;
            
            if (!isBoss)
            {
                if (task.IsApproved)
                {
                    if (!isAssignedToMe) throw new UserFriendlyException(L["TaskManagement::No Permission"]);
                    if (input.Title != task.Title || input.Description != task.Description) 
                        throw new UserFriendlyException(L["TaskManagement::Cannot Edit Content After Approval"]);
                }
                else
                {
                    if (!isCreator) throw new UserFriendlyException(L["TaskManagement::No Permission"]);
                    if (input.Status != task.Status) 
                        throw new UserFriendlyException(L["TaskManagement::Cannot Change Status Before Approval"]);
                }
            }

            task.Title = input.Title;
            task.Description = input.Description;
            task.Status = input.Status;
            task.Weight = input.Weight;
            task.DueDate = input.DueDate; 
            
            if (isBoss)
            {
                task.ClearAssignments();
                foreach (var userId in input.AssignedUserIds) task.AddAssignment(userId);
            }

            await _taskRepository.UpdateAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task DeleteAsync(Guid id, string reason)
        {
            var task = await _taskRepository.GetAsync(id);
            if (string.IsNullOrWhiteSpace(reason)) throw new UserFriendlyException(L["TaskManagement::DeletionReasonRequired"]);

            bool isBoss = await IsBossOfProject(task.ProjectId);

            if (!isBoss)
            {
                if (task.IsApproved) throw new UserFriendlyException(L["TaskManagement::No Permission To Delete Approved Task"]);
                if (task.CreatorId != CurrentUser.Id) throw new UserFriendlyException(L["TaskManagement::No Permission"]);
            }
            else
            {
                if (task.Status == TaskStatus.Completed) throw new UserFriendlyException(L["TaskManagement::CannotDeleteCompletedTask"]);
            }

            task.DeletionReason = reason;
            await _taskRepository.DeleteAsync(id);
        }

        public async Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var currentUserId = CurrentUser.Id;
            var isBoss = await IsBossOfProject(projectId);

            queryable = queryable.Where(t => 
                t.ProjectId == projectId && 
                t.DueDate < Clock.Now && 
                t.IsApproved == true
            );

            if (!isBoss)
            {
                queryable = queryable.Where(t => 
                    t.Assignments.Any(a => a.UserId == currentUserId) || t.CreatorId == currentUserId
                );
            }

            var tasks = await AsyncExecuter.ToListAsync(queryable);
            var dtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

            foreach (var dto in dtos)
            {
                var taskObj = tasks.First(t => t.Id == dto.Id);
                var assignedIds = taskObj.Assignments.Select(a => a.UserId).ToList();
                
                if (assignedIds.Any())
                {
                    var users = await _userRepository.GetListAsync(u => assignedIds.Contains(u.Id));
                    dto.AssignedUserName = string.Join(", ", users.Select(u => u.UserName));
                    dto.AssignedUserIds = assignedIds;
                }
                else
                {
                    dto.AssignedUserName = L["TaskManagement::Unassigned"];
                }
            }
            return new PagedResultDto<TaskDto>(dtos.Count, dtos);
        }

        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();
            return new ListResultDto<UserLookupDto>(users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList());
        }

        private async Task<bool> IsBossOfProject(Guid projectId)
        {
            if (projectId == Guid.Empty) return false;
            var project = await _projectRepository.GetAsync(projectId);
            return project.ProjectManagerId == CurrentUser.Id || 
                await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        }

        private async Task<bool> CanViewTask(AppTask task)
        {
            if (await IsBossOfProject(task.ProjectId)) return true;
            return task.Assignments.Any(a => a.UserId == CurrentUser.Id) || task.CreatorId == CurrentUser.Id;
        }
    }
}