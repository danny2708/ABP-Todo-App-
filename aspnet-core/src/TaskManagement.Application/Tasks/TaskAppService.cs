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

namespace TaskManagement.Tasks
{
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public TaskAppService(
            ITaskRepository repository,
            IRepository<IdentityUser, Guid> userRepository)
        {
            _taskRepository = repository;
            _userRepository = userRepository;
        }

        public async Task<TaskDto> GetAsync(Guid id)
        {
            var task = await _taskRepository.GetAsync(id);
            var dto = ObjectMapper.Map<AppTask, TaskDto>(task);
            
            if (task.AssignedUserId.HasValue)
            {
                var user = await _userRepository.GetAsync(task.AssignedUserId.Value);
                dto.AssignedUserName = user.UserName;
            }
            
            return dto;
        }

        public async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            var query = (await _taskRepository.GetQueryableAsync())
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status)
                .WhereIf(input.AssignedUserId.HasValue, x => x.AssignedUserId == input.AssignedUserId);

            var totalCount = await AsyncExecuter.CountAsync(query);
            var tasks = await AsyncExecuter.ToListAsync(query.PageBy(input));

            var userIds = tasks
                .Where(t => t.AssignedUserId.HasValue)
                .Select(t => t.AssignedUserId!.Value)
                .Distinct()
                .ToList();

            var userDict = new Dictionary<Guid, string>();
            if (userIds.Any())
            {
                var users = await _userRepository.GetListAsync(u => userIds.Contains(u.Id));
                userDict = users.ToDictionary(u => u.Id, u => u.UserName);
            }

            var items = tasks.Select(task =>
            {
                var dto = ObjectMapper.Map<AppTask, TaskDto>(task);
                if (task.AssignedUserId.HasValue && userDict.TryGetValue(task.AssignedUserId.Value, out var userName))
                {
                    dto.AssignedUserName = userName;
                }
                return dto;
            }).ToList();

            return new PagedResultDto<TaskDto>(totalCount, items);
        }

        [Authorize(TaskManagementPermissions.Tasks.Create)]
        public async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            var task = ObjectMapper.Map<CreateUpdateTaskDto, AppTask>(input);
            await _taskRepository.InsertAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        [Authorize(TaskManagementPermissions.Tasks.Default)]
        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            var task = await _taskRepository.GetAsync(id);

            // 1. Kiểm tra xem người dùng có quyền Update toàn diện (Admin/Manager) không
            var canUpdateAll = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Update);

            if (canUpdateAll)
            {
                // Nếu là Admin: Cho phép map toàn bộ dữ liệu mới
                ObjectMapper.Map(input, task);
            }
            else
            {
                // 2. Nếu là User thường: Kiểm tra tính chính chủ (Ownership)
                // CurrentUser.Id lấy từ phiên đăng nhập an toàn
                if (task.AssignedUserId != CurrentUser.Id)
                {
                    throw new UserFriendlyException("Bạn không thể cập nhật công việc được giao cho người khác.");
                }

                // 3. Kiểm tra xem có cố tình sửa các trường bị cấm (Title, AssignedUser) không
                if (task.Title != input.Title || task.AssignedUserId != input.AssignedUserId)
                {
                    throw new UserFriendlyException("Bạn chỉ có quyền cập nhật trạng thái (Status) cho công việc của mình.");
                }

                // Chỉ cập nhật trạng thái
                task.Status = input.Status;
            }

            await _taskRepository.UpdateAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        [Authorize(TaskManagementPermissions.Tasks.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _taskRepository.DeleteAsync(id);
        }

        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();
            return new ListResultDto<UserLookupDto>(
                users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList()
            );
        }
    }
}