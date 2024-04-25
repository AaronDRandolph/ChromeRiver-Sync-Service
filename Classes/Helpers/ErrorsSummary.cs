using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace ChromeRiverService.Classes.Helpers
{
    public static class ErrorsSummary 
    {
        private static int NumNonCritialErrors = 0;
        private static int NumCritialErrors = 0;
        private static string BaseLogHunterQuery = $"<path_to_executable>.\\LogHunter.exe --app ChromeriverSyncService --start \"{GetCurrentCentralTime()}\"";


        public static void IncrementNumLowPriorityErrors() 
        {
            NumNonCritialErrors++;
        }

        public static void IncrementNumHighPriorityErrors() 
        {
            NumCritialErrors++;
        }

        public static async Task SendEmail(IConfiguration _configuration) 
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration.GetValue<string>("MAIL_FROM_ADDRESS")));
            email.To.Add(MailboxAddress.Parse(_configuration.GetValue<string>("MAIL_TO_ADDRESS")));
            email.Subject = "ChromeRiver Synch Service Error";
            email.Body = new TextPart(TextFormat.Html) 
            { 
                Text = new StringBuilder($"<h1>ChromeRiver Synch Service Error Report</h1>")
                        .Append(GetCriticalErrorsCommandForLogHunter())
                        .Append("<br><br>")
                        .Append(GetNonCriticalErrorsCommandForLogHunter())
                        .Append("<br><br>")
                        .Append(GetAllErrorsCommandForLogHunter())
                        .ToString() 
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration.GetValue<string>("MAIL_SERVER_HOST"), _configuration.GetValue<int>("MAIL_SERVER_PORT"), SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(new NetworkCredential(_configuration.GetValue<string>("MAIL_SERVER_USER_NAME"), _configuration.GetValue<string>("MAIL_SERVER_PASSWORD")));
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        private static string GetCurrentCentralTime() 
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")).ToString("dd-MM-yyyy hh:mm:ss tt"); 
        }

        private static string GetNonCriticalErrorsCommandForLogHunter()
        {
            return NumNonCritialErrors == 0 ? "" : $"<div>{NumNonCritialErrors} low priority error{(NumNonCritialErrors == 1 ? "" : "s")} can be found with the following Loghunter command line arguments: {BaseLogHunterQuery} --end \"{GetCurrentCentralTime()}\"  --level ERROR</div>";
        }

        private static string GetCriticalErrorsCommandForLogHunter()
        {
            return NumCritialErrors == 0 ? "" : $"<div>{NumCritialErrors} high priority error{(NumCritialErrors == 1 ? "" : "s")} can be found with the following Loghunter command line arguments: {BaseLogHunterQuery} --end \"{GetCurrentCentralTime()}\"  --level FATAL</div>";
        }

        private static string GetAllErrorsCommandForLogHunter()
        {
            return !ContainsErrors() ? "" : $"<div>Both critical and noncritical errors can be found with the following loghunter command line arguments: {BaseLogHunterQuery} --end \"{GetCurrentCentralTime()}\"  --level ERROR,FATAL</div>";
        }

        public static bool ContainsErrors()
        {
            return NumNonCritialErrors + NumCritialErrors > 0 ;
        }
    }
}