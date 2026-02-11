using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class TaskDto : AuditedEntityDto<Guid>
{
    public string Title { get; set; } = default!;
    public string Description { get; set; }
    public TaskStatus Status { get; set; }
    
    public Guid ProjectId { get; set; } // Task phải thuộc dự án nào đó
    public Guid? AssignedUserId { get; set; }
    public string AssignedUserName { get; set; }
    
    public DateTime? DueDate { get; set; } // Hạn chót
    public bool IsApproved { get; set; } // Trạng thái phê duyệt đề xuất
}