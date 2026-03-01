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
using Volo.Abp.Uow; // THÊM NAMESPACE NÀY

namespace TaskManagement.Workers
{
    public class OverdueTaskWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public OverdueTaskWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory
        ) : base(timer, serviceScopeFactory)
        {
            Timer.Period = 1000; // 10 phút quét 1 lần
        }

        // THÊM ATTRIBUTE NÀY ĐỂ GIỮ KẾT NỐI DATABASE LUÔN MỞ
        [UnitOfWork]
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var taskRepository = workerContext.ServiceProvider.GetRequiredService<ITaskRepository>();
            var notificationRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<AppNotification, Guid>>();
            var hubContext = workerContext.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
            var objectMapper = workerContext.ServiceProvider.GetRequiredService<IObjectMapper>();

            // 1. Tìm các Task đã quá hạn và chưa hoàn thành
            // Sử dụng đường dẫn tuyệt đối cho TaskStatus để tránh xung đột namespace
            var overdueTasks = await taskRepository.GetListAsync(t => 
                t.DueDate < DateTime.Now && 
                t.Status != TaskManagement.Tasks.TaskStatus.Completed && 
                t.IsApproved == true
            );

            foreach (var task in overdueTasks)
            {
                // 2. Kiểm tra tránh gửi trùng thông báo
                var alreadyNotified = await notificationRepository.AnyAsync(n => 
                    n.RelatedTaskId == task.Id && 
                    n.Type == "TaskOverdue"
                );

                if (!alreadyNotified)
                {
                    // 3. Gửi thông báo cho từng nhân viên được gán
                    foreach (var assignment in task.Assignments)
                    {
                        var notification = new AppNotification(
                            Guid.NewGuid(),
                            assignment.UserId,
                            "Cảnh báo quá hạn",
                            $"Công việc '{task.Title}' đã trôi qua hạn chót!",
                            "TaskOverdue",
                            task.Id
                        );

                        await notificationRepository.InsertAsync(notification);

                        var notificationDto = objectMapper.Map<AppNotification, NotificationDto>(notification);

                        // Gửi realtime qua SignalR
                        await hubContext.Clients.User(assignment.UserId.ToString())
                            .SendAsync("ReceiveNotification", notificationDto);
                    }
                }
            }
        }
    }
}