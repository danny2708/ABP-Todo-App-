using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    // Kế thừa PagedAndSorted để có SkipCount, MaxResultCount và Sorting
    public class GetTasksInput : PagedAndSortedResultRequestDto 
    {
        public string? Filter { get; set; } 

        public TaskStatus? Status { get; set; }

        public Guid? AssignedUserId { get; set; }
    }
}