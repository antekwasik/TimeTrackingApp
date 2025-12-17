using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeTrackingsApp.Models.Entities;


public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }

    public DbSet<EmailLog> Emaillogs { get; set; }

}