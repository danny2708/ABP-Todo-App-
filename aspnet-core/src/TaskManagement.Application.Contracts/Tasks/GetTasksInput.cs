using System; 
using Volo.Abp.Application.Dtos; 
using TaskManagement.Tasks; 

namespace TaskManagement.Tasks;

public class GetTasksInput : PagedAndSortedResultRequestDto
{
    public TaskStatus? Status { get; set; }
    public Guid? AssignedUserId { get; set; }
}