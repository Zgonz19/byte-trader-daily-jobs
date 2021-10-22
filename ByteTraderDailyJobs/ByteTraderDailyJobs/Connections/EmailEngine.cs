using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog.Web;
using NLog;

namespace ByteTraderDailyJobs.Connections
{
    public class EmailEngine
    {
        public ByteTraderRepository Repo = new ByteTraderRepository();

        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        private EmailConfig EmailConfig { get; set; }
        public EmailEngine()
        {
            InitializeClass();
        }
        public async void InitializeClass()
        {
            var attribute = await Repo.GetSystemDefault("ByteTrader Email");
            var keys = JsonConvert.DeserializeObject<EmailConfig>(attribute.AttributeValue);
            EmailConfig = keys;
        }


        public async void SendTradeAlert(List<string> emails)
        {
            var body = "";
            var subject = "";
            await SendEmailBatch(emails, body, subject);
        }

        public async Task EmailBatchTemplate(List<string> emails, string subject, string body, bool isBodyHtml)
        {
            var tasks = new List<Task>();
            foreach (var email in emails)
            {
                tasks.Add(SendTemplate(email, subject, body, isBodyHtml));
            }
            await Task.WhenAll(tasks);
        }


        public async Task SendTemplate(string email, string subject, string body, bool isBodyHtml)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(EmailConfig.SmtpServer);

                mail.From = new MailAddress(EmailConfig.EmailAddress);
                mail.To.Add(email);
                mail.IsBodyHtml = isBodyHtml;
                mail.Subject = subject;
                mail.Body = body;

                SmtpServer.Port = EmailConfig.Port;
                SmtpServer.Credentials = new System.Net.NetworkCredential(EmailConfig.Username, EmailConfig.Password);
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task SendEmailBatch(List<string> emails, string body, string subject)
        {
            var tasks = new List<Task>();
            foreach (var email in emails)
            {
                tasks.Add(SendEmail(email, body, subject));
            }
            await Task.WhenAll(tasks);
        }

        public async Task SendEmailOld(List<string> emails, string body, string subject)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(EmailConfig.SmtpServer);

                mail.From = new MailAddress(EmailConfig.EmailAddress);
                foreach (var email in emails)
                {
                    mail.To.Add(email);
                }
                mail.Subject = subject;
                mail.Body = body;

                SmtpServer.Port = EmailConfig.Port;
                SmtpServer.Credentials = new System.Net.NetworkCredential(EmailConfig.Username, EmailConfig.Password);
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }

        }
        public async Task SendEmail(string email, string body, string subject)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(EmailConfig.SmtpServer);

                mail.From = new MailAddress(EmailConfig.EmailAddress);
                mail.To.Add(email);

                mail.Subject = subject;
                mail.Body = body;

                SmtpServer.Port = EmailConfig.Port;
                SmtpServer.Credentials = new System.Net.NetworkCredential(EmailConfig.Username, EmailConfig.Password);
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }

        }
    }
    public class EmailConfig
    {
        public string EmailAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string SmtpServer { get; set; }
    }
}
