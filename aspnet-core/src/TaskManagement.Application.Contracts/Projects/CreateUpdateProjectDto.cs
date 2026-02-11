using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Projects;

public class CreateUpdateProjectDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = default!;

    public string Description { get; set; }

    [Required]
    public Guid ProjectManagerId { get; set; } // Bắt buộc chọn PM

    public List<Guid> MemberIds { get; set; } = new(); // Danh sách thành viên tham gia
}