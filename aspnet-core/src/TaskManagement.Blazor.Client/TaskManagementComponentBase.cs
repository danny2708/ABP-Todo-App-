using TaskManagement.Localization;
using Volo.Abp.AspNetCore.Components;

namespace TaskManagement.Blazor.Client;

public abstract class TaskManagementComponentBase : AbpComponentBase
{
    protected TaskManagementComponentBase()
    {
        LocalizationResource = typeof(TaskManagementResource);
    }
}
