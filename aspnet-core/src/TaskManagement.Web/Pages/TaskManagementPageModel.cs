using TaskManagement.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace TaskManagement.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class TaskManagementPageModel : AbpPageModel
{
    protected TaskManagementPageModel()
    {
        LocalizationResourceType = typeof(TaskManagementResource);
    }
}
