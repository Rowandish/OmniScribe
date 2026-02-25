using System;
using OmniScribe.Models;
using Xunit;

namespace OmniScribe.Tests.Models;

public class SessionRecordTests
{
    [Fact]
    public void DefaultId_IsEightChars()
    {
        var record = new SessionRecord();
        Assert.Equal(8, record.Id.Length);
    }

    [Fact]
    public void TwoRecords_HaveDifferentIds()
    {
        var r1 = new SessionRecord();
        var r2 = new SessionRecord();
        Assert.NotEqual(r1.Id, r2.Id);
    }

    [Fact]
    public void DefaultTimestamp_IsCloseToNow()
    {
        var before = DateTime.Now;
        var record = new SessionRecord();
        var after = DateTime.Now;

        Assert.InRange(record.Timestamp, before, after);
    }

    [Fact]
    public void DisplayName_WithSourceFile_ShowsFileName()
    {
        var record = new SessionRecord
        {
            Timestamp = new DateTime(2025, 6, 15, 14, 30, 0),
            SourceFileName = "meeting.mp3"
        };

        Assert.Equal("14:30 — meeting.mp3", record.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutSourceFile_ShowsRegistrazione()
    {
        var record = new SessionRecord
        {
            Timestamp = new DateTime(2025, 6, 15, 9, 5, 0),
            SourceFileName = ""
        };

        Assert.Equal("09:05 — Registrazione", record.DisplayName);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var record = new SessionRecord
        {
            TranscriptionText = "Hello world",
            AnalysisResult = "# Summary",
            TokensUsed = 500,
            Cost = 0.01m
        };

        Assert.Equal("Hello world", record.TranscriptionText);
        Assert.Equal("# Summary", record.AnalysisResult);
        Assert.Equal(500, record.TokensUsed);
        Assert.Equal(0.01m, record.Cost);
    }
}
