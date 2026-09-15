using LooseLeaf.Models;

namespace LooseLeaf.Tests;

public sealed class ModelTests
{
    [Fact]
    public void GetSource_ResolvesNestedPropertyPath()
    {
        var sources = new Dictionary<string, FieldSource>(StringComparer.OrdinalIgnoreCase)
        {
            ["Vendor.Name"] = new FieldSource { Page = 1, Text = "Acme Corp" }
        };
        var result = new ExtractionResult<InvoiceWithVendor>(
            new InvoiceWithVendor { Vendor = new Vendor { Name = "Acme Corp" } },
            sources,
            [],
            new ProcessedDocument());

        var source = result.GetSource(x => x.Vendor.Name);

        Assert.NotNull(source);
        Assert.Equal("Acme Corp", source!.Text);
    }

    [Fact]
    public void Citations_ReturnsAllRegisteredSources()
    {
        var sources = new Dictionary<string, FieldSource>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new FieldSource { Page = 1 },
            ["B"] = new FieldSource { Page = 2 }
        };
        var result = new ExtractionResult<InvoiceWithVendor>(
            new InvoiceWithVendor(),
            sources,
            [],
            new ProcessedDocument());

        Assert.Equal(2, result.Citations.Count);
    }

    [Fact]
    public void ValidateData_ReturnsIssue_WhenRequiredFieldMissing()
    {
        var issues = ExtractionResult<ValidatedModel>.ValidateData(new ValidatedModel { Name = null! });

        Assert.Contains(issues, i => i.Field == nameof(ValidatedModel.Name));
    }

    [Fact]
    public void ValidateData_ReturnsNoIssues_WhenModelIsValid()
    {
        var issues = ExtractionResult<ValidatedModel>.ValidateData(new ValidatedModel { Name = "ok" });

        Assert.Empty(issues);
    }

    [Fact]
    public void BoundingBox_StoresNormalizedCoordinates()
    {
        var bounds = new BoundingBox(0.1f, 0.2f, 0.3f, 0.4f);

        Assert.Equal(0.1f, bounds.X);
        Assert.Equal(0.2f, bounds.Y);
        Assert.Equal(0.3f, bounds.Width);
        Assert.Equal(0.4f, bounds.Height);
    }

    private sealed class InvoiceWithVendor
    {
        public Vendor Vendor { get; set; } = new();
    }

    private sealed class Vendor
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ValidatedModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;
    }
}
