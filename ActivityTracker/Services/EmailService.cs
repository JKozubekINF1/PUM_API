using System.Net;
using System.Net.Mail;

namespace ActivityTracker.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpHost = _config["EmailSettings:Host"];
        var smtpPort = int.Parse(_config["EmailSettings:Port"]!);
        var smtpUser = _config["EmailSettings:User"]; // Twój login do poczty na hostingu
        var smtpPass = _config["EmailSettings:Password"]; // Twoje hasło do poczty
        var smtpFrom = _config["EmailSettings:From"]; // Np. no-reply@twojadomena.pl

        var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true // Hostingi zazwyczaj wymagają SSL
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpFrom!, "Activity Tracker"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}