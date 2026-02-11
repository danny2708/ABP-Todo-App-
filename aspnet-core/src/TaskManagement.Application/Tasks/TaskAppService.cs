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
            // Lấy Queryable từ Repository để tự định nghĩa logic filter
            var queryable = await _taskRepository.GetQueryableAsync();

            // Logic tìm kiếm: Sử dụng FilterText từ GetTasksInput
            var filter = input.FilterText?.ToLower().Trim(); 
            
            queryable = queryable
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x =>
                    x.Title.ToLower().Contains(filter!) || 
                    (x.Description != null && x.Description.ToLower().Contains(filter!))
                )
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status)
                .WhereIf(input.AssignedUserId.HasValue, x => x.AssignedUserId == input.AssignedUserId);

            // Đếm tổng số bản ghi sau khi lọc
            var totalCount = await AsyncExecuter.CountAsync(queryable);

            // Áp dụng sắp xếp và phân trang
            if (string.IsNullOrWhiteSpace(input.Sorting))
            {
                input.Sorting = "CreationTime DESC";
            }

            var tasks = await AsyncExecuter.ToListAsync(
                queryable.OrderBy(input.Sorting).PageBy(input.SkipCount, input.MaxResultCount)
            );

            var taskDtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

            // Lấy danh sách UserName tập trung để tối ưu
            var userIds = tasks.Where(t => t.AssignedUserId.HasValue).Select(t => t.AssignedUserId!.Value).Distinct().ToList();
            var userDict = userIds.Any() 
                ? (await _userRepository.GetListAsync(u => userIds.Contains(u.Id))).ToDictionary(u => u.Id, u => u.UserName) 
                : new Dictionary<Guid, string>();

            foreach (var dto in taskDtos)
            {
                if (dto.AssignedUserId.HasValue && userDict.TryGetValue(dto.AssignedUserId.Value, out var userName))
                {
                    dto.AssignedUserName = userName;
                }
            }

            return new PagedResultDto<TaskDto>(totalCount, taskDtos);
        }

        [Authorize(TaskManagementPermissions.Tasks.Create)]
        public async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            var task = ObjectMapper.Map<CreateUpdateTaskDto, AppTask>(input);
            await _taskRepository.CreateTaskAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        [Authorize(TaskManagementPermissions.Tasks.Default)]
        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) throw new UserFriendlyException("Công việc không tồn tại.");

            // Logic Map đơn giản, bỏ qua phân quyền phức tạp
            ObjectMapper.Map(input, task);

            await _taskRepository.UpdateTaskAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        [Authorize(TaskManagementPermissions.Tasks.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _taskRepository.DeleteTaskAsync(id);
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