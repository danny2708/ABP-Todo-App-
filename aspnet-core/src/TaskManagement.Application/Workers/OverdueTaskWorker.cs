using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Hubs;
using TaskManagement.Notifications;
using TaskManagement.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using Volo.Abp.Timing;
using Volo.Abp.Linq; // Cần thiết để dùng IAsyncQueryableExecuter

namespace TaskManagement.Workers
{
    public class OverdueTaskWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public OverdueTaskWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory) 
            : base(timer, serviceScopeFactory)
        {
            // Thiết lập 100 giây quét 1 lần để test
            Timer.Period = 100000; 
        }

        [UnitOfWork]
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var logger = workerContext.ServiceProvider.GetRequiredService<ILogger<OverdueTaskWorker>>();
            var taskRepository = workerContext.ServiceProvider.GetRequiredService<ITaskRepository>();
            var notificationRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<AppNotification, Guid>>();
            var hubContext = workerContext.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
            var objectMapper = workerContext.ServiceProvider.GetRequiredService<IObjectMapper>();
            var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
            
            // GIẢI PHÁP: Lấy AsyncExecuter từ ServiceProvider
            var asyncExecuter = workerContext.ServiceProvider.GetRequiredService<IAsyncQueryableExecuter>();

            logger.LogInformation("--- Đang kiểm tra task quá hạn: " + clock.Now + " ---");

            // Nạp dữ liệu kèm Assignments để tránh lỗi null
            var queryable = await taskRepository.WithDetailsAsync(t => t.Assignments);
            
            // Sửa lỗi: Sử dụng asyncExecuter đã resolve ở trên
            var overdueTasks = await asyncExecuter.ToListAsync(
                queryable.Where(t => 
                    t.DueDate < clock.Now && 
                    t.Status != TaskManagement.Tasks.TaskStatus.Completed && 
                    t.IsApproved == true
                )
            );

            if (overdueTasks.Any())
            {
                logger.LogInformation($"Tìm thấy {overdueTasks.Count} công việc quá hạn!");
            }

            foreach (var task in overdueTasks)
            {
                var alreadyNotified = await notificationRepository.AnyAsync(n => 
                    n.RelatedTaskId == task.Id && 
                    n.Type == "TaskOverdue"
                );

                if (!alreadyNotified)
                {
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
                        
                        await hubContext.Clients.User(assignment.UserId.ToString())
                            .SendAsync("ReceiveNotification", notificationDto);
                            
                        logger.LogInformation($"Gửi cảnh báo tới User ID: {assignment.UserId}");
                    }
                }
            }
        }
    }
}