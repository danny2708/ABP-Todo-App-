using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManagement.Tasks
{
public class CreateUpdateTaskDto
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        
        // Danh sách nhiều người nhận việc
        public List<Guid> AssignedUserIds { get; set; } = new();
    }
}