using D365KnowledgeBase.Labels;
using LabelDiffTool.Core.Models;

namespace LabelDiffTool.Core.Parsing;

/// <summary>
/// Serialises a <see cref="LabelFile"/> back to the D365 label file format, exactly as Visual Studio's label
/// editor writes it (<see cref="LabelResourceFile"/>, D365KnowledgeBase.Labels): UTF-8 with a BOM, CRLF, the
/// whole file sorted by label id ignoring case, <c>Id=Text</c> then <c> ;Description</c> when there is one.
/// </summary>
public static class LabelWriter
{
    public static void WriteFile(LabelFile file, string? path = null)
    {
        var target = path ?? file.FilePath
            ?? throw new InvalidOperationException("No path supplied and the file has no FilePath.");
        ToResource(file).Save(target);
    }

    /// <summary>The file content as it is saved, without the byte order mark.</summary>
    public static string Write(LabelFile file) => ToResource(file).ToText();

    private static LabelResourceFile ToResource(LabelFile file)
    {
        var resource = new LabelResourceFile();
        foreach (var entry in file.Entries)
            resource.AddOrReplace(new D365KnowledgeBase.Labels.LabelEntry(entry.Id, entry.Text, entry.Description ?? ""));
        return resource;
    }
}
