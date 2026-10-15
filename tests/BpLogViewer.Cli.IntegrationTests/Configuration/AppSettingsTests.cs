using BpLogViewer.Cli.Configuration;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void Defaults_AreJsonlAndEuropeBerlin()
    {
        var s = new AppSettings();
        Assert.Equal("jsonl", s.Output.DefaultFormat);
        Assert.Equal("Europe/Berlin", s.Timezone.DisplayTimezone);
        Assert.Equal(1_000, s.Output.MaxRowsBeforeFile);
        Assert.Equal(100_000, s.Output.MaxRowsAbsoluteCeiling);
    }

    [Fact]
    public void DatabaseSettings_DefaultsAreNull()
    {
        var s = new DatabaseSettings();
        Assert.Null(s.ConnectionString);
        Assert.Null(s.LocalDbInstance);
    }

    [Fact]
    public void QueueThresholds_StalePendingThresholdHours_Is24()
    {
        Assert.Equal(24, QueueThresholds.StalePendingThresholdHours);
    }
}
