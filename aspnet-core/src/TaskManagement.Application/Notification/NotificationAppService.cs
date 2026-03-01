using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users; // Cần thiết cho CurrentUser.GetId()
using Volo.Abp;       // QUAN TRỌNG: Cần thiết cho [RemoteService]

namespace TaskManagement.Notifications
{
    // Thêm Attribute này để ép ABP tạo API Controller
    [RemoteService(Name = "Notification")] 
    [Authorize]
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public NotificationAppService(IRepository<AppNotification, Guid> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync()
        {
            var userId = CurrentUser.GetId(); 
            var queryable = await _notificationRepository.GetQueryableAsync();
            
            var notifications = queryable.Where(n => n.UserId == userId)
                                         .OrderByDescending(n => n.CreationTime)
                                         .Take(20)
                                         .ToList();

            return ObjectMapper.Map<List<AppNotification>, List<NotificationDto>>(notifications);
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _notificationRepository.GetAsync(id);
            if (notification.UserId == CurrentUser.GetId())
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);
            }
        }
    }
}