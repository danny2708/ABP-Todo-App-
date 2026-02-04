using TaskManagement.Localization;
using Volo.Abp.AspNetCore.Components;

namespace TaskManagement.Blazor.WebApp.Tiered;

public abstract class TaskManagementComponentBase : AbpComponentBase
{
    protected TaskManagementComponentBase()
    {
        LocalizationResource = typeof(TaskManagementResource);
    }
}
