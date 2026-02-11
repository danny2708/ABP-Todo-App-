using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Projects;

public class ProjectDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; } = default!;
    public string Description { get; set; }
    public Guid ProjectManagerId { get; set; }
    public string ProjectManagerName { get; set; } // Hiển thị tên quản lý
    public float Progress { get; set; } // % Tiến độ dự án
    public int TaskCount { get; set; } // Tổng số task
    public int CompletedTaskCount { get; set; } // Số task đã xong
}