using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.SignalR;

namespace TaskManagement.Hubs
{
    [Authorize]
    public class NotificationHub : AbpHub
    {
        
    }
}