using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class AppTask : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; } = default!;
    public string Description { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? AssignedUserId { get; set; }
}