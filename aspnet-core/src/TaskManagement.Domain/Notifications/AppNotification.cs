using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Notifications
{
    // Kế thừa CreationAuditedEntity để hệ thống tự động lưu CreationTime (Thời gian tạo)
    public class AppNotification : CreationAuditedEntity<Guid>
    {
        public Guid UserId { get; set; } // Người nhận thông báo
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // VD: "NewTask", "TaskApproved"
        public bool IsRead { get; set; } // Trạng thái đã đọc hay chưa
        public Guid? RelatedTaskId { get; set; } // Link tới Task cụ thể (nếu có)

        protected AppNotification() { }

        public AppNotification(Guid id, Guid userId, string title, string message, string type, Guid? relatedTaskId = null)
        {
            Id = id;
            UserId = userId;
            Title = title;
            Message = message;
            Type = type;
            IsRead = false;
            RelatedTaskId = relatedTaskId;
        }
    }
}