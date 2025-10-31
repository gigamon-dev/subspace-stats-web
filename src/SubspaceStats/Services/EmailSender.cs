using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using SubspaceStats.Areas.Identity.Data;
using SubspaceStats.Options;

namespace SubspaceStats.Services;

public class EmailSender(
    IOptions<AuthenticationEmailOptions> optionsAccessor,
    ILogger<EmailSender> logger) : IEmailSender<SubspaceStatsUser>
{
    private const string LogEmailSuccessTemplate = "Successfully sent sent email to {emailAddress}.";
    private const string LogEmailFailureTemplate = "Failed to send email to {emailAddress}. (Response code:{responseCode}, message:{responseMessage})";
    private const string LogEmailNotConfigured = "Failed to send email because email options are not configured.";

    private readonly AuthenticationEmailOptions _options = optionsAccessor.Value;
    private readonly ILogger _logger = logger;

    public Task SendConfirmationLinkAsync(SubspaceStatsUser user, string email, string confirmationLink)
    {
        return SendEmailAsync(
            email,
            "Confirm your email",
            $"""Please confirm your account by going to {confirmationLink}""",
            $"""<html lang="en"><head></head><body>Please confirm your account by <a href="{confirmationLink}">clicking here</a>.</body></html>""");
    }

    public Task SendPasswordResetLinkAsync(SubspaceStatsUser user, string email, string resetLink)
    {
        return SendEmailAsync(
            email, 
            "Reset your password",
            $"""Please reset your password by going to {resetLink}""",
            $"""<html lang="en"><head></head><body>Please reset your password by <a href="{resetLink}">clicking here</a>.</body></html>""");
    }

    public Task SendPasswordResetCodeAsync(SubspaceStatsUser user, string email, string resetCode)
    {
        return SendEmailAsync(
            email, 
            "Reset your password",
            $"""Please reset your password using the following code: {resetCode}""",
            $"""<html lang="en"><head></head><body>Please reset your password using the following code:<br>{resetCode}</body></html>""");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string plainMessage, string htmlMessage)
    {
        if (!string.IsNullOrEmpty(_options.SendGridKey))
        {
            await ExecuteSendGrid(_options.SendGridKey, toEmail, subject, plainMessage, htmlMessage);
        }
        else if (!string.IsNullOrEmpty(_options.SmtpHost) && !string.IsNullOrEmpty(_options.SmtpUsername) && !string.IsNullOrEmpty(_options.SmtpPassword))
        {
            await ExecuteSmtp(toEmail, subject, plainMessage, htmlMessage);
        }
        else
        {
            throw new Exception(LogEmailNotConfigured);
        }
    }

    private async Task ExecuteSendGrid(string apiKey, string toEmail, string subject, string plainMessage, string htmlMessage)
    {
        var client = new SendGridClient(apiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_options.FromEmail!, _options.FromName!),
            Subject = subject,
            PlainTextContent = plainMessage,
            HtmlContent = htmlMessage,
        };
        msg.AddTo(new EmailAddress(toEmail));

        // Disable click tracking.
        // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
        msg.SetClickTracking(false, false);

        // Send the email.
        var response = await client.SendEmailAsync(msg);

        // Check the repsonse.
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(LogEmailSuccessTemplate, toEmail);
        }
        else
        {
            _logger.LogError(
                LogEmailFailureTemplate, 
                toEmail, 
                response.StatusCode, 
                await response.Body.ReadAsStringAsync());
        }
    }

    private async Task ExecuteSmtp(string toEmail, string subject, string plainMessage, string htmlMessage)
    {
        using MimeMessage message = new();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = subject;

        // Set the body.
        BodyBuilder builder = new();
        builder.TextBody = plainMessage;
        builder.HtmlBody = htmlMessage;
        message.Body = builder.ToMessageBody();

        using SmtpClient client = new();

        try
        {
            // Connect
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.SmtpSSL);

            if (!string.IsNullOrEmpty(_options.SmtpUsername))
            {
                // Authenticate
                await client.AuthenticateAsync(_options.SmtpUsername, _options.SmtpPassword);
            }

            // Send
            await client.SendAsync(message);
        }
        catch (SmtpCommandException smtpCommandException)
        {
            _logger.LogError(
                LogEmailFailureTemplate,
                toEmail,
                smtpCommandException.StatusCode,
                smtpCommandException.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                LogEmailFailureTemplate,
                toEmail,
                "(none)",
                ex.Message);
        }

        _logger.LogInformation(LogEmailSuccessTemplate, toEmail);

        await client.DisconnectAsync(true);
    }
}