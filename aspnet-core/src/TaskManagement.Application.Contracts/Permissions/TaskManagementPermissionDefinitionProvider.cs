using TaskManagement.Localization;
using TaskManagement.Permissions; 
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace TaskManagement.Permissions;

public class TaskManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        // 1. Tạo Group (Nếu chưa có)
        var myGroup = context.AddGroup(TaskManagementPermissions.GroupName, L("Permission:TaskManagement"));

        // 2. Đăng ký quyền 'TaskManagement.Tasks' (Quyền cha)
        var tasksPermission = myGroup.AddPermission(
            TaskManagementPermissions.Tasks.Default, 
            L("Permission:Tasks"));

        // 3. Đăng ký các quyền con (Create, Edit, Delete)
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Create, L("Permission:Tasks.Create"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Update, L("Permission:Tasks.Update"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Delete, L("Permission:Tasks.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TaskManagementResource>(name);
    }
}