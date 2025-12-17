using TimeTrackingsApp.Models.Entities;

namespace TimeTrackingApp.Services
{
    public interface INotificationEmailService
    {
        Task SendWelcomeEmail(User user, string password);
        Task SendPasswordResetEmail(User user, string resetLink);
        Task SendLeaveRequestSubmittedEmail(User user);
        Task SendLeaveDecisionEmail(User user, LeaveRequest request);
        Task SendMissingTimeEntriesReminder(User user);
    }

}
