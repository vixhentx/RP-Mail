using Spire.Doc;

namespace RPMailCore;

public class DocConverter(DataParser dataParser,bool saveRawDoc)
{
    public void Parse(string patternPath, string outputPath, Dictionary<string, string> row)
    {
        string ext = Path.GetExtension(patternPath).ToLower();
        switch (ext)
        {
            case ".pdf":
                ParseFromPDF(patternPath, outputPath, row);
                break;
            default:
                ParseAutomatically(patternPath, outputPath, row);
                break;
        };
    }

    private void ParseFromPDF(string patternPath, string outputPath, Dictionary<string, string> row)
    {
        throw new NotImplementedException("ParseFromPDF is not implemented yet.");
    }

    private void ParseAutomatically(string patternPath, string outputPath, Dictionary<string, string> row)
    {
        Document doc = new ();
        doc.LoadFromFile(patternPath, FileFormat.Auto);

        dataParser.AbstractParse(doc.GetText(), row, (f, r) =>
            doc.Replace(f, r, true, true));

        doc.SaveToFile(outputPath, FileFormat.PDF);
        if (saveRawDoc)
        {
            string ext = Path.GetExtension(patternPath);
            doc.SaveToFile(outputPath + $".{ext}", doc.DetectedFormatType);
        }
    }

}