using TimeTrackingsApp.Models.Entities;

namespace TimeTrackingApp.Services
{
    public class NotificationEmailService : INotificationEmailService
    {
        private readonly IEmailService _emailService;

        public NotificationEmailService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendWelcomeEmail(User user, string password)
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Witamy w systemie ewidencji czasu pracy",
                $"""
            <p>Witaj {user.FirstName},</p>
            <p>Twoje dane logowania:</p>
            <ul>
                <li>Login: <b>{user.Email}</b></li>
                <li>Hasło: <b>{password}</b></li>
            </ul
            """
            );
        }

        public async Task SendPasswordResetEmail(User user, string resetLink)
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Reset hasła",
                $"""
            <p>Aby zresetować hasło kliknij w link:</p>
            <p><a href="{resetLink}">Resetuj hasło</a></p>
            """
            );
        }

        public async Task SendLeaveRequestSubmittedEmail(User user)
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Wniosek urlopowy złożony",
                "<p>Twój wniosek urlopowy został poprawnie złożony i oczekuje na decyzję kierownika.</p>"
            );
        }

        public async Task SendLeaveDecisionEmail(User user, LeaveRequest request)
        {
            var decision = request.status == "Approved" ? "ZATWIERDZONY" : "ODRZUCONY";

            await _emailService.SendEmailAsync(
                user.Email,
                $"Wniosek urlopowy – {decision}",
                $"""
            <p>Twój wniosek urlopowy:</p>
            <p>{request.startdate:dd.MM.yyyy} – {request.enddate:dd.MM.yyyy}</p>
            <p>Status: <b>{decision}</b></p>
            """
            );
        }

        public async Task SendMissingTimeEntriesReminder(User user)
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Brakujące wpisy czasu pracy",
                """
            <p>Przypominamy o uzupełnieniu brakujących wpisów czasu pracy.</p>
            <p>Zaloguj się do systemu i uzupełnij ewidencję.</p>
            """
            );
        }
    }

}
