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

        // 1. Lấy chi tiết 1 Task
        public async Task<TaskDto> GetAsync(Guid id)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) throw new UserFriendlyException("Không tìm thấy công việc.");

            var dto = ObjectMapper.Map<AppTask, TaskDto>(task);
            
            if (task.AssignedUserId.HasValue)
            {
                var user = await _userRepository.FindAsync(task.AssignedUserId.Value);
                dto.AssignedUserName = user?.UserName;
            }
            
            return dto;
        }

        // 2. Lấy danh sách Task theo Project (Có phân trang, lọc, sắp xếp)
        public async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            if (!input.ProjectId.HasValue) 
                throw new UserFriendlyException("Vui lòng chọn một dự án cụ thể.");

            // Lấy tổng số lượng dựa trên bộ lọc
            var totalCount = await _taskRepository.GetTotalCountAsync(
                input.ProjectId, 
                input.FilterText, 
                input.Status, 
                input.AssignedUserId, 
                input.IsApproved ?? true // Mặc định chỉ hiện các task đã duyệt
            );

            // Truy vấn danh sách Task
            var tasks = await _taskRepository.GetListAsync(
                input.SkipCount, 
                input.MaxResultCount, 
                input.Sorting ?? "CreationTime DESC", 
                input.ProjectId, 
                input.FilterText, 
                input.Status, 
                input.AssignedUserId, 
                input.IsApproved ?? true
            );

            var taskDtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

            // Tối ưu hiệu năng: Lấy danh sách UserName bằng Dictionary
            var userIds = tasks.Where(t => t.AssignedUserId.HasValue)
                               .Select(t => t.AssignedUserId!.Value)
                               .Distinct().ToList();
            
            var userDict = userIds.Any() 
                ? (await _userRepository.GetListAsync(u => userIds.Contains(u.Id)))
                  .ToDictionary(u => u.Id, u => u.UserName) 
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

        // 3. Tạo Task mới (Admin/PM tạo thẳng, User tạo là đề xuất)
        public async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            var project = await _projectRepository.GetAsync(input.ProjectId);
            
            // Kiểm tra xem người tạo có phải Admin hoặc Project Manager không
            var isManagerOrAdmin = project.ProjectManagerId == CurrentUser.Id || 
                                   await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Create);

            var task = new AppTask(GuidGenerator.Create(), input.ProjectId, input.Title)
            {
                Description = input.Description,
                Status = input.Status,
                AssignedUserId = input.AssignedUserId,
                DueDate = input.DueDate,
                IsApproved = isManagerOrAdmin // Nếu không phải sếp, task sẽ ở trạng thái chờ duyệt
            };

            await _taskRepository.CreateTaskAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        // 4. Phê duyệt đề xuất Task
        public async Task<TaskDto> ApproveAsync(Guid id)
        {
            var task = await _taskRepository.GetAsync(id);
            var project = await _projectRepository.GetAsync(task.ProjectId);

            // Chỉ PM của dự án đó hoặc Admin mới có quyền duyệt
            if (project.ProjectManagerId != CurrentUser.Id && 
                !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Create))
            {
                throw new UserFriendlyException("Bạn không có quyền phê duyệt công việc cho dự án này.");
            }

            task.IsApproved = true;
            await _taskRepository.UpdateAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        // 5. Cập nhật thông tin Task
        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) throw new UserFriendlyException("Công việc không tồn tại.");

            ObjectMapper.Map(input, task);
            await _taskRepository.UpdateTaskAsync(task);
            
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        // 6. Xóa Task
        public async Task DeleteAsync(Guid id)
        {
            await _taskRepository.DeleteTaskAsync(id);
        }

        // 7. Lấy danh sách Task quá hạn (Overdue)
        public async Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId)
        {
            var queryable = await _taskRepository.GetQueryableAsync();
            
            // Lọc: Thuộc dự án, Hạn chót < hiện tại, Chưa hoàn thành, Đã được duyệt
            var overdueTasks = queryable.Where(t => t.ProjectId == projectId && 
                                                    t.DueDate < Clock.Now && 
                                                    t.Status != TaskStatus.Completed &&
                                                    t.IsApproved);
            
            var totalCount = await AsyncExecuter.CountAsync(overdueTasks);
            var tasks = await AsyncExecuter.ToListAsync(overdueTasks.OrderBy(t => t.DueDate));
            
            return new PagedResultDto<TaskDto>(totalCount, ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks));
        }

        // 8. Lookup danh sách User cho Dropdown
        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();
            return new ListResultDto<UserLookupDto>(
                users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList()
            );
        }
    }
}