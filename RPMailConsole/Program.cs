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
    
    [Required]
    [Option(Template = "-t|--title|--subject", Description = "Email Subject Pattern")]
    public string SubjectPattern { get;}
    
    [Required]
    [Option(Template = "-m|-b|--html|--body|--message", Description = "HTML Email Body Pattern File Path")]
    public string BodyPattern { get;}
    
    [Option(Template = "-a|--attachment", Description = "PDF Attachment Pattern File Path")]
    public string[]? AttachmentPatterns { get;} = null;
    
    [Option(Template = "-n|--attachment-name", Description = "Attachment Name Pattern File Path")]
    public string[]? AttachmentNamePattern { set; get;} = null;
    
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

    private string GetDefaultPattern(int index) => $"{DataParser.Format(ReceiverHeader)}_attachment_{index + 1}.pdf";
    
    private void InitArgs()
    {
        if (AttachmentPatterns is not null)
        {
            var newNamePatterns = new string[AttachmentPatterns.Length];
            int nameLength = 0;
            if (AttachmentNamePattern is not null)
            {
                for (int i = 0; i < AttachmentNamePattern.Length; i++)
                {
                    newNamePatterns[i] = AttachmentNamePattern[i];
                }
                nameLength = AttachmentNamePattern.Length;
            }
            for (int i = nameLength; i < AttachmentPatterns.Length; i++)
            {
                newNamePatterns[i] = GetDefaultPattern(i);
            }
            AttachmentNamePattern = newNamePatterns;
        }
    }
    
    private async Task OnExecute()
    {
        InitArgs();
        
        DateTime time = DateTime.Now;
        
        //Parse Data
        Log($"Parsing Data File: {DataFile}", ConsoleColor.White);
        DataParser dataParser = new(DataFile);
        var receivers = dataParser.GetProperties(ReceiverHeader);
        Log($"Data File Parsed: {receivers.Count} Receivers Found", ConsoleColor.Green);
        
        //Prebuild Output Directory
        string realOutputDir = Path.Combine(OutputFileDir,$"{time:yyyy-MM-dd_HH-mm-ss}");
        if (!Directory.Exists(realOutputDir))
        {
            Directory.CreateDirectory(realOutputDir);
            Log($"Output Directory Created: {realOutputDir}", ConsoleColor.White);
        }

        List<int> failList = [];
        
        //Build And Send
        for (int i = 0; i < receivers.Count; i++)
        {
            try
            {
                var htmlContent = await File.ReadAllTextAsync(dataParser.Parse(BodyPattern, i));

                string receiver = receivers[i];
                Log($"Building Email To: {receiver}", ConsoleColor.White);

                Log($"- Parsing Email Subject and Body", ConsoleColor.White);
                string subject = dataParser.Parse(SubjectPattern, i);
                string body = dataParser.Parse(htmlContent, i);
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
                List<string> outputs = [];
                if (AttachmentPatterns is not null)
                {
                    Log("- Building Attachments", ConsoleColor.White);
                    PDFParser pdfParser = new(dataParser)
                    {
                        SaveRawDoc = SaveRawDoc
                    };

                    for (int j = 0; j < AttachmentPatterns.Length; j++)
                    {
                        var attachment = AttachmentPatterns[j];

                        string patternPath = dataParser.Parse(attachment, i);
                        if (string.IsNullOrWhiteSpace(patternPath)) continue;

                        string targetFile = dataParser.Parse(AttachmentNamePattern[j], i);
                        if (string.IsNullOrWhiteSpace(targetFile)) continue;

                        string outputPath = Path.Combine(realOutputDir, targetFile);
                        if (Path.GetExtension(attachment) != ".pdf")
                        {
                            outputPath = Path.ChangeExtension(outputPath, ".pdf");
                        }

                        outputs.Add(outputPath);

                        if (Path.GetExtension(outputPath) == ".pdf")
                        {
                            //parse attachment
                            pdfParser.Parse(patternPath, outputPath, i);
                        }
                        else
                        {
                            //simply copy attachment
                            File.Copy(patternPath, outputPath, true);
                        }

                        //add attachment to mail
                        mail.Attach(outputPath);
                    }

                    Log("- Attachments Built", ConsoleColor.Green);
                }

                if (ConvertOnly) continue;

                //Send
                Log("- Sending Email", ConsoleColor.Yellow);
                await mail.SendAsync();
                Log("- Email Sent", ConsoleColor.Green);

                if (DeleteAfterConvert)
                {
                    foreach (var output in outputs) File.Delete(output);
                }
                else
                {
                    //rename outputs
                    string targetDir = Path.Combine(realOutputDir, receiver);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    for (int j = 0; j < outputs.Count; j++)
                    {
                        var output = outputs[j];
                        File.Move(output,
                            Path.Combine(targetDir, dataParser.Parse(AttachmentNamePattern[j], i)));
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                Console.ResetColor();
                //make it into fail list
                failList.Add(i);
            }
        }
        //Done
        if (failList.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("All Done");
            Console.ResetColor();
        }
        else
        {
            string dataToResend =
                Path.Combine(realOutputDir, Path.GetFileNameWithoutExtension(DataFile) + "_failed.csv");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Failed to send {failList.Count} emails");
            
            dataParser.HandleFailed(failList, dataToResend);
            
            Console.WriteLine($"Created failed data to {dataToResend}");
            Console.ResetColor();
        }
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