using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RPMailCore.Models;

namespace RPMailCore.Processors;

public class MailSendProcessor(SenderConfig conf)
{
    public async Task SendAsync(ContentParsed content, CancellationToken ct = default)
    {
        var (smtpHost, port) = ParseHost(conf.SmtpHost);
        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, port, SecureSocketOptions.Auto, ct);
        await client.AuthenticateAsync(conf.SenderEmail, conf.SenderPassword, ct);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(conf.SenderEmail));
        message.To.Add(MailboxAddress.Parse(content.Email));
        message.Subject = content.Subject;

        var body = new BodyBuilder { HtmlBody = content.BodyHtml };
        foreach (var attachment in content.Attachments)
            body.Attachments.Add(attachment,ct);

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
