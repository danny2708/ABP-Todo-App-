using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Notifications
{
    [Authorize]
    public class NotificationAppService : ApplicationService
    {
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public NotificationAppService(IRepository<AppNotification, Guid> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        // Lấy top 20 thông báo mới nhất của user đang đăng nhập
        public async Task<List<AppNotification>> GetMyNotificationsAsync()
        {
            var userId = CurrentUser.Id.Value;
            var query = await _notificationRepository.GetQueryableAsync();
            
            var notifications = query.Where(n => n.UserId == userId)
                                     .OrderByDescending(n => n.CreationTime)
                                     .Take(20)
                                     .ToList();
            return notifications;
        }

        // Đánh dấu đã đọc
        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _notificationRepository.GetAsync(id);
            if (notification.UserId == CurrentUser.Id)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);
            }
        }
    }
}