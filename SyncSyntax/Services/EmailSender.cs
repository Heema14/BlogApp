using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

public class EmailSender
{
    private readonly EmailSettings _emailSettings;

 
    public EmailSender(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

  
    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            var smtpClient = new SmtpClient(_emailSettings.MailServer)
            {
                Port = _emailSettings.MailPort,
                Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

      
            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }
}
