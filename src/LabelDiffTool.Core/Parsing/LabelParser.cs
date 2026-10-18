using D365KnowledgeBase.Labels;
using LabelDiffTool.Core.Models;

namespace LabelDiffTool.Core.Parsing;

/// <summary>
/// Reads D365 F&amp;O label files. The format itself — <c>LabelId=Label text</c>, then an optional
/// <c> ;Description</c> line — is read by <see cref="LabelResourceFile"/> (D365KnowledgeBase.Labels), as
/// tolerantly as Visual Studio: with or without a BOM, a description starting with <c>;</c> or <c>#</c>, legacy
/// <c>@SYS123=</c> lines. Lines that hold no label are not kept, as in Visual Studio.
/// </summary>
public static class LabelParser
{
    public static LabelFile ParseFile(string path)
    {
        var file = FromResource(Path.GetFileName(path), LanguageOf(path), LabelResourceFile.Read(path));
        file.FilePath = path;
        return file;
    }

    public static LabelFile Parse(string fileId, string language, IEnumerable<string> lines) =>
        FromResource(fileId, language, LabelResourceFile.Parse(lines));

    /// <summary>
    /// Extracts the language tag from a name like <c>Foo.fr-FR.label.txt</c> → <c>fr-FR</c>.
    /// Returns <c>null</c> when the name doesn't carry a recognisable tag.
    /// </summary>
    public static string? LanguageFromFileName(string fileName) => LabelLanguages.FromContentFileName(fileName);

    /// <summary>
    /// The language of a content file: the <c>&lt;Language&gt;</c> of its <c>AxLabelFile</c> descriptor when the
    /// file sits in a model (<c>…\AxLabelFile\LabelResources\&lt;lang&gt;\</c>), else the tag in its name, else
    /// "unknown".
    /// </summary>
    private static string LanguageOf(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var labelFileFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fullPath)));
        if (labelFileFolder != null && string.Equals(Path.GetFileName(labelFileFolder), "AxLabelFile", StringComparison.OrdinalIgnoreCase))
        {
            var descriptor = LabelFileDescriptorReader.ReadAll(Path.GetDirectoryName(labelFileFolder)!)
                .FirstOrDefault(d => string.Equals(Path.GetFullPath(d.ContentFilePath), fullPath, StringComparison.OrdinalIgnoreCase));
            if (descriptor != null)
                return descriptor.Language;
        }

        return LanguageFromFileName(Path.GetFileName(path)) ?? "unknown";
    }

    private static LabelFile FromResource(string fileId, string language, LabelResourceFile resource)
    {
        var file = new LabelFile(fileId, language);
        foreach (var e in resource.Entries)
            file.AddOrReplace(new Models.LabelEntry(e.Id, e.Text, e.Description.Length == 0 ? null : e.Description));
        return file;
    }
}
