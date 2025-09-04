using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using McMaster.Extensions.CommandLineUtils;

namespace RPMailConsole;

public class Program
{
    public static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);
    
    //basic settings
    [Required]
    [Option(Template = "-s|--sender", Description = "Sender email address")]
    public string Sender { get;}
    
    [Required]
    [Option(Template = "-d|--csv|--data", Description = "CSV Data File Path")]
    public string DataFile { get;}
    
    [Option(Template = "-a|--attachment", Description = "Attachment File Path")]
    public string[]?  Attachments { get;} = null;
    
    //smtp settings
    [Required]
    [Option(Template = "-h|--host", Description = "SMTP Host")]
    public string Host { get;}
    
    [Required]
    [Option(Template = "-p|--pwd|--password", Description = "Password")]
    public string Password { get;}
    
    [Option(Template = "--port", Description = "SMTP Port")]
    public int Port { get;} = 587;
    [Option(Template = "--enable-ssl", Description = "Enable SSL")]
    public bool EnableSsl { get;} = true;
    
    //parse settings
    [Option(Template = "-r|--receiver-header", Description = "Receiver header in CSV file")]
    public string ReceiverHeader { get;} = "Receiver";
    
    
    [Required]
    [Option(Template = "-t|--title|--subject", Description = "Email Subject Pattern")]
    public string SubjectPattern { get;}
    
    
    [Required]
    [Option(Template = "-m|-b|--html|--body|--message", Description = "HTML Email Body Pattern File Path")]
    public string BodyPattern { get;}
    
    //misc
    [Option]
    public bool Verbose { get; } = false;
    
    //Execution
    private void OnExecute(CommandLineApplication app, CancellationToken cancellationToken =  default)
    {
        //Create SmtpClient
        Log($"Creating SMTP Client: {Host}:{Port}", ConsoleColor.White);
        SmtpClient smtpClient = new(Host,Port);
        smtpClient.EnableSsl = EnableSsl;
        smtpClient.UseDefaultCredentials = false;
        smtpClient.Credentials = new NetworkCredential(Sender, Password);
        Log($"SMTP Client Created: {smtpClient.Host}:{smtpClient.Port}", ConsoleColor.Green);
        
        //Parse Data
        Log($"Parsing Data File: {DataFile}", ConsoleColor.White);
        DataParser dataParser = new(DataFile);
        var receivers = dataParser.GetProperties(ReceiverHeader);
        Log($"Data File Parsed: {receivers.Count} Receivers Found", ConsoleColor.Green);
        
        //Build And Send
        for (int i = 0; i < receivers.Count; i++)
        {
            string receiver = receivers[i];
            Log($"Building Email To: {receiver}", ConsoleColor.White);
            
            Log($"- Parsing Email Subject and Body", ConsoleColor.White);
            string subject = dataParser.Parse(SubjectPattern,i);
            string body = dataParser.Parse(BodyPattern,i);
            Log($"- Email Subject and Body Parsed", ConsoleColor.Green);
            
            //Create MailMessage
            MailMessage mailMessage = new();
            mailMessage.From = new MailAddress(Sender);
            mailMessage.To.Add(receiver);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;
            
            //Add Attachments
            if (Attachments is not null)
            {
                foreach (string attachment in Attachments)
                {
                    mailMessage.Attachments.Add(new (attachment));
                }
            }
            
            //Send
            Log("- Sending Email", ConsoleColor.Yellow);
            smtpClient.Send(mailMessage);
            Log("- Email Sent", ConsoleColor.Green);
        }
        Log("All Emails Sent", ConsoleColor.Green);
    }

    private void Log(string message, ConsoleColor color)
    {
        if (!Verbose)  return;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
}