using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq; 
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks
{
    public class AppTask : FullAuditedAggregateRoot<Guid>
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string? DeletionReason { get; set; }
        
        public int Weight { get; set; }

        public virtual ICollection<TaskAssignment> Assignments { get; set; } 

        protected AppTask() 
        {
            Assignments = new Collection<TaskAssignment>();
        }

        public AppTask(Guid id, Guid projectId, string title, TaskStatus status, int weight) : base(id)
        {
            ProjectId = projectId;
            Title = title;
            Status = status; 
            Weight = weight; 
            IsApproved = true; 
            IsRejected = false;
            Assignments = new Collection<TaskAssignment>();
        }

        public void AddAssignment(Guid userId)
        {
            // Kiểm tra tránh thêm trùng lặp cùng một user vào một task
            if (Assignments.Any(a => a.UserId == userId))
            {
                return;
            }
            Assignments.Add(new TaskAssignment(Id, userId));
        }

        public void RemoveAssignment(Guid userId)
        {
            var assignment = Assignments.FirstOrDefault(a => a.UserId == userId);
            if (assignment != null)
            {
                Assignments.Remove(assignment);
            }
        }

        public void ClearAssignments()
        {
            Assignments.Clear();
        }
    }

    public class TaskAssignment
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        
        protected TaskAssignment() { }

        public TaskAssignment(Guid taskId, Guid userId)
        {
            TaskId = taskId;
            UserId = userId;
        }
    }
}