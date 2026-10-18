using LabelDiffTool.Core.Parsing;
using Xunit;

namespace LabelDiffTool.Tests;

public class LabelParserTests
{
    [Theory]
    [InlineData("Foo.fr-FR.label.txt", "fr-FR")]
    [InlineData("MyLabels.en-US.label.txt", "en-US")]
    [InlineData("Bar.zh-Hans.label.txt", "zh-Hans")]
    [InlineData("Baz.txt", null)]
    [InlineData("NoLanguage.label.txt", null)]
    public void LanguageFromFileName_extracts_tag(string fileName, string? expected)
    {
        Assert.Equal(expected, LabelParser.LanguageFromFileName(fileName));
    }

    [Fact]
    public void Parse_binds_description_to_preceding_label()
    {
        var lines = new[]
        {
            ";Header comment",
            "CustAccount=Customer account",
            ";The account number of the customer",
            "ItemName=Item name",
        };

        var file = LabelParser.Parse("Foo.en-US.label.txt", "en-US", lines);

        // The header comment holds no label: like Visual Studio, the parser does not keep it.
        Assert.Equal(2, file.Count);

        var cust = file.Find("CustAccount");
        Assert.NotNull(cust);
        Assert.Equal("Customer account", cust!.Text);
        Assert.Equal("The account number of the customer", cust.Description);

        var item = file.Find("ItemName");
        Assert.NotNull(item);
        Assert.Null(item!.Description);
    }

    [Fact]
    public void Parse_keeps_equals_sign_inside_text()
    {
        var file = LabelParser.Parse("f.en-US.label.txt", "en-US", new[] { "Formula=a=b+c" });
        Assert.Equal("a=b+c", file.Find("Formula")!.Text);
    }

    [Fact]
    public void Parse_reads_hash_descriptions_and_indented_ones()
    {
        // Both occur in Microsoft's own files (StateMachine.en-US.label.txt uses '#').
        var lines = new[]
        {
            "NewLabel0=Label file created",
            "#Label file comment",
            "InvalidTransition=Invalid operation",
            " ;Shown when the transition is invalid",
        };

        var file = LabelParser.Parse("StateMachine.en-US.label.txt", "en-US", lines);

        Assert.Equal("Label file comment", file.Find("NewLabel0")!.Description);
        Assert.Equal("Shown when the transition is invalid", file.Find("InvalidTransition")!.Description);
    }

    [Fact]
    public void Ids_are_found_ignoring_case()
    {
        var file = LabelParser.Parse("f.en-US.label.txt", "en-US", new[] { "CustAccount=Customer account" });

        Assert.True(file.Contains("custaccount"));
        Assert.Equal("Customer account", file.Find("CUSTACCOUNT")!.Text);
    }
}
