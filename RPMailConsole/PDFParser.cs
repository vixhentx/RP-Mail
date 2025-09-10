
using System.Text;
using Spire.Doc;

namespace RPMailConsole;

public class PDFParser(DataParser dataParser)
{
    public bool SaveRawDoc { get; set; } = false;

    public void Parse(string patternPath, string outputPath, int index)
    {
        string ext = Path.GetExtension(patternPath).ToLower();
        switch (ext)
        {
            case ".pdf":
                ParseFromPDF(patternPath, outputPath, index);
                break;
            default:
                ParseAutoMatically(patternPath, outputPath, index);
                break;
        };
    }

    private void ParseFromPDF(string patternPath, string outputPath, int index)
    {
        throw new NotImplementedException("ParseFromPDF is not implemented yet.");
    }

    private void ParseAutoMatically(string patternPath, string outputPath, int index)
    {
        using Document doc = new Document();
        doc.LoadFromFile(patternPath, FileFormat.Auto);

        dataParser.AbstractParse(doc.GetText(), index, (f, r) =>
            doc.Replace(f, r, true, true));

        doc.SaveToFile(outputPath, FileFormat.PDF);
        if (SaveRawDoc)
        {
            string ext = Path.GetExtension(patternPath);
            doc.SaveToFile(outputPath + $".{ext}", doc.DetectedFormatType);
        }
    }

}