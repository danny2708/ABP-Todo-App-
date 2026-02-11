using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks
{
    public class AppTask : FullAuditedAggregateRoot<Guid>
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        
        // Thuộc dự án nào?
        public Guid ProjectId { get; set; }
        
        // Người thực hiện
        public Guid? AssignedUserId { get; set; }
        
        // Hạn chót (Deadline) - Requirement 5
        public DateTime? DueDate { get; set; }
        
        // Trạng thái phê duyệt (True = Task chuẩn, False = Đề xuất)
        public bool IsApproved { get; set; }

        protected AppTask() { }

        public AppTask(Guid id, Guid projectId, string title) : base(id)
        {
            ProjectId = projectId;
            Title = title;
            Status = TaskStatus.New;
            IsApproved = true; // Mặc định true nếu Admin tạo
        }
    }
}