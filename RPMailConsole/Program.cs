using System.ComponentModel.DataAnnotations;
using MailKitSimplified.Sender.Services;
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
    [Option(Template = "-h|--host", Description = "SMTP Host & Port")]
    public string Host { get;}
    
    [Required]
    [Option(Template = "-p|--pwd|--password", Description = "Password")]
    public string Password { get;}
    
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
    public bool Quiet { get; } = false;
    
    //Execution
    private async Task OnExecute()
    {
        //Parse Data
        Log($"Parsing Data File: {DataFile}", ConsoleColor.White);
        DataParser dataParser = new(DataFile);
        var receivers = dataParser.GetProperties(ReceiverHeader);
        var htmlContent = await File.ReadAllTextAsync(BodyPattern);
        Log($"Data File Parsed: {receivers.Count} Receivers Found", ConsoleColor.Green);
        
        //Build And Send
        for (int i = 0; i < receivers.Count; i++)
        {
            string receiver = receivers[i];
            Log($"Building Email To: {receiver}", ConsoleColor.White);
            
            Log($"- Parsing Email Subject and Body", ConsoleColor.White);
            string subject = dataParser.Parse(SubjectPattern,i);
            string body = dataParser.Parse(htmlContent,i);
            Log($"- Email Subject and Body Parsed", ConsoleColor.Green);
            
            //Create SmtpClient
            Log("- Creating SMTP Client", ConsoleColor.White);
            using var smtpClient = SmtpSender.Create(Host)
                .SetCredential(Sender, Password);
            Log("- SMTP Client Created", ConsoleColor.Green);

            var mail = smtpClient.WriteEmail
                .From(Sender)
                .To(receiver)
                .Subject(subject)
                .BodyHtml(body);
            //Add Attachments
            if (Attachments is not null)
            {
                foreach (string attachment in Attachments)
                {
                    mail.TryAttach(attachment);
                }
            }
            
            //Send
            Log("- Sending Email", ConsoleColor.Yellow);
            await mail.SendAsync();
            Log("- Email Sent", ConsoleColor.Green);
        }
        //Done
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("All Emails Sent");
        Console.ResetColor();
    }

    private void Log(string message, ConsoleColor color)
    {
        if (Quiet)  return;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
}