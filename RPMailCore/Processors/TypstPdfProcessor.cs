using RPMailCore.Services;
using Typst.Native;

namespace RPMailCore.Processors;

public sealed class TypstPdfProcessor : IDisposable
{
    private readonly TypstCompiler _compiler = new();

    public void Convert(string renderedTyp, string templateDir, string outputPdfPath)
    {
        _compiler.SetRoot(templateDir);
        using var result = _compiler.Compile(renderedTyp);
        if (!result.IsSuccess)
            throw new InvalidOperationException(string.Join("; ", result.Diagnostics));
        FileIo.WriteAllBytes(outputPdfPath, result.ToPdf());
    }

    public void Dispose() => _compiler.Dispose();
}
