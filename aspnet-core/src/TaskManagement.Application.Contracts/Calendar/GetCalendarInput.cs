// TaskManagement.Application.Contracts/Calendar/GetCalendarInput.cs
using System;

namespace TaskManagement.Tasks;

public class GetCalendarInput
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}