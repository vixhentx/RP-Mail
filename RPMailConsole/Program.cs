using System.ComponentModel.DataAnnotations;
using System.Text;
using MailKitSimplified.Sender.Abstractions;
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
    
    [Option(Template = "-c|--charset", Description = "All Files Encoding if BOM absent")]
    public string CharSet { get; } = "utf-8";
    
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

    private Encoding _encoding;
    
    private void InitArgs()
    {
        //Logger
        if (Quiet) Log = (s, c) => { };
        
        //GB2312
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
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

        if(string.IsNullOrWhiteSpace(CharSet))
            _encoding = Encoding.Default;
        else
        {
            try
            {
                _encoding = Encoding.GetEncoding(CharSet);
            }
            catch (ArgumentException)
            {
                Log($"Invalid CharSet: {CharSet}, using default encoding", ConsoleColor.Yellow);
                _encoding = Encoding.Default;
            }
        }
    }
    
    private async Task OnExecute()
    {
        InitArgs();
        
        DateTime time = DateTime.Now;
        
        //Parse Data
        Log($"Parsing Data File: {DataFile}", ConsoleColor.White);
        DataParser dataParser;
        List<string> receivers;
        try
        {
            dataParser = new(DataFile,_encoding);
            receivers = dataParser.GetProperties(ReceiverHeader);
            Log($"Data File Parsed: {receivers.Count} Receivers Found", ConsoleColor.Green);
        }
        catch (Exception e)
        {
            Info("Failed to parse data file and get receivers", ConsoleColor.DarkRed);
            Info(e.Message, ConsoleColor.DarkRed);
            return;
        }
        
        //Prebuild Output Directory
        string realOutputDir = Path.Combine(OutputFileDir,$"{time:yyyy-MM-dd_HH-mm-ss}");
        if (!Directory.Exists(realOutputDir))
        {
            Directory.CreateDirectory(realOutputDir);
            Log($"Output Directory Created: {realOutputDir}", ConsoleColor.White);
        }
        
        Log("- Creating SMTP Client", ConsoleColor.White);
        await using var smtpClient = SmtpSender.Create(Host)
            .SetCredential(Sender, Password);
        Log("- SMTP Client Created", ConsoleColor.Green);

        List<int> failList = [];
        List<string> failReasons = [];
        
        //Build And Send
        for (int i = 0; i < receivers.Count; i++)
        {
            List<string> outputs = [];
            string receiver = receivers[i];
            string htmlPath = string.Empty;
            string htmlBody = string.Empty;
            Log($"Building Email {i+1}/{receivers.Count} To: {receiver}", ConsoleColor.White);
            try
            {
                #region Build Email Basics
                htmlPath = dataParser.Parse(BodyPattern, i);
                var htmlContent = await File.ReadAllTextAsync(htmlPath, _encoding);

                Log($"- Parsing Email Subject and Body", ConsoleColor.White);
                string subject = dataParser.Parse(SubjectPattern, i);
                htmlBody = dataParser.Parse(htmlContent, i);
                Log($"- Email Subject and Body Parsed", ConsoleColor.Green);

                IEmailWriter? mail = null;
                if (!ConvertOnly)
                {
                    mail = smtpClient.WriteEmail
                        .From(Sender)
                        .To(receiver)
                        .Subject(subject)
                        .BodyHtml(htmlBody);

                }
                #endregion

                #region Build Attachments
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

                        if (string.IsNullOrWhiteSpace(Path.GetExtension(targetFile)))
                        {
                            targetFile = Path.ChangeExtension(targetFile, ".pdf");
                        }

                        string outputPath = Path.Combine(realOutputDir, targetFile);

                        outputs.Add(outputPath);

                        if (Path.GetExtension(targetFile) == ".pdf")
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
                        if (!ConvertOnly)
                            mail.Attach(outputPath);
                    }

                    Log("- Attachments Built", ConsoleColor.Green);
                }
                #endregion

                #region Send Email
                if (!ConvertOnly)
                {
                    Log("- Sending Email", ConsoleColor.Yellow);
                    await mail.SendAsync();
                    Log("- Email Sent", ConsoleColor.Green);
                }
                #endregion
            }
            catch (Exception e)
            {
                Info(e.Message, ConsoleColor.Red);
                //make it into fail list
                failList.Add(i);
                failReasons.Add(e.Message);
            }
            finally
            {
                #region Move or Delete
                if (DeleteAfterConvert)
                {
                    try
                    {
                        foreach (var output in outputs)
                        {
                            if(File.Exists(output)) File.Delete(output);
                        }
                    }
                    catch (Exception e)
                    {
                        Info("Failed to delete output files", ConsoleColor.Yellow);
                        Info(e.Message, ConsoleColor.Yellow);
                    }
                }
                else
                {
                    try
                    {
                        //rename outputs
                        string targetDir = Path.Combine(realOutputDir, receiver);
                        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                        for (int j = 0; j < outputs.Count; j++)
                        {
                            var output = outputs[j];
                            if (File.Exists(output))
                                File.Move(output,
                                    Path.Combine(targetDir, Path.GetFileName(output)), true);
                        }

                        if (ConvertOnly)
                        {
                            //save html
                            string htmlFileOutput = Path.Combine(targetDir, htmlPath);
                            if (File.Exists(htmlFileOutput)) File.Delete(htmlFileOutput);
                            File.WriteAllText(htmlFileOutput, htmlBody, _encoding);
                        }
                    }
                    catch (Exception e)
                    {
                        Info("Failed to move output files to receiver directory", ConsoleColor.Yellow);
                        Info(e.Message, ConsoleColor.Yellow);
                    }
                }
                #endregion
            }
        }
        //Done
        if (failList.Count == 0)
        {
            Log("All Done",ConsoleColor.Green);
        }
        else
        {
            #region Handle Failed
            string dataToResend =
                Path.Combine(realOutputDir, Path.GetFileNameWithoutExtension(DataFile) + "_failed.csv");
            Info($"Failed to send {failList.Count} / {receivers.Count} emails:",ConsoleColor.White);

            int n = failList.Count;
            for (int i = 0; i < n; i++)
            {
                int index = failList[i];
                string reason = failReasons[i];
                var row = dataParser.GetRow(index);
                
                Info($"Failed to send to {receivers[index]} : {reason}", ConsoleColor.Yellow);
                Info("--- Data --- ", ConsoleColor.White);
                foreach (var item in row)
                {
                    Info($"{item.Key} : {item.Value}", ConsoleColor.White);
                }
                Info("",ConsoleColor.Gray);
            }
            
            dataParser.HandleFailed(failList, dataToResend);
            Info($"Created failed data to {dataToResend}",ConsoleColor.Yellow);
            #endregion
        }
    }

    private Action<string, ConsoleColor> Log = Info;

    private static void Info(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    
    #endregion
}