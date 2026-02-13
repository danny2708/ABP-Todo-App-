using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Permissions;
using TaskManagement.Projects;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Tasks
{
    [Authorize]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<Project, Guid> _projectRepository;

        public TaskAppService(
            ITaskRepository repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<Project, Guid> projectRepository)
        {
            _taskRepository = repository;
            _userRepository = userRepository;
            _projectRepository = projectRepository;
        }

        public async Task<TaskDto> GetAsync(Guid id)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var task = await queryable.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) throw new UserFriendlyException(L["TaskManagement::TaskNotFound"]);

            var dto = ObjectMapper.Map<AppTask, TaskDto>(task);
            var assignedUserIds = task.Assignments.Select(a => a.UserId).ToList();
            var users = await _userRepository.GetListAsync(u => assignedUserIds.Contains(u.Id));
            dto.AssignedUserNames = users.Select(u => u.UserName).ToList();
            dto.AssignedUserName = string.Join(", ", dto.AssignedUserNames);
            
            return dto;
        }

        public async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var currentUserId = CurrentUser.Id;
            var isBoss = await IsBossOfProject(input.ProjectId ?? Guid.Empty);

            // BẢO MẬT: Nhân viên chỉ thấy task giao cho họ hoặc do họ đề xuất
            if (!isBoss)
            {
                queryable = queryable.Where(t => 
                    t.Assignments.Any(a => a.UserId == currentUserId) || t.CreatorId == currentUserId);
            }

            // PHÂN LUỒNG: Bảng chính (Approved) vs Bảng chờ duyệt (Pending)
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
                dto.AssignedUserName = userNames.Any() ? string.Join(", ", userNames) : L["TaskManagement::Unassigned"];
                dto.AssignedUserIds = assignedIds;
            }

            return new PagedResultDto<TaskDto>(totalCount, taskDtos);
        }

        public async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            // Kiểm tra xem User có thuộc dự án này không trước khi cho phép tạo
            var project = await _projectRepository.WithDetailsAsync(p => p.Members);
            var projectEntity = await project.FirstOrDefaultAsync(p => p.Id == input.ProjectId);
            
            bool isBoss = projectEntity.ProjectManagerId == CurrentUser.Id || await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Create);
            bool isMember = projectEntity.Members.Any(m => m.UserId == CurrentUser.Id);

            if (!isBoss && !isMember) throw new UserFriendlyException(L["TaskManagement::NoPermissionToCreateTaskInThisProject"]);

            var task = new AppTask(GuidGenerator.Create(), input.ProjectId, input.Title)
            {
                Description = input.Description,
                DueDate = input.DueDate,
                IsApproved = isBoss, // Sếp tạo -> Duyệt luôn. Nhân viên -> Chờ duyệt
                IsRejected = false
            };

            foreach (var userId in input.AssignedUserIds)
            {
                task.AddAssignment(userId);
            }

            await _taskRepository.InsertAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task<TaskDto> ApproveAsync(Guid id)
        {
            var task = await _taskRepository.GetAsync(id);
            if (!await IsBossOfProject(task.ProjectId)) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);

            task.IsApproved = true;
            task.IsRejected = false;
            await _taskRepository.UpdateAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task RejectAsync(Guid id)
        {
            var task = await _taskRepository.GetAsync(id);
            if (!await IsBossOfProject(task.ProjectId)) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);

            task.IsRejected = true; // Mark as rejected
            await _taskRepository.UpdateAsync(task);
        }

        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var task = await queryable.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) throw new UserFriendlyException(L["TaskManagement::TaskNotFound"]);

            bool isBoss = await IsBossOfProject(task.ProjectId);
            bool isAssignedToMe = task.Assignments.Any(a => a.UserId == CurrentUser.Id);

            // BẢO MẬT: Chỉ nhân viên được giao task mới được cập nhật trạng thái
            if (!isBoss && !isAssignedToMe) throw new UserFriendlyException(L["TaskManagement::NoPermissionToUpdateThisTask"]);

            // KHÓA: Nhân viên chỉ được đổi Status, sếp được đổi tất cả
            if (!isBoss)
            {
                if (task.Title != input.Title || task.Description != input.Description)
                    throw new UserFriendlyException(L["TaskManagement::OnlyBossCanEditTaskContent"]);
            }

            ObjectMapper.Map(input, task);
            
            if (isBoss) // Chỉ sếp mới được đổi người nhận việc
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
            
            // Chỉ xóa được task chưa Completed
            if (task.Status == TaskStatus.Completed) throw new UserFriendlyException(L["TaskManagement::CannotDeleteCompletedTask"]);

            await _taskRepository.DeleteAsync(id);
        }

        private async Task<bool> IsBossOfProject(Guid projectId)
        {
            var project = await _projectRepository.GetAsync(projectId);
            return project.ProjectManagerId == CurrentUser.Id || await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Create);
        }

        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();
            return new ListResultDto<UserLookupDto>(users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList());
        }

        public async Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId)
        {
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var tasks = await queryable.Where(t => t.ProjectId == projectId && t.DueDate < Clock.Now).ToListAsync();
            
            var dtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);
            foreach (var dto in dtos)
            {
                var taskObj = tasks.First(t => t.Id == dto.Id);
                var assignedIds = taskObj.Assignments.Select(a => a.UserId).ToList();
                var userNames = (await _userRepository.GetListAsync(u => assignedIds.Contains(u.Id))).Select(u => u.UserName);
                dto.AssignedUserName = string.Join(", ", userNames); 
            }
            return new PagedResultDto<TaskDto>(dtos.Count, dtos);
        }
    }
}