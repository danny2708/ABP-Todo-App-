using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Hubs;
using TaskManagement.Notifications;
using TaskManagement.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace TaskManagement.Workers
{
    public class OverdueTaskWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public OverdueTaskWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory
        ) : base(timer, serviceScopeFactory)
        {
            // Thiết lập chu kỳ quét: 600,000ms = 10 phút
            Timer.Period = 600000; 
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            // Giải phóng Service từ Scope để tránh lỗi tràn bộ nhớ (Memory Leak)
            var taskRepository = workerContext.ServiceProvider.GetRequiredService<ITaskRepository>();
            var notificationRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<AppNotification, Guid>>();
            var hubContext = workerContext.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
            var objectMapper = workerContext.ServiceProvider.GetRequiredService<IObjectMapper>();

            // 1. Tìm các Task đã quá hạn (DueDate < Now) và chưa hoàn thành
            var overdueTasks = await taskRepository.GetListAsync(t => 
                t.DueDate < DateTime.Now && 
                t.Status != TaskStatus.Completed &&
                t.IsApproved == true
            );

            foreach (var task in overdueTasks)
            {
                // 2. Kiểm tra xem đã gửi thông báo quá hạn cho Task này chưa để tránh spam
                var alreadyNotified = await notificationRepository.AnyAsync(n => 
                    n.RelatedTaskId == task.Id && 
                    n.Type == "TaskOverdue"
                );

                if (!alreadyNotified)
                {
                    // 3. Gửi thông báo cho tất cả nhân viên được gán vào Task này
                    foreach (var assignment in task.Assignments)
                    {
                        var notification = new AppNotification(
                            Guid.NewGuid(),
                            assignment.UserId,
                            "Cảnh báo quá hạn",
                            $"Công việc '{task.Title}' đã quá hạn hoàn thành. Vui lòng cập nhật tiến độ!",
                            "TaskOverdue",
                            task.Id
                        );

                        // Lưu vào Database để đảm bảo F5 không mất
                        await notificationRepository.InsertAsync(notification);

                        // Chuyển đổi sang DTO để gửi qua SignalR
                        var notificationDto = objectMapper.Map<AppNotification, NotificationDto>(notification);

                        // Bắn SignalR realtime
                        await hubContext.Clients.User(assignment.UserId.ToString())
                            .SendAsync("ReceiveNotification", notificationDto);
                    }
                }
            }
        }
    }
}