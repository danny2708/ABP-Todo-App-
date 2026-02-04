using AutoMapper; 
using TaskManagement.Tasks;

namespace TaskManagement;

public class TaskManagementApplicationAutoMapperProfile : Profile
{
    public TaskManagementApplicationAutoMapperProfile()
    {
        /* 1. Ánh xạ từ Entity sang DTO (Dùng khi lấy dữ liệu ra) */
        CreateMap<AppTask, TaskDto>();
        
        /* 2. Ánh xạ từ DTO sang Entity (Dùng khi tạo mới hoặc cập nhật) */
        CreateMap<CreateUpdateTaskDto, AppTask>();
    }
}