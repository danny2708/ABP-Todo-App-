using TaskManagement.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace TaskManagement.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class TaskManagementController : AbpControllerBase
{
    protected TaskManagementController()
    {
        LocalizationResource = typeof(TaskManagementResource);
    }
}
