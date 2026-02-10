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
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) throw new UserFriendlyException("Không tìm thấy công việc.");

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
            // Gọi hàm Repository mới có Search/Sort
            var tasks = await _taskRepository.GetListAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Sorting,
                input.Filter,
                input.Status,
                input.AssignedUserId
            );

            var totalCount = await _taskRepository.GetTotalCountAsync(input.Filter, input.Status, input.AssignedUserId);

            // Tối ưu lấy UserName qua Dictionary
            var userIds = tasks.Where(t => t.AssignedUserId.HasValue).Select(t => t.AssignedUserId!.Value).Distinct().ToList();
            var userDict = userIds.Any() 
                ? (await _userRepository.GetListAsync(u => userIds.Contains(u.Id))).ToDictionary(u => u.Id, u => u.UserName) 
                : new Dictionary<Guid, string>();

            var items = tasks.Select(task =>
            {
                var dto = ObjectMapper.Map<AppTask, TaskDto>(task);
                // Sửa lỗi unassigned local variable bằng TryGetValue an toàn
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
            await _taskRepository.CreateTaskAsync(task); // Dùng Repository tùy chỉnh
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        [Authorize(TaskManagementPermissions.Tasks.Default)]
        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) throw new UserFriendlyException("Công việc không tồn tại.");

            var canUpdateAll = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Update);

            if (canUpdateAll)
            {
                ObjectMapper.Map(input, task);
            }
            else
            {
                // Kiểm tra Ownership: Chỉ được sửa task của chính mình
                if (task.AssignedUserId != CurrentUser.Id)
                {
                    throw new UserFriendlyException("Bạn không thể cập nhật công việc của người khác.");
                }

                // Chặn sửa Title nếu chỉ có quyền sửa Status
                if (task.Title != input.Title)
                {
                    throw new UserFriendlyException("Bạn chỉ được phép cập nhật trạng thái của công việc này.");
                }

                task.Status = input.Status;
            }

            await _taskRepository.UpdateTaskAsync(task); // Dùng Repository tùy chỉnh
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        [Authorize(TaskManagementPermissions.Tasks.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _taskRepository.DeleteTaskAsync(id); // Dùng Repository tùy chỉnh
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