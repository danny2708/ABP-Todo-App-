using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tasks
{
    public class CreateUpdateTaskDto
    {
        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        public string Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.New;

        public Guid? AssignedUserId { get; set; }
    }
}
