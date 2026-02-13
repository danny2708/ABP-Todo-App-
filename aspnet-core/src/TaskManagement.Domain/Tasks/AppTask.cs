using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        
        // Hạn chót (Deadline)
        public DateTime? DueDate { get; set; }
        
        // Trạng thái phê duyệt
        public bool IsApproved { get; set; }
        
        // Trạng thái bị từ chối
        public bool IsRejected { get; set; }
        
        // Lý do xóa 
        public string? DeletionReason { get; set; }

        // Giao việc cho nhiều người (Quan hệ 1-N tới bảng trung gian)
        public ICollection<TaskAssignment> Assignments { get; set; }

        protected AppTask() 
        {
            Assignments = new Collection<TaskAssignment>();
        }

        public AppTask(Guid id, Guid projectId, string title) : base(id)
        {
            ProjectId = projectId;
            Title = title;
            Status = TaskStatus.New;
            IsApproved = true; 
            IsRejected = false;
            Assignments = new Collection<TaskAssignment>();
        }

        // Helper để thêm người nhận việc vào danh sách
        public void AddAssignment(Guid userId)
        {
            Assignments.Add(new TaskAssignment(Id, userId));
        }

        // Helper để làm sạch danh sách người nhận (dùng khi Update)
        public void ClearAssignments()
        {
            Assignments.Clear();
        }
    }

    // Thực thể trung gian để quản lý việc giao cho nhiều người
    public class TaskAssignment
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }

        public TaskAssignment(Guid taskId, Guid userId)
        {
            TaskId = taskId;
            UserId = userId;
        }
    }
}