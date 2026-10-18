using LabelDiffTool.Core.Comparison;
using LabelDiffTool.Core.Models;
using LabelDiffTool.Core.Parsing;
using Xunit;

namespace LabelDiffTool.Tests;

public class LabelComparerAndWriterTests
{
    private static LabelFile FileWith(string id, string lang, params (string Id, string Text, string? Desc)[] entries)
    {
        var f = new LabelFile(id, lang);
        foreach (var e in entries)
            f.AddOrReplace(new LabelEntry(e.Id, e.Text, e.Desc));
        return f;
    }

    [Fact]
    public void Compare_detects_gaps_in_both_directions()
    {
        var a = FileWith("A.en-US.label.txt", "en-US",
            ("Shared", "Text", null),
            ("OnlyInA", "A only", null));
        var b = FileWith("B.fr-FR.label.txt", "fr-FR",
            ("Shared", "Texte", null),
            ("OnlyInB", "B only", null));

        var result = LabelComparer.Compare(new[] { a, b });

        Assert.Equal(3, result.Rows.Count);
        Assert.Equal(new[] { "OnlyInB" }, result.MissingInFile("A.en-US.label.txt"));
        Assert.Equal(new[] { "OnlyInA" }, result.MissingInFile("B.fr-FR.label.txt"));

        var shared = result.Rows.Single(r => r.Id == "Shared");
        Assert.True(shared.IsComplete);
    }

    [Fact]
    public void Writer_roundtrips_labels_and_descriptions()
    {
        var lines = new[]
        {
            ";File header",
            "CustAccount=Customer account",
            ";The customer account number",
            "ItemName=Item name",
        };
        var file = LabelParser.Parse("Foo.en-US.label.txt", "en-US", lines);

        var text = LabelWriter.Write(file);

        // Visual Studio's format: sorted by id, " ;" description lines, CRLF; the header holds no label and is dropped.
        Assert.Equal(
            "CustAccount=Customer account\r\n" +
            " ;The customer account number\r\n" +
            "ItemName=Item name\r\n",
            text);

        // Re-parsing the written text yields the same entries.
        var reparsed = LabelParser.Parse("Foo.en-US.label.txt", "en-US", text.Split("\r\n"));
        Assert.Equal(2, reparsed.Count);
        Assert.Equal("The customer account number", reparsed.Find("CustAccount")!.Description);
    }

    [Fact]
    public void WriteFile_writes_utf8_with_bom_like_visual_studio()
    {
        var file = FileWith("Foo.fr.label.txt", "fr",
            ("Zeta", "Dernier", null),
            ("alpha", "Champ personnalisé", "SOG_1_Setup"));
        var path = Path.Combine(Path.GetTempPath(), $"labeldiff-{Guid.NewGuid():N}.label.txt");
        try
        {
            LabelWriter.WriteFile(file, path);

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
            Assert.Equal(
                "alpha=Champ personnalisé\r\n ;SOG_1_Setup\r\nZeta=Dernier\r\n",
                System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
