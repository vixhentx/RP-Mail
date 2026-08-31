using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RPMailCore.Models;

namespace RPMailCore.Processors;

public class MailSendProcessor(string host, string senderEmail, string smtpPassword)
{
    public virtual async Task SendAsync(ContentParsed content, CancellationToken ct = default)
    {
        var (smtpHost, port) = ParseHost(host);
        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, port, SecureSocketOptions.Auto, ct);
        await client.AuthenticateAsync(senderEmail, smtpPassword, ct);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(senderEmail));
        message.To.Add(MailboxAddress.Parse(content.Receiver));
        message.Subject = content.Subject;

        var body = new BodyBuilder { HtmlBody = content.HtmlBody };
        foreach (var attachment in content.Attachments)
            body.Attachments.Add(attachment);

        message.Body = body.ToMessageBody();
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    private static (string host, int port) ParseHost(string host)
    {
        int idx = host.LastIndexOf(':');
        if (idx >= 0 && int.TryParse(host.AsSpan(idx + 1), out int port))
            return (host[..idx], port);
        return (host, 587);
    }
}
