using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace EventPlatform.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string html
    )
    {
        try
        {
            var email = new MimeMessage();

            email.From.Add(
                MailboxAddress.Parse(
                    _config["EmailSettings:FromEmail"]
                )
            );

            email.To.Add(
                MailboxAddress.Parse(to)
            );

            email.Subject = subject;

            email.Body = new TextPart(TextFormat.Html)
            {
                Text = html
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpHost"],
                int.Parse(
                    _config["EmailSettings:SmtpPort"]
                ),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SmtpUser"],
                _config["EmailSettings:SmtpPassword"]
            );

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "EMAIL ERROR: " + ex.Message
            );
        }
    }
}