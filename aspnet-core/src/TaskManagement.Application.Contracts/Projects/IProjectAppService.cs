using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using TaskManagement.Tasks;

namespace TaskManagement.Projects;

public interface IProjectAppService : IApplicationService
{
    Task<ProjectDto> GetAsync(Guid id);
    Task<PagedResultDto<ProjectDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input);
    Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input);
    Task DeleteAsync(Guid id);
    Task<ListResultDto<UserLookupDto>> GetMembersLookupAsync(Guid projectId);
}