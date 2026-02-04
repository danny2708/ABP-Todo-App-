using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace TaskManagement.Tasks
{
    public class TaskAppService :
        CrudAppService<AppTask, TaskDto, Guid, GetTasksInput, CreateUpdateTaskDto>,
        ITaskAppService
    {
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public TaskAppService(
            IRepository<AppTask, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository)
            : base(repository)
        {
            _userRepository = userRepository;

            CreatePolicyName = TaskManagementPermissions.Tasks.Create;
            UpdatePolicyName = TaskManagementPermissions.Tasks.Update;
            DeletePolicyName = TaskManagementPermissions.Tasks.Delete;
        }

        protected override async Task<IQueryable<AppTask>> CreateFilteredQueryAsync(GetTasksInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            return query
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status)
                .WhereIf(input.AssignedUserId.HasValue, x => x.AssignedUserId == input.AssignedUserId);
        }

        // Override để trả kèm AssignedUserName
        public override async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            var query = await CreateFilteredQueryAsync(input);

            var totalCount = await AsyncExecuter.CountAsync(query);

            var tasks = await AsyncExecuter.ToListAsync(
                ApplySorting(query, input)
                    .PageBy(input)
            );

            var userIds = tasks
                .Where(t => t.AssignedUserId.HasValue)
                .Select(t => t.AssignedUserId!.Value)
                .Distinct()
                .ToList();

            var users = userIds.Any()
                ? await _userRepository.GetListAsync(u => userIds.Contains(u.Id))
                : new List<IdentityUser>();

            var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

            var items = tasks.Select(task =>
            {
                var dto = ObjectMapper.Map<AppTask, TaskDto>(task);

                dto.AssignedUserName =
                    task.AssignedUserId.HasValue &&
                    userDict.TryGetValue(task.AssignedUserId.Value, out var userName)
                        ? userName
                        : null;

                return dto;
            }).ToList();

            return new PagedResultDto<TaskDto>(totalCount, items);
        }

        // Lookup users for dropdown
        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();

            return new ListResultDto<UserLookupDto>(
                users.Select(u => new UserLookupDto
                {
                    Id = u.Id,
                    UserName = u.UserName
                }).ToList()
            );
        }
    }
}
