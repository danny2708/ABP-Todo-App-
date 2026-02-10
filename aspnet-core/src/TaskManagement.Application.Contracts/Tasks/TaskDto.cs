using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class TaskDto : AuditedEntityDto<Guid>
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
}