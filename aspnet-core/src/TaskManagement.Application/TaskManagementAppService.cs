using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Localization;
using Volo.Abp.Application.Services;

namespace TaskManagement;

/* Inherit your application services from this class.
 */
public abstract class TaskManagementAppService : ApplicationService
{
    protected TaskManagementAppService()
    {
        LocalizationResource = typeof(TaskManagementResource);
    }
    
}
