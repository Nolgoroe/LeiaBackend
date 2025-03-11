using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;

namespace Services.Emailer
{

	public interface IEmailService
	{
		void SendEmail(string to, string subject, string body);
	}

	public class EmailService : IEmailService
	{
		private readonly SmtpClient _client;
		private readonly string _from;

		public EmailService(string fromAddress, string smtpServer, string username, string password)
		{
			_client = new SmtpClient(smtpServer)
			{
				Port = 587,
				Credentials = new NetworkCredential(username, password),
				EnableSsl = true,
			};
			_from = fromAddress;
		}

		public void SendEmail(string to, string subject, string body)
		{
			MailMessage message = new MailMessage
			{
				From = new MailAddress(_from),
				Subject = subject,
				SubjectEncoding = System.Text.Encoding.UTF8,
				Body = body,
				BodyEncoding = System.Text.Encoding.UTF8,
				IsBodyHtml = true,
			};
			message.To.Add(to);
			_client.Send(message);
		}
	}
}