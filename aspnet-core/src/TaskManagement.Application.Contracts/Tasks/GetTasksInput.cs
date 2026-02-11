using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class GetTasksInput : PagedAndSortedResultRequestDto 
    {
        public string? FilterText { get; set; }

        public TaskStatus? Status { get; set; }

        public Guid? AssignedUserId { get; set; }
    }
}