namespace TaskManagement.Permissions;

public static class TaskManagementPermissions
{
    public const string GroupName = "TaskManagement";

    // Thêm đoạn này để định nghĩa các hằng số quyền
    public static class Tasks
    {
        public const string Default = GroupName + ".Tasks";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string UpdateStatus = Default + ".UpdateStatus";
    }
}