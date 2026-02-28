using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Notifications
{
    public class NotificationDto : EntityDto<Guid>
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public Guid? RelatedTaskId { get; set; }
        public DateTime CreationTime { get; set; }
    }
}