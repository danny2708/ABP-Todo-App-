using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tasks
{
    public class CreateUpdateTaskDto
    {
        [Required]
        public Guid ProjectId { get; set; } // ID dự án chứa task này

        [Required] [StringLength(128)]
        public string Title { get; set; } = default!;
        public string Description { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.New;
        public Guid? AssignedUserId { get; set; }
        public DateTime? DueDate { get; set; } // Thêm hạn chót khi tạo
    }
}