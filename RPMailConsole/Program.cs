using System.ComponentModel.DataAnnotations;
using MailKitSimplified.Sender.Services;
using McMaster.Extensions.CommandLineUtils;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable ReplaceAutoPropertyWithComputedProperty
#pragma warning disable CS8618

namespace RPMailConsole;

public class Program
{
    public static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);
    
    #region sender settings
    [Required]
    [Option(Template = "-s|--sender", Description = "Sender email address")]
    public string Sender { get;}
    
    [Required]
    [Option(Template = "-h|--host", Description = "SMTP Host & Port")]
    public string Host { get;}
    
    [Required]
    [Option(Template = "-p|--pwd|--password", Description = "Password")]
    public string Password { get;}
    
    //parse settings
    [Option(Template = "-r|--receiver-header", Description = "Receiver header in CSV file")]
    public string ReceiverHeader { get;} = "Receiver";
    
    #endregion
    
    #region content settings
    
    [Required]
    [Option(Template = "-d|--csv|--data", Description = "CSV Data File Path")]
    public string DataFile { get;}
    
    [Option(Template = "-a|--attachment", Description = "PDF Attachment Pattern File Path")]
    public string[]? AttachmentsPattern { get;} = null;
    
    [Required]
    [Option(Template = "-t|--title|--subject", Description = "Email Subject Pattern")]
    public string SubjectPattern { get;}
    
    
    [Required]
    [Option(Template = "-m|-b|--html|--body|--message", Description = "HTML Email Body Pattern File Path")]
    public string BodyPattern { get;}
    
    #endregion
    
    #region misc
    
    [Option(Template = "-o|--output", Description = "Attachment Convert File Directory")]
    public string OutputFileDir { get; } = "Output";
    
    [Option(Template = "--delete-after-convert", Description = "Delete Attachment Output after convert")]
    public bool DeleteAfterConvert { get; } = false;
    
    [Option]
    public bool Quiet { get; } = false;
    [Option(Template = "--convert-only", Description = "Only Convert Attachments and Exit")]
    public bool ConvertOnly { get; } = false;
    [Option(Template = "--save-raw-doc", Description = "Save Raw Document in Output Directory")]
    public bool SaveRawDoc { get; } = false;
    
    #endregion

    #region excution

    private async Task OnExecute()
    {
        DateTime time = DateTime.Now;
        
        //Parse Data
        Log($"Parsing Data File: {DataFile}", ConsoleColor.White);
        DataParser dataParser = new(DataFile);
        var receivers = dataParser.GetProperties(ReceiverHeader);
        Log($"Data File Parsed: {receivers.Count} Receivers Found", ConsoleColor.Green);
        
        //Prebuild Output Directory
        string realOutputDir = $"{OutputFileDir}/{time:yyyy-MM-dd_HH-mm-ss}";
        if (!Directory.Exists(realOutputDir))
        {
            Directory.CreateDirectory(realOutputDir);
            Log($"Output Directory Created: {realOutputDir}", ConsoleColor.White);
        }
        
        //Build And Send
        for (int i = 0; i < receivers.Count; i++)
        {
            var htmlContent = await File.ReadAllTextAsync(dataParser.Parse(BodyPattern,i));
            
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
            //Build Attachments
            if (AttachmentsPattern is not null)
            {
                Log("- Building Attachments", ConsoleColor.White);
                PDFParser pdfParser = new(dataParser)
                {
                    SaveRawDoc = SaveRawDoc
                };
                
                for(int j = 0; j < AttachmentsPattern.Length; j++)
                {
                    var attachment = AttachmentsPattern[j];
                    
                    string patternPath = dataParser.Parse(attachment,i);
                    string outputPath = $"{realOutputDir}/{receiver}_attachment_{j}.pdf";
                    
                    //parse attachment
                    pdfParser.Parse(patternPath,outputPath,i);
                    
                    //add attachment to mail
                    mail.Attach(outputPath);
                }
                Log("- Attachments Built", ConsoleColor.Green);
            }
            
            if(ConvertOnly) continue;
            
            //Send
            Log("- Sending Email", ConsoleColor.Yellow);
            await mail.SendAsync();
            Log("- Email Sent", ConsoleColor.Green);
            
            if(DeleteAfterConvert) Directory.Delete(realOutputDir,true);
        }
        //Done
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("All Done");
        Console.ResetColor();
    }

    private void Log(string message, ConsoleColor color)
    {
        if (Quiet)  return;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    
    #endregion
}