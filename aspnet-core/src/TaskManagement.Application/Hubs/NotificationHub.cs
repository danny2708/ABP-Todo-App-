using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.SignalR;

namespace TaskManagement.Hubs
{
    // ABP sẽ tự động map route này mà không cần cấu hình ở Host Module
    [HubRoute("/signalr-hubs/notification")]
    [Authorize]
    public class NotificationHub : AbpHub
    {
    }
}