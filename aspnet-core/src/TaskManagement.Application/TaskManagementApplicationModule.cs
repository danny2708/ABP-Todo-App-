using Volo.Abp.Account;
using Volo.Abp.AutoMapper; 
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Microsoft.AspNetCore.SignalR;
using TaskManagement.Hubs;
using Volo.Abp.AspNetCore.SignalR;
// THÊM CÁC USING CẦN THIẾT CHO WORKER
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using TaskManagement.Workers;

namespace TaskManagement;

[DependsOn(
    typeof(TaskManagementDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(TaskManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSignalRModule) 
    )]
public class TaskManagementApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<TaskManagementApplicationModule>();
        });
    }

    // ĐĂNG KÝ WORKER KHI KHỞI CHẠY ỨNG DỤNG
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // Kích hoạt tiến trình quét task quá hạn tự động
        context.AddBackgroundWorkerAsync<OverdueTaskWorker>();
    }
}