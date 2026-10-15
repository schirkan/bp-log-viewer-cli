using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.FilterTests;

public class FilterParserTests
{
    [Fact]
    public void OptionsWithValue_ContainsAllGlobalOptions()
    {
        Assert.Contains("--since", FilterParser.OptionsWithValue);
        Assert.Contains("--until", FilterParser.OptionsWithValue);
        Assert.Contains("--max-rows", FilterParser.OptionsWithValue);
        Assert.Contains("--format", FilterParser.OptionsWithValue);
        Assert.Contains("--output", FilterParser.OptionsWithValue);
        Assert.Contains("--out-file", FilterParser.OptionsWithValue);
        Assert.Contains("--connection-string", FilterParser.OptionsWithValue);
    }

    [Fact]
    public void Parse_Empty_ReturnsEmpty()
    {
        var f = FilterParser.Parse(Array.Empty<string>());
        Assert.Null(f.Since);
        Assert.Null(f.Until);
        Assert.Null(f.Process);
        Assert.Null(f.Resource);
        Assert.Null(f.Status);
        Assert.False(f.HasException);
        Assert.Null(f.MaxRows);
        Assert.False(f.Verbose);
    }

    [Fact]
    public void Parse_AllBasicOptions()
    {
        var f = FilterParser.Parse(new[]
        {
            "--since", "2026-01-01T00:00:00Z",
            "--until", "2026-12-31T23:59:59Z",
            "--process", "MyProcess",
            "--resource", "MyResource",
            "--status", "Completed",
            "--status", "Exception",
            "--has-exception",
            "--level", "error",
            "--text", "boom",
            "--max-rows", "100",
            "--verbose",
            "--format", "jsonl",
            "--output", "out.jsonl",
            "--connection-string", "Server=foo",
        });

        Assert.NotNull(f.Since);
        Assert.NotNull(f.Until);
        Assert.Equal("MyProcess", f.Process);
        Assert.Equal("MyResource", f.Resource);
        Assert.Equal(new[] { "Completed", "Exception" }, f.Status);
        Assert.True(f.HasException);
        Assert.Equal("error", f.Level);
        Assert.Equal("boom", f.Text);
        Assert.Equal(100, f.MaxRows);
        Assert.True(f.Verbose);
        Assert.Equal("jsonl", f.Format);
        Assert.Equal("out.jsonl", f.Output);
        Assert.Equal("Server=foo", f.ConnectionString);
    }

    [Fact]
    public void Parse_OutFileAlias_WorksLikeOutput()
    {
        var f = FilterParser.Parse(new[] { "--out-file", "alt.jsonl" });
        Assert.Equal("alt.jsonl", f.Output);
    }

    [Fact]
    public void Parse_UnknownOption_IsIgnoredAndDoesNotThrow()
    {
        var f = FilterParser.Parse(new[] { "--made-up", "value" });
        Assert.Null(f.Process);
    }

    [Fact]
    public void Parse_InvalidLevel_Throws()
    {
        var ex = Assert.Throws<FilterValidationException>(
            () => FilterParser.Parse(new[] { "--level", "fatal" }));
        Assert.Equal("filter_invalid", ex.Code);
        Assert.Equal(11, ex.ExitCode);
    }

    [Fact]
    public void Parse_InvalidMaxRows_Throws()
    {
        var ex = Assert.Throws<FilterValidationException>(
            () => FilterParser.Parse(new[] { "--max-rows", "-1" }));
        Assert.Equal("filter_invalid", ex.Code);
        Assert.Equal(11, ex.ExitCode);
    }

    [Fact]
    public void Parse_MaxRowsExceedingCeiling_ThrowsExit8()
    {
        var ex = Assert.Throws<FilterValidationException>(
            () => FilterParser.Parse(new[] { "--max-rows", "1000001" }));
        Assert.Equal("max_rows_exceeds_ceiling", ex.Code);
        Assert.Equal(8, ex.ExitCode);
    }

    [Fact]
    public void Parse_InvalidFormat_Throws()
    {
        var ex = Assert.Throws<FilterValidationException>(
            () => FilterParser.Parse(new[] { "--format", "yaml" }));
        Assert.Equal("filter_invalid", ex.Code);
        Assert.Equal(11, ex.ExitCode);
    }

    [Fact]
    public void Parse_InvalidIsoDate_Throws()
    {
        var ex = Assert.Throws<FilterValidationException>(
            () => FilterParser.Parse(new[] { "--since", "not-a-date" }));
        Assert.Equal("filter_invalid", ex.Code);
        Assert.Equal(11, ex.ExitCode);
    }

    [Fact]
    public void Parse_SinceAfterUntil_Throws()
    {
        var ex = Assert.Throws<FilterValidationException>(
            () => FilterParser.Parse(new[]
            {
                "--since", "2026-12-31T00:00:00Z",
                "--until", "2026-01-01T00:00:00Z"
            }));
        Assert.Equal("filter_invalid", ex.Code);
        Assert.Equal(11, ex.ExitCode);
    }

    [Fact]
    public void Parse_MissingValue_Throws()
    {
        Assert.Throws<FilterValidationException>(() => FilterParser.Parse(new[] { "--max-rows" }));
        Assert.Throws<FilterValidationException>(() => FilterParser.Parse(new[] { "--since" }));
        Assert.Throws<FilterValidationException>(() => FilterParser.Parse(new[] { "--format" }));
    }

    [Fact]
    public void Parse_DefaultsAreRespected()
    {
        var defaults = new FilterSet(
            Since: DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            Until: null,
            Process: "DefaultProc",
            Resource: null,
            Status: null,
            HasException: false,
            Level: null,
            Text: null,
            MaxRows: 500,
            Verbose: false,
            Format: null,
            Output: null,
            ConnectionString: null);
        var f = FilterParser.Parse(Array.Empty<string>(), defaults);
        Assert.Equal(DateTimeOffset.Parse("2026-06-01T00:00:00Z").ToUniversalTime(), f.Since);
        Assert.Equal("DefaultProc", f.Process);
        Assert.Equal(500, f.MaxRows);
    }

    [Fact]
    public void Empty_HasSensibleDefaults()
    {
        var f = FilterSet.Empty;
        Assert.Equal(1_000, f.EffectiveMaxRows);
        Assert.Equal("jsonl", f.EffectiveFormat);
    }

    [Fact]
    public void EffectiveMaxRows_UsesExplicitValue()
    {
        var f = FilterSet.Empty with { MaxRows = 42 };
        Assert.Equal(42, f.EffectiveMaxRows);
    }
}
