using BpLogViewer.Cli.Configuration;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Configuration;

public class PathResolverTests
{
    [Fact]
    public void Resolve_NullOrEmpty_ReturnsBaseDir()
    {
        var baseDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        Assert.Equal(baseDir, PathResolver.Resolve(null, baseDir));
        Assert.Equal(baseDir, PathResolver.Resolve("", baseDir));
        Assert.Equal(baseDir, PathResolver.Resolve("   ", baseDir));
    }

    [Fact]
    public void Resolve_NullOrEmpty_DefaultsToAppContextBaseDirectory()
    {
        var result = PathResolver.Resolve(null);
        // BaseDir may or may not have a trailing separator depending on the
        // runtime; compare the canonicalized forms.
        var expected = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var actual = result.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_AbsolutePath_ReturnsItself()
    {
        var absolute = Path.GetFullPath(@"C:\temp\foo.jsonl");
        Assert.Equal(absolute, PathResolver.Resolve(absolute, baseDir: @"C:\elsewhere"));
    }

    [Fact]
    public void Resolve_RelativePath_AnchoredAtBaseDir()
    {
        var baseDir = @"C:\work";
        var expected = Path.GetFullPath(Path.Combine(baseDir, "out.jsonl"));
        Assert.Equal(expected, PathResolver.Resolve("out.jsonl", baseDir));
    }

    [Fact]
    public void Resolve_NormalizesRelativePath()
    {
        var baseDir = @"C:\work";
        var expected = Path.GetFullPath(Path.Combine(baseDir, "sub", "out.jsonl"));
        Assert.Equal(expected, PathResolver.Resolve("sub/../sub/out.jsonl", baseDir));
    }

    [Fact]
    public void TryResolveOutputPath_Empty_Fails()
    {
        var ok = PathResolver.TryResolveOutputPath("", out var path, out var code, out var msg);
        Assert.False(ok);
        Assert.Equal("output_invalid_path", code);
        Assert.Equal(string.Empty, path);
        Assert.NotNull(msg);
    }

    [Fact]
    public void TryResolveOutputPath_Whitespace_Fails()
    {
        var ok = PathResolver.TryResolveOutputPath("   ", out _, out var code, out _);
        Assert.False(ok);
        Assert.Equal("output_invalid_path", code);
    }

    [Fact]
    public void TryResolveOutputPath_RelativePathInBaseDir_Succeeds()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "bp-log-viewer-tests");
        Directory.CreateDirectory(tmp);
        try
        {
            var prev = Environment.GetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT");
            Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", tmp);
            try
            {
                var ok = PathResolver.TryResolveOutputPath("out.jsonl", out var path, out var code, out var msg);
                Assert.True(ok, msg);
                Assert.Null(code);
                Assert.StartsWith(tmp, path);
            }
            finally
            {
                Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", prev);
            }
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void TryResolveOutputPath_Traversal_Blocked()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "bp-log-viewer-tests-traversal");
        Directory.CreateDirectory(tmp);
        try
        {
            var prev = Environment.GetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT");
            Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", tmp);
            try
            {
                var ok = PathResolver.TryResolveOutputPath("..\\..\\escape.jsonl", out _, out var code, out _);
                Assert.False(ok);
                Assert.Equal("output_path_outside_root", code);
            }
            finally
            {
                Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", prev);
            }
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void TryResolveOutputPath_ExistingDirectory_Rejected()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "bp-log-viewer-tests-dir");
        Directory.CreateDirectory(tmp);
        try
        {
            var prev = Environment.GetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT");
            Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", tmp);
            try
            {
                var ok = PathResolver.TryResolveOutputPath(".", out _, out var code, out _);
                Assert.False(ok);
                Assert.Equal("output_is_directory", code);
            }
            finally
            {
                Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", prev);
            }
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }
}
